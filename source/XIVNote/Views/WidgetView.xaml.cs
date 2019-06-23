using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using aframe;
using aframe.Views;
using CefSharp;
using CefSharp.Wpf;
using XIVNote.ViewModels;

namespace XIVNote.Views
{
    /// <summary>
    /// WidgetView.xaml の相互作用ロジック
    /// </summary>
    public partial class WidgetView : Window, INoteOverlay
    {
        private static readonly double VisibleOpacity = 1;
        private static readonly double HideOpacity = 0.001;

        public WidgetView()
        {
            this.InitializeComponent();
            InitializeCef();
            this.Topmost = true;

            this.MinWidth = Note.MinWidth;
            this.MinHeight = Note.MinHeight;

            this.ToolBarGrid.MouseLeftButtonDown += (_, __) => this.StartDragMove();
            this.BackgroundBorder.MouseLeftButtonDown += (_, __) => this.StartDragMove();

            this.MouseEnter += (_, __) => this.ToolBarGrid.Opacity = VisibleOpacity;
            this.MouseLeave += (_, __) => this.ToolBarGrid.Opacity = HideOpacity;

            this.Loaded += (_, __) =>
            {
                this.ApplyLock();

                this.ViewModel.Model.PropertyChanged += this.Model_PropertyChanged;

                this.InitializeOverlayVisible(
                    ref this.overlayVisible,
                    this.ViewModel.Model?.IsVisible ?? true);

                this.ToolBarGrid.Opacity = this.IsMouseOver ? VisibleOpacity : HideOpacity;

                this.SubscribeZOrderCorrector();

                this.CefBrowser.LoadingStateChanged += async (___, e) =>
                {
                    /*
                    // 動かないので封印
                    if (!e.IsLoading)
                    {
                        this.InjectUserCSS();
                    }
                    */

                    await WPFHelper.Dispatcher.InvokeAsync(() =>
                        this.LoadingLabel.Visibility = e.IsLoading ?
                            Visibility.Visible :
                            Visibility.Collapsed);
                };

                this.Note.RefreshCallback = () => this.CefBrowser.Reload();

                this.CefBrowser.Address = this.Note?.Text;
                this.WebGrid.Children.Add(this.CefBrowser);
            };

            this.Closed += (_, __) =>
            {
                this.ViewModel.Model.PropertyChanged -= this.Model_PropertyChanged;
            };

            this.LeftThumb.DragDelta += (_, e) =>
            {
                this.Left += e.HorizontalChange;
                this.Width -= e.HorizontalChange;
            };

            this.RightThumb.DragDelta += (_, e) =>
            {
                this.Width += e.HorizontalChange;
            };

            this.TopThumb.DragDelta += (_, e) =>
            {
                this.Top += e.VerticalChange;
                this.Height -= e.VerticalChange;
            };

            this.BottomThumb.DragDelta += (_, e) =>
            {
                this.Height += e.VerticalChange;
            };

            this.UrlPanel.MouseLeftButtonUp += (_, __)
                => this.UrlPanel.Visibility = Visibility.Collapsed;

            this.UrlTextBox.KeyUp += (_, e) => closeInputBox(e);
            this.NameTextBox.KeyUp += (_, e) => closeInputBox(e);

            async void closeInputBox(KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    this.UrlPanel.Visibility = Visibility.Collapsed;
                    await WPFHelper.Dispatcher.InvokeAsync(
                        () =>
                        {
                            this.ViewModel.Model.Text = this.UrlTextBox.Text;
                            this.ViewModel.Model.Name = this.NameTextBox.Text;
                        });
                }
            }
        }

        public WidgetViewModel ViewModel => this.DataContext as WidgetViewModel;

        public Guid ID => this.ViewModel?.Model?.ID ?? Guid.Empty;

        public Note Note
        {
            get => this.ViewModel.Model;
            set => this.ViewModel.Model = value;
        }

        private Lazy<ChromiumWebBrowser> LazyCefBrowser = new Lazy<ChromiumWebBrowser>(() =>
        {
            var browser = new ChromiumWebBrowser()
            {
                RequestHandler = new WidgetRequestHandler(),
                BrowserSettings = new BrowserSettings()
                {
                    FileAccessFromFileUrls = CefState.Enabled,
                    UniversalAccessFromFileUrls = CefState.Enabled,
                    WindowlessFrameRate = 30,
                },
            };

            return browser;
        });

