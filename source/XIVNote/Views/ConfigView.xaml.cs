using System.Threading.Tasks;
using MahApps.Metro.Controls;

namespace XIVNote.Views
{
    /// <summary>
    /// ConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigView : MetroWindow
    {
        public ConfigView()
        {
            this.InitializeComponent();

            this.Closed += async (_, __) =>
            {
                await Task.Run(() =>
                {
                    Config.Instance.Save();
                    Notes.Instance.Save();
                });
            };
        }
    }
}
