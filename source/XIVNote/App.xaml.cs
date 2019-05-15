using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using aframe;

namespace XIVNote
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            this.Startup += this.App_Startup;
            this.Exit += this.App_Exit;
            this.DispatcherUnhandledException += this.App_DispatcherUnhandledException;
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            var c = Config.Instance;
            c.SetStartup(c.IsStartupWithWindows);

            var notes = await Task.Run(() => Notes.Instance);
            await notes.ShowNotesAsync();

            await Task.Delay(10);
            notes.StartForegroundAppSubscriber();
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            // NO-OP
        }

        private async void App_DispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e)
        {
            await Task.Run(() =>
            {
                Notes.Instance.Save();
                Config.Instance.Save();

                File.WriteAllText(
                    @".\XIVNote.error.log",
                    e.Exception.ToString(),
                    new UTF8Encoding(false));
            });

            if (this.MainWindow != null)
            {
                MessageBoxHelper.ShowDialogMessageWindow(
                    "XIVNote - Fatal",
                    "予期しない例外を検知しました。アプリケーションを終了します。",
                    e.Exception);
            }
            else
            {
                MessageBox.Show(
                    "予期しない例外を検知しました。アプリケーションを終了します。\n\n" +
                    e.Exception,
                    "XIVNote - Fatal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
