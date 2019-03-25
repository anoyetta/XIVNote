using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using aframe;
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

        public void Load() => this.Load(FileName);

        public void Load(
            string fileName)
        {
            lock (this)
            {
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
                }
                finally
                {
                    if (!this.NoteList.Any(x => x.IsDefault))
                    {
                        this.NoteList.Add(Note.DefaultNoteStyle);
                    }
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

        private readonly List<NoteView> NoteViews = new List<NoteView>(64);

        public async Task ShowNotesAsync() => await WPFHelper.Dispatcher.InvokeAsync(async () =>
        {
            this.NoteViews.Clear();

            foreach (var model in this.NoteList)
            {
                if (model.IsDefault)
                {
                    continue;
                }

                var view = new NoteView();
                view.ViewModel.Model = model;

                view.Show();

                this.NoteViews.Add(view);
                await Task.Delay(10);
            }
        });

        public async Task CloseNotesAsync() => await WPFHelper.Dispatcher.InvokeAsync(() =>
        {
            foreach (var view in this.NoteViews)
            {
                view.Close();
                this.NoteViews.Remove(view);
            }
        });

        public async Task AddNoteAsync(
            Note note = null)
        {
            if (note == null)
            {
                note = Note.CreateNew();
            }

            note.IsDefault = false;
            this.NoteList.Add(note);

            await WPFHelper.Dispatcher.InvokeAsync(() =>
            {
                var view = new NoteView();
                view.ViewModel.Model = note;

                view.Width = Note.DefaultNoteSize;
                view.Height = Note.DefaultNoteSize;
                view.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                view.Show();

                this.NoteViews.Add(view);
            });

            await Task.Run(this.Save);
        }

        public async Task RemoveNoteAsync(
            Note note)
        {
            var toRemove = this.NoteViews.FirstOrDefault(x => x.ID == note.ID);

            await WPFHelper.Dispatcher.InvokeAsync(() =>
            {
                if (toRemove != null)
                {
                    toRemove.Close();
                    this.NoteViews.Remove(toRemove);
                }
            });

            this.NoteList.Remove(note);

            await Task.Run(this.Save);
        }
    }
}
