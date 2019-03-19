using System.Windows;
using System.Windows.Threading;

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

            this.Startup += this.App_Startup;
            this.Exit += this.App_Exit;
            this.DispatcherUnhandledException += this.App_DispatcherUnhandledException;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            var notes = Notes.Instance;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Notes.Instance.Save();
            Config.Instance.Save();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Notes.Instance.Save();
            Config.Instance.Save();
        }
    }
}
