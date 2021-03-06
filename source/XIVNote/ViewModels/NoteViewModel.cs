﻿using System;
using System.Windows.Threading;
using aframe;
using Prism.Commands;
using Prism.Mvvm;
using XIVNote.Views;

namespace XIVNote.ViewModels
{
    public class NoteViewModel : BindableBase
    {
        public NoteViewModel()
        {
            if (!AutoSaveTimer.IsEnabled)
            {
                AutoSaveTimer.Start();
            }
        }

        private Note model = WPFHelper.IsDesignMode ? Note.DefaultNoteStyle : null;

        public Note Model
        {
            get => this.model;
            set => this.SetProperty(ref this.model, value);
        }

        public Guid ID => this.Model?.ID ?? Guid.Empty;

        public Config Config => Config.Instance;

        #region AddNote

        private DelegateCommand addNoteCommand;

        public DelegateCommand AddNoteCommand =>
            this.addNoteCommand ?? (this.addNoteCommand = new DelegateCommand(this.ExecuteAddNoteCommand));

        private async void ExecuteAddNoteCommand() => await Notes.Instance.AddNoteAsync(this.Model);

        #endregion AddNote

        #region ShowConfig

        private DelegateCommand showConfigCommand;

        public DelegateCommand ShowConfigCommand =>
            this.showConfigCommand ?? (this.showConfigCommand = new DelegateCommand(this.ExecuteShowConfigCommand));

        private void ExecuteShowConfigCommand()
        {
            var config = new NoteConfigView();

            config.ViewModel.Model = this.Model;
            config.Height = this.Model.H;
            config.Top = this.Model.Y;
            config.Left = this.Model.X + this.Model.W + 3;

            config.Show();
        }

        #endregion ShowConfig

        #region Minimize

        private DelegateCommand minimizeCommand;

        public DelegateCommand MinimizeCommand =>
            this.minimizeCommand ?? (this.minimizeCommand = new DelegateCommand(this.ExecuteMinimizeCommand));

        private void ExecuteMinimizeCommand()
        {
            this.Model.IsVisible = false;
            Notes.Instance.EnqueueSave();
        }

        #endregion Minimize

        #region Close

        private DelegateCommand closeCommand;

        public DelegateCommand CloseCommand =>
            this.closeCommand ?? (this.closeCommand = new DelegateCommand(this.ExecuteCloseCommand));

        private async void ExecuteCloseCommand() => await Notes.Instance.RemoveNoteAsync(this.Model);

        #endregion Close

        #region Auto Save Timer

        public static volatile bool IsSaveQueue = false;

        private static readonly Lazy<DispatcherTimer> LazyAutoSaveTimer = new Lazy<DispatcherTimer>(CreateAutoSaveTimer);

        private static DispatcherTimer AutoSaveTimer => LazyAutoSaveTimer.Value;

        private static DispatcherTimer CreateAutoSaveTimer()
        {
            var timer = new DispatcherTimer(DispatcherPriority.ContextIdle)
            {
                Interval = TimeSpan.FromSeconds(6),
            };

            timer.Tick += (_, __) =>
            {
                if (IsSaveQueue)
                {
                    IsSaveQueue = false;
                    Notes.Instance.EnqueueSave();
                }
            };

            return timer;
        }

        #endregion Auto Save Timer
    }
}
