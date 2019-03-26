using System;
using MahApps.Metro.Controls;
using XIVNote.ViewModels;

namespace XIVNote.Views
{
    /// <summary>
    /// NoteConfigView.xaml の相互作用ロジック
    /// </summary>
    public partial class NoteConfigView : MetroWindow
    {
        public NoteConfigView()
        {
            this.InitializeComponent();
        }

        public NoteConfigViewModel ViewModel => this.DataContext as NoteConfigViewModel;

        public Guid ID => this.ViewModel?.Model?.ID ?? Guid.Empty;

        public Config Config => Config.Instance;
    }
}