        public ChromiumWebBrowser CefBrowser => this.LazyCefBrowser.Value;

        private static volatile bool isInitialized = false;
        private static readonly object CefLocker = new object();

        public static void InitializeCef()
        {
            lock (CefLocker)
            {
                if (isInitialized)
                {
                    return;
                }

                isInitialized = true;
            }

            var settings = new CefSettings();

            settings.BrowserSubprocessPath = Path.Combine(
                AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                Environment.Is64BitProcess ? "x64" : "x86",
                "CefSharp.BrowserSubprocess.exe");

            settings.Locale = CultureInfo.CurrentCulture.Parent.ToString();
            settings.AcceptLanguageList = CultureInfo.CurrentCulture.Name;

            settings.CachePath = Path.Combine(
                Path.GetTempPath(),
                "XIVNote");
            settings.LogFile = Path.Combine(
                Path.GetTempPath(),
                "XIVNote",
                "browser.log");
            settings.LogSeverity = LogSeverity.Warning;

            // GPUアクセラレータを切る
            settings.DisableGpuAcceleration();

            Cef.EnableHighDPISupport();
            Cef.Initialize(settings);

            // shutdown を仕込む
            App.Current.Exit += (_, __) => Cef.Shutdown();
        }

        private async void InjectUserCSS()
        {
            var css = new StringBuilder();
            css.AppendLine(@"<style type=""text/css"">");
            css.AppendLine(@"body { font-family: HackGen, sans-serif !important; }");
            css.AppendLine(@"</style>");

            var script = "(function() {{ $('head').append('" + css.ToString() + "'); return true; }}) ();";

            await this.CefBrowser.GetMainFrame().EvaluateScriptAsync(script, null);
        }

        private readonly SolidColorBrush SemiTransparent = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#01000000"));

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var model = sender as Note;

            switch (e.PropertyName)
            {
                case nameof(Note.IsVisible):
                    this.OverlayVisible = model.IsVisible;
                    break;

                case nameof(Note.Text):
                    this.NaviateUrl(this.ViewModel.Model.Text);
                    break;

                case nameof(Note.IsLock):
                    this.ApplyLock();
                    break;

                default:
                    break;
            }

            WidgetViewModel.IsSaveQueue = true;
        }

        private void StartDragMove()
        {
            if (!this.Note.IsLock)
            {
                this.DragMove();
            }
        }

        private void ApplyLock()
        {
            this.ResizeMode = this.Note.IsLock ?
                ResizeMode.NoResize :
                ResizeMode.CanResizeWithGrip;
            this.Background = this.Note.IsLock ?
                Brushes.Transparent :
                this.SemiTransparent;
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.UrlPanel.Visibility != Visibility.Visible)
            {
                this.UrlPanel.Visibility = Visibility.Visible;
                this.UrlTextBox.Focus();
                this.UrlTextBox.SelectAll();
            }
            else
            {
                this.UrlPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void NaviateUrl(
            string url)
            => await WPFHelper.Dispatcher.InvokeAsync(
                () => this.CefBrowser.Address = url);

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            this.Note.RefreshCommand.Execute();
        }

        private void DevToolButton_Click(object sender, RoutedEventArgs e)
        {
            this.CefBrowser.ShowDevTools();
        }

        #region IOverlay

        public int ZOrder => 0;

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value);
        }

        #endregion IOverlay
    }

    public class WidgetRequestHandler : IRequestHandler
    {
        public CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
            => CefReturnValue.Continue;

        public bool CanGetCookies(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
            => true;

        public bool CanSetCookie(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, Cookie cookie)
            => true;

        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
            => true;

        public IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            => null;

        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            => false;

        public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
            => true;

        public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            => false;

        public void OnPluginCrashed(IWebBrowser chromiumWebBrowser, IBrowser browser, string pluginPath)
        {
        }

        public bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, string url)
            => true;

        public bool OnQuotaRequest(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
            => true;

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status)
        {
        }

        public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
        }

        public void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
        }

        public bool OnResourceResponse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            => false;

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
            => false;
    }
}
