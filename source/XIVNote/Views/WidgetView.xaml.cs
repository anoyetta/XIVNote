using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
        public WidgetView()
        {
            this.InitializeComponent();
            InitializeCef();
            this.Topmost = true;

            this.MinWidth = Note.MinWidth;
            this.MinHeight = Note.MinHeight;

            this.ToolBarGrid.MouseLeftButtonDown += (_, __) => this.StartDragMove();
            this.BackgroundBorder.MouseLeftButtonDown += (_, __) => this.StartDragMove();

            this.MouseEnter += (_, __) => this.ToolBarGrid.Visibility = Visibility.Visible;
            this.MouseLeave += (_, __) => this.ToolBarGrid.Visibility = Visibility.Collapsed;

            this.Loaded += (_, __) =>
            {
                this.ViewModel.Model.PropertyChanged += this.Model_PropertyChanged;

                this.OverlayVisible = this.ViewModel.Model?.IsVisible ?? true;

                this.ToolBarGrid.Visibility = this.IsMouseOver ?
                    Visibility.Visible :
                    Visibility.Collapsed;

                this.SubscribeZOrderCorrector();

                this.CefBrowser.LoadingStateChanged += async (___, e) =>
                    await WPFHelper.Dispatcher.InvokeAsync(() =>
                        this.LoadingLabel.Visibility = e.IsLoading ?
                            Visibility.Visible :
                            Visibility.Collapsed);

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

            this.UrlTextBox.KeyUp += async (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    this.UrlPanel.Visibility = Visibility.Collapsed;
                    await WPFHelper.Dispatcher.InvokeAsync(
                        () => this.ViewModel.Model.Text = this.UrlTextBox.Text);
                }
            };
        }

        public WidgetViewModel ViewModel => this.DataContext as WidgetViewModel;

        public Guid ID => this.ViewModel?.Model?.ID ?? Guid.Empty;

        public Note Note
        {
            get => this.ViewModel.Model;
            set => this.ViewModel.Model = value;
        }

        private Lazy<ChromiumWebBrowser> LazyCefBrowser = new Lazy<ChromiumWebBrowser>(() => new ChromiumWebBrowser());

        public ChromiumWebBrowser CefBrowser => this.LazyCefBrowser.Value;

        private static volatile bool isInitialized = false;

        public static void InitializeCef()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

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
            settings.DisableGpuAcceleration();

            Cef.Initialize(settings);
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
}
