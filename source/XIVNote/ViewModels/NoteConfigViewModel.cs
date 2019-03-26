using System;
using aframe;
using Prism.Commands;
using Prism.Mvvm;

namespace XIVNote.ViewModels
{
    public class NoteConfigViewModel : BindableBase
    {
        public NoteConfigViewModel()
        {
        }

        private Note model = WPFHelper.IsDesignMode ? Note.DefaultNoteStyle : null;

        public Note Model
        {
            get => this.model;
            set => this.SetProperty(ref this.model, value);
        }

        public Guid ID => this.Model?.ID ?? Guid.Empty;

        private DelegateCommand changeBackgroundCommand;

        public DelegateCommand ChangeBackgroundCommand =>
            this.changeBackgroundCommand ?? (this.changeBackgroundCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeColor(
                    () => this.model.BackgroundColor,
                    color => this.model.BackgroundColor = color)));

        private DelegateCommand changeForegroundCommand;

        public DelegateCommand ChangeForegroundCommand =>
            this.changeForegroundCommand ?? (this.changeForegroundCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeColor(
                    () => this.model.ForegroundColor,
                    color => this.model.ForegroundColor = color)));

        private DelegateCommand changeFontCommand;

        public DelegateCommand ChangeFontCommand =>
            this.changeFontCommand ?? (this.changeFontCommand = new DelegateCommand(
                () => CommandHelper.ExecuteChangeFont(
                    () => this.model.Font,
                    font => this.model.Font = font)));
    }
}
