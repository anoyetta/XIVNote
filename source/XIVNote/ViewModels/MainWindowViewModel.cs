using System.Collections.Generic;
using System.Linq;
using aframe;
using Prism.Commands;
using Prism.Mvvm;

namespace XIVNote.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            Notes.Instance.NoteList.CollectionChanged += (_, __) => this.RaisePropertyChanged(nameof(this.NoteList));
        }

        public Config Config => Config.Instance;

        public IEnumerable<Note> NoteList => Notes.Instance.NoteList.Where(x => !x.IsDefault);

        #region ShowConfig

        private DelegateCommand showConfigCommand;

        public DelegateCommand ShowConfigCommand =>
            this.showConfigCommand ?? (this.showConfigCommand = new DelegateCommand(this.ExecuteShowConfigCommand));

        private void ExecuteShowConfigCommand()
        {
        }

        #endregion ShowConfig

        #region Exit

        private DelegateCommand exitCommand;

        public DelegateCommand ExitCommand =>
            this.exitCommand ?? (this.exitCommand = new DelegateCommand(this.ExecuteExitCommand));

        private void ExecuteExitCommand() => WPFHelper.MainWindow?.Close();

        #endregion Exit

        #region AddNote

        private DelegateCommand addNoteCommand;

        public DelegateCommand AddNoteCommand =>
            this.addNoteCommand ?? (this.addNoteCommand = new DelegateCommand(this.ExecuteAddNoteCommand));

        private async void ExecuteAddNoteCommand() => await Notes.Instance.AddNoteAsync();

        #endregion AddNote
    }
}
