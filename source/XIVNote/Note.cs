using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using aframe;
using Microsoft.Win32;
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

        private readonly SuspendableObservableCollection<NoteImage> ImageList = new SuspendableObservableCollection<NoteImage>();

        [XmlElement(ElementName = "Image")]
        public SuspendableObservableCollection<NoteImage> Images
        {
            get => this.ImageList;
            set
            {
                this.ImageList.Clear();
                this.ImageList.AddRange(value);
            }
        }

        #region AddImage

        private static readonly OpenFileDialog OpenFileDialog = new OpenFileDialog()
        {
            RestoreDirectory = true,
            Filter = "Image Files (*.bmp; *.jpeg; *.jpg; *.gif; *.png)|*.bmp;*.jpeg;*.jpg;`*.gif;*.png|All Files (*.*)|*.*",
            FilterIndex = 1,
        };

        private DelegateCommand addImageCommand;

        public DelegateCommand AddImageCommand =>
            this.addImageCommand ?? (this.addImageCommand = new DelegateCommand(this.ExecuteAddImageCommand));

        private async void ExecuteAddImageCommand()
        {
            OpenFileDialog.InitialDirectory = Config.Instance.ImageFileDirectory;

            var result = OpenFileDialog.ShowDialog(WPFHelper.MainWindow) ?? false;
            if (!result)
            {
                return;
            }

            var fileName = OpenFileDialog.FileName;

            Config.Instance.ImageFileDirectory = Path.GetFileName(fileName);

            var storeFileName = $"{Guid.NewGuid()}.png";
            var storeFilePath = Path.Combine(@".\images", storeFileName);

            // PNGŒ`Ž®‚É•ÏŠ·‚µ‚Ä•Û‘¶‚·‚é
            await Task.Run(() =>
            {
                using (var ms = new WrappingStream(new MemoryStream(File.ReadAllBytes(fileName))))
                {
                    var img = System.Drawing.Image.FromStream(ms);
                    img.Save(storeFilePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            });

            this.Images.Add(new NoteImage()
            {
                FileName = storeFileName
            });
        }

        #endregion AddImage

        #region RemoveImage

        private DelegateCommand<string> removeImageCommand;

        public DelegateCommand<string> RemoveImageCommand =>
            this.removeImageCommand ?? (this.removeImageCommand = new DelegateCommand<string>(this.ExecuteRemoveImageCommand));

        private void ExecuteRemoveImageCommand(
            string imageFileName)
        {
            var toRemove = this.Images.FirstOrDefault(x => x.FileName == imageFileName);
            if (toRemove != null)
            {
                this.Images.Remove(toRemove);
            }
        }

        #endregion RemoveImage

        #region Close

        private DelegateCommand closeCommand;

        [XmlIgnore]
        public DelegateCommand CloseCommand =>
            this.closeCommand ?? (this.closeCommand = new DelegateCommand(this.ExecuteCloseCommand));

        private async void ExecuteCloseCommand() => await Notes.Instance.RemoveNoteAsync(this);

        #endregion Close
    }

    [Serializable]
    public class NoteImage : BindableBase
    {
        private string fileName;

        [XmlAttribute(AttributeName = "File")]
        public string FileName
        {
            get => this.fileName;
            set
            {
                if (this.SetProperty(ref this.fileName, value))
                {
                    this.CreateImageSource();
                }
            }
        }

        private ImageSource imageSource;

        [XmlIgnore]
        public ImageSource ImageSource
        {
            get => this.imageSource;
            set => this.SetProperty(ref this.imageSource, value);
        }

        private void CreateImageSource()
        {
            var path = Path.Combine(
                @".\images",
                this.FileName);

            if (File.Exists(path))
            {
                var img = default(ImageSource);
                using (var ms = new WrappingStream(new MemoryStream(File.ReadAllBytes(path))))
                {
                    img = new WriteableBitmap(BitmapFrame.Create(ms));
                }

                img.Freeze();
                this.ImageSource = img;
            }
        }
    }
}
