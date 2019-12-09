using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using aframe;
using aframe.Views;
using XIVNote.Views;

namespace XIVNote
{
    public class Notes
    {
        #region Lazy Instance

        private static readonly Lazy<Notes> LazyInstance = new Lazy<Notes>();

        public static Notes Instance => LazyInstance.Value;

        #endregion Lazy Instance

        public Notes()
        {
            this.Load();
        }

        [XmlElement("note")]
        public SuspendableObservableCollection<Note> NoteList { get; } = new SuspendableObservableCollection<Note>();

        [XmlIgnore]
        public Note DefaultNote => this.NoteList.FirstOrDefault(x => x.IsDefault);

        public static string FileName => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.xml");

        private static readonly XmlSerializer NotesSerializer = new XmlSerializer(
            typeof(SuspendableObservableCollection<Note>),
            new XmlRootAttribute("notes"));

        private volatile bool isLoading;

        public void Load() => this.Load(FileName);

        public void Load(
            string fileName)
        {
            lock (this)
            {
                this.isLoading = true;

                try
                {
                    if (!File.Exists(fileName))
                    {
                        return;
                    }

                    using (var sr = new StreamReader(fileName, new UTF8Encoding(false)))
                    {
                        if (sr.BaseStream.Length <= 0)
                        {
                            return;
                        }

                        var data = NotesSerializer.Deserialize(sr) as IEnumerable<Note>;

                        if (data != null)
                        {
                            this.NoteList.AddRange(data, true);
                        }
                    }

                    if (!this.NoteList.Any(x => x.IsDefault))
                    {
                        this.NoteList.Add(Note.DefaultNoteStyle);
                    }

                    this.NoteList.CollectionChanged += (_, __) => this.EnqueueSave();

                    foreach (var note in this.NoteList)
                    {
                        note.SetAutoSave();
                    }
                }
                finally
                {
                    this.isLoading = false;
                }
            }
        }

        public void Save() => this.Save(FileName);

        public void Save(
            string fileName)
        {
            lock (this)
            {
                var dir = Path.GetDirectoryName(FileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, string.Empty);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                {
                    NotesSerializer.Serialize(sw, this.NoteList, ns);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    fileName,
                    sb.ToString() + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }

        private volatile bool isSaving;

        public async void EnqueueSave()
        {
            if (this.isLoading)
            {
                return;
            }

            if (this.isSaving)
            {
                return;
            }

            this.isSaving = true;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await Task.Run(() => this.Save(FileName));
            }
            finally
            {
                this.isSaving = false;
            }
        }

        private readonly List<INoteOverlay> NoteViews = new List<INoteOverlay>(64);

        public async Task ShowNotesAsync() => await WPFHelper.Dispatcher.InvokeAsync(async () =>
        {
            this.NoteViews.Clear();

            foreach (var model in this.NoteList)
            {
                if (model.IsDefault)
                {
                    continue;
                }

                if (model.IsWidget)
                {
                    model.ForegroundColor = Note.WidgetForegroundColor;
                }

                var view = !model.IsWidget ?
                    new NoteView() as INoteOverlay :
                    new WidgetView() as INoteOverlay;
                view.Note = model;

                view.ToWindow().Show();

                this.NoteViews.Add(view);
                await Task.Delay(10);
            }
        });

        public async Task CloseNotesAsync() => await WPFHelper.Dispatcher.InvokeAsync(() =>
        {
            foreach (var view in this.NoteViews)
            {
                view.ToWindow().Close();
                this.NoteViews.Remove(view);
            }
        });

        public async Task AddNoteAsync(
            Note parentNote = null,
            bool isWidget = false)
        {
            var note = Note.CreateNew();

            note.IsDefault = false;
            note.IsWidget = isWidget;
            if (isWidget)
            {
                note.Text = "https://www.anoyetta.com";
                note.ForegroundColor = Note.WidgetForegroundColor;
            }

            this.NoteList.Add(note);

            await WPFHelper.Dispatcher.InvokeAsync(() =>
            {
                var view = !isWidget ?
                    new NoteView() as INoteOverlay :
                    new WidgetView() as INoteOverlay;
                view.Note = note;

                var window = view.ToWindow();
                window.Width = !isWidget ?
                    Note.DefaultNoteSize :
                    Note.DefaultWidgetNoteSize;
                window.Height = Note.DefaultNoteSize;

                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                window.Show();

                this.NoteViews.Add(view);
            });

            Notes.Instance.EnqueueSave();
        }

        public async Task RemoveNoteAsync(
            Note note)
        {
            var toRemove = this.NoteViews.FirstOrDefault(x => x.ID == note.ID);

            await WPFHelper.Dispatcher.InvokeAsync(() =>
            {
                if (toRemove != null)
                {
                    toRemove.ToWindow().Close();
                    this.NoteViews.Remove(toRemove);
                }
            });

            this.NoteList.Remove(note);

            Notes.Instance.EnqueueSave();
        }

        public void StartForegroundAppSubscriber()
        {
            if (!ForegroundAppSubscriber.IsAlive)
            {
                ForegroundAppSubscriber.Start();
            }
        }

        private static readonly TimeSpan ForgroundAppSubscribeInterval = TimeSpan.FromSeconds(3);

        private readonly Thread ForegroundAppSubscriber = new Thread(SubscribeForegroundApp)
        {
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
        };

        private static void SubscribeForegroundApp()
        {
            Thread.Sleep(ForgroundAppSubscribeInterval.Add(ForgroundAppSubscribeInterval));

            var isFirst = true;
            while (true)
            {
                try
                {
                    var isFFXIVActiveBack = IsFFXIVActive;

                    if (Config.Instance.IsForceHide)
                    {
                        IsFFXIVActive = false;
                    }
                    else
                    {
                        if (Config.Instance.IsHideWhenNotExistsFFXIV)
                        {
                            RefreshFFXIVIsActive();
                        }
                        else
                        {
                            IsFFXIVActive = true;
                        }
                    }

                    if (IsFFXIVActive != isFFXIVActiveBack ||
                        isFirst)
                    {
                        var value = IsFFXIVActive ?
                            Visibility.Visible :
                            Visibility.Hidden;

                        WPFHelper.Dispatcher.Invoke(() =>
                        {
                            foreach (var view in Notes.Instance.NoteViews)
                            {
                                view.ToWindow().Visibility = value;
                            }
                        },
                        DispatcherPriority.Background);
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(ForgroundAppSubscribeInterval.Add(ForgroundAppSubscribeInterval));
                }
                finally
                {
                    isFirst = false;
                    Thread.Sleep(ForgroundAppSubscribeInterval);
                }
            }
        }

        private static volatile bool IsFFXIVActive;

        private static readonly string[] ActiveTarets = new[]
        {
            "devenv.exe",
            "ffxiv.exe",
            "ffxiv_dx11.exe",
            Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName),
        };

        private static void RefreshFFXIVIsActive()
        {
            try
            {
                // フォアグラウンドWindowのハンドルを取得する
                var hWnd = NativeMethods.GetForegroundWindow();

                // プロセスIDに変換する
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

                // フォアウィンドウのファイル名を取得する
                var p = Process.GetProcessById((int)pid);
                if (p != null)
                {
                    var fileName = Path.GetFileName(
                        p.MainModule.FileName);

                    if (ActiveTarets.Any(x => string.Equals(
                        x,
                        fileName,
                        StringComparison.OrdinalIgnoreCase)))
                    {
                        IsFFXIVActive = true;
                    }
                    else
                    {
                        IsFFXIVActive = false;
                    }
                }
            }
            catch (Win32Exception)
            {
            }
        }
    }
}
