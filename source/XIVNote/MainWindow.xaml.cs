using System;
using System.Windows;
using MahApps.Metro.Controls;
using XIVNote.ViewModels;

namespace XIVNote
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.StateChanged += this.MainWindow_StateChanged;

            if (Config.Instance.IsMinimizeStartup)
            {
                this.ShowInTaskbar = false;
                this.WindowState = WindowState.Minimized;

                this.Loaded += (_, __) =>
                {
                    this.ToHide();
                    this.ShowInTaskbar = true;
                };
            }
            else
            {
                this.Loaded += (_, __) => this.Activate();
            }

            this.ViewModel.ShowCallback = () => this.ToShow();
        }

        public MainWindowViewModel ViewModel => this.DataContext as MainWindowViewModel;

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ToHide();
            }
            else
            {
                this.ToShow();
            }
        }

        public void ToShow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.NotifyIcon.Visibility = Visibility.Collapsed;

            this.Activate();
        }

        public void ToHide()
        {
            this.NotifyIcon.Visibility = Visibility.Visible;
            this.Hide();
        }
    }
}
