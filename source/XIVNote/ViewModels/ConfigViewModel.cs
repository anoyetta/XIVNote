using aframe;
using Prism.Commands;
using Prism.Mvvm;

namespace XIVNote.ViewModels
{
    public class ConfigViewModel : BindableBase
    {
        public ConfigViewModel()
        {
        }

        public Config Config => Config.Instance;

        public Note DefaultNote => Notes.Instance.DefaultNote;

        private DelegateCommand changeBackgroundCommand;

        public DelegateCommand ChangeBackgroundCommand =>
            this.changeBackgroundCommand ?? (this.changeBackgroundCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeColor(
                    () => this.DefaultNote.BackgroundColor,
                    color => this.DefaultNote.BackgroundColor = color)));

        private DelegateCommand changeForegroundCommand;

        public DelegateCommand ChangeForegroundCommand =>
            this.changeForegroundCommand ?? (this.changeForegroundCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeColor(
                    () => this.DefaultNote.ForegroundColor,
                    color => this.DefaultNote.ForegroundColor = color)));

        private DelegateCommand changeFontCommand;

        public DelegateCommand ChangeFontCommand =>
            this.changeFontCommand ?? (this.changeFontCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeFont(
                    () => this.DefaultNote.Font,
                    font => this.DefaultNote.Font = font)));
    }
}
