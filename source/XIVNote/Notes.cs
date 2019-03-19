using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using aframe;

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

        public static string FileName => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.xml");

        public void Load() => this.Load(FileName);

        public void Load(
            string fileName)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            lock (this)
            {
                using (var sr = new StreamReader(fileName, new UTF8Encoding(false)))
                {
                    if (sr.BaseStream.Length <= 0)
                    {
                        return;
                    }

                    var xs = new XmlSerializer(this.NoteList.GetType());
                    var data = xs.Deserialize(sr) as IEnumerable<Note>;

                    if (data != null)
                    {
                        this.NoteList.AddRange(data, true);
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
                    var xs = new XmlSerializer(this.NoteList.GetType(), new XmlRootAttribute("notes"));
                    xs.Serialize(sw, this.NoteList, ns);
                }

                sb.Replace("utf-16", "utf-8");

                File.WriteAllText(
                    fileName,
                    sb.ToString() + Environment.NewLine,
                    new UTF8Encoding(false));
            }
        }
    }
}
