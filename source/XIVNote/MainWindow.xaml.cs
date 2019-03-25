using System.Threading.Tasks;
using MahApps.Metro.Controls;

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

            this.Closed += async (_, __) =>
            {
                await Task.Run(() =>
                {
                    Notes.Instance.Save();
                    Config.Instance.Save();
                });
            };
        }
    }
}
