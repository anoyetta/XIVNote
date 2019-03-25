using System;
using System.Xml.Serialization;
using aframe;
using Prism.Mvvm;

namespace XIVNote
{
    public partial class Note : BindableBase
    {
        public static readonly double DefaultNoteSize = 350;

        public static readonly double MinWidth = 200;

        public static readonly double MinHeight = 100;

        public static Note CreateNew()
        {
            var obj = (Notes.Instance.DefaultNote ?? DefaultNoteStyle).Clone();
            obj.ID = Guid.NewGuid();
            obj.Text = string.Empty;
            return obj;
        }

        [XmlIgnore]
        public static readonly Note DefaultNoteStyle = new Note()
        {
            IsDefault = true,
            W = DefaultNoteSize,
            H = DefaultNoteSize,
            ForegroundColorString = "#000000",
            BackgroundColorString = "#fcc800",
            Opacity = 0.9,
            Font = FontInfo.DefaultFont,
        };
    }
}
