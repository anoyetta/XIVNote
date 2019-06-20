using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using aframe;
using Prism.Commands;
using Prism.Mvvm;

namespace XIVNote.ViewModels
{
    public class WidgetViewModel : BindableBase
    {
        public WidgetViewModel()
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

        #region Minimize

        private DelegateCommand minimizeCommand;

        public DelegateCommand MinimizeCommand =>
            this.minimizeCommand ?? (this.minimizeCommand = new DelegateCommand(this.ExecuteMinimizeCommand));

        private async void ExecuteMinimizeCommand()
        {
            this.Model.IsVisible = false;
            await Task.Run(Notes.Instance.Save);
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
                Interval = TimeSpan.FromSeconds(5),
            };

            timer.Tick += async (_, __) =>
            {
                if (IsSaveQueue)
                {
                    IsSaveQueue = false;
                    await Task.Run(Notes.Instance.Save);
                }
            };

            return timer;
        }

        #endregion Auto Save Timer
    }
}
