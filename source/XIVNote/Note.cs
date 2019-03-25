using System;
using System.Windows.Media;
using System.Xml.Serialization;
using aframe;
using Prism.Commands;
using Prism.Mvvm;

namespace XIVNote
{
    [Serializable]
    public partial class Note : BindableBase
    {
        [XmlIgnore]
        public Guid ID { get; private set; } = Guid.NewGuid();

        private double x;

        [XmlAttribute]
        public double X
        {
            get => this.x;
            set => this.SetProperty(ref this.x, Math.Round(value, 1));
        }

        private double y;

        [XmlAttribute]
        public double Y
        {
            get => this.y;
            set => this.SetProperty(ref this.y, Math.Round(value, 1));
        }

        private double w;

        [XmlAttribute]
        public double W
        {
            get => this.w;
            set => this.SetProperty(ref this.w, Math.Round(value, 1));
        }

        private double h;

        [XmlAttribute]
        public double H
        {
            get => this.h;
            set => this.SetProperty(ref this.h, Math.Round(value, 1));
        }

        private Color foregroundColor = Colors.Black;

        [XmlIgnore]
        public Color ForegroundColor
        {
            get => this.foregroundColor;
            set
            {
                if (this.SetProperty(ref this.foregroundColor, value))
                {
                    this.RaisePropertyChanged(nameof(this.ForegroundBrush));
                }
            }
        }

        [XmlIgnore]
        public SolidColorBrush ForegroundBrush
        {
            get
            {
                var brush = new SolidColorBrush(this.foregroundColor);
                brush.Freeze();
                return brush;
            }
        }

        [XmlAttribute("Foreground")]
        public string ForegroundColorString
        {
            get => this.foregroundColor.ToString();
            set => this.ForegroundColor = (Color)ColorConverter.ConvertFromString(value);
        }

        private Color backgroundColor = Colors.Black;

        [XmlIgnore]
        public Color BackgroundColor
        {
            get => this.backgroundColor;
            set
            {
                if (this.SetProperty(ref this.backgroundColor, value))
                {
                    this.RaisePropertyChanged(nameof(this.BackgroundBrush));
                }
            }
        }

        [XmlIgnore]
        public SolidColorBrush BackgroundBrush
        {
            get
            {
                var brush = new SolidColorBrush(this.backgroundColor);
                brush.Opacity = this.opacity;
                brush.Freeze();
                return brush;
            }
        }

        [XmlAttribute("Background")]
        public string BackgroundColorString
        {
            get => this.backgroundColor.ToString();
            set => this.BackgroundColor = (Color)ColorConverter.ConvertFromString(value);
        }

        private double opacity = 0.60d;

        [XmlAttribute]
        public double Opacity
        {
            get => this.opacity;
            set
            {
                if (this.SetProperty(ref this.opacity, value))
                {
                    this.RaisePropertyChanged(nameof(this.BackgroundBrush));
                }
            }
        }

        private bool isVisible = true;

        [XmlAttribute]
        public bool IsVisible
        {
            get => this.isVisible;
            set => this.SetProperty(ref this.isVisible, value);
        }

        private bool isReadOnly;

        [XmlAttribute]
        public bool IsReadOnly
        {
            get => this.isReadOnly;
            set => this.SetProperty(ref this.isReadOnly, value);
        }

        private bool isPositionLocked;

        [XmlAttribute]
        public bool IsPositionLocked
        {
            get => this.isPositionLocked;
            set => this.SetProperty(ref this.isPositionLocked, value);
        }

        private bool isDefault = false;

        [XmlAttribute]
        public bool IsDefault
        {
            get => this.isDefault;
            set => this.SetProperty(ref this.isDefault, value);
        }

        private FontInfo font = (FontInfo)FontInfo.DefaultFont.Clone();

        public FontInfo Font
        {
            get => this.font;
            set => this.SetProperty(ref this.font, value);
        }

        private string text;

        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value);
        }

        #region Close

        private DelegateCommand closeCommand;

        [XmlIgnore]
        public DelegateCommand CloseCommand =>
            this.closeCommand ?? (this.closeCommand = new DelegateCommand(this.ExecuteCloseCommand));

        private async void ExecuteCloseCommand() => await Notes.Instance.RemoveNoteAsync(this);

        #endregion Close
    }
}
