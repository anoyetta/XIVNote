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
        public void SetAutoSave()
        {
            this.PropertyChanged += (_, __) => Notes.Instance.EnqueueSave();

            foreach (var image in this.ImageList)
            {
                image.SetAutoSave();
            }
        }

        [XmlIgnore]
        public Guid ID { get; private set; } = Guid.NewGuid();

        private string name;

        [XmlAttribute]
        public string Name
        {
            get => this.name;
            set
            {
                if (this.SetProperty(ref this.name, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsExistsName));
                }
            }
        }

        [XmlIgnore]
        public bool IsExistsName => !string.IsNullOrEmpty(this.Name);

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

        private bool isLock;

        [XmlAttribute]
        public bool IsLock
        {
            get => this.isLock;
            set => this.SetProperty(ref this.isLock, value);
        }

        [XmlIgnore]
        public static readonly Color WidgetForegroundColor = (Color)ColorConverter.ConvertFromString("#2b2b2b");

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
            set
            {
                if (this.SetProperty(ref this.text, value))
                {
                    this.RaisePropertyChanged(nameof(this.IsBlank));
                }
            }
        }

        [XmlIgnore]
        public bool IsBlank => string.IsNullOrEmpty(this.Text);

        private bool isWidget;

        [XmlAttribute]
        public bool IsWidget
        {
            get => this.isWidget;
            set => this.SetProperty(ref this.isWidget, value);
        }

        private readonly SuspendableObservableCollection<NoteImage> ImageList = new SuspendableObservableCollection<NoteImage>();

        [XmlIgnore]
        public SuspendableObservableCollection<NoteImage> Images
        {
            get => this.ImageList;
            set
            {
                this.ImageList.Clear();

                if (value != null)
                {
                    foreach (var item in value)
                    {
                        item.Parent = this;
                        this.ImageList.Add(item);
                    }
                }
            }
        }

        [XmlElement(ElementName = "Image")]
        public NoteImage[] ImagesXML
        {
            get => this.ImageList.ToArray();
            set
            {
                this.ImageList.Clear();

                if (value != null)
                {
                    foreach (var item in value)
                    {
                        item.Parent = this;
                        this.ImageList.Add(item);
                    }
                }
            }
        }

        #region AddImage

        private static readonly Lazy<OpenFileDialog> LazyOpenFileDialog = new Lazy<OpenFileDialog>(() =>
        {
            var dialog = default(OpenFileDialog);

            WPFHelper.Dispatcher.Invoke(() =>
            {
                dialog = new OpenFileDialog()
                {
                    RestoreDirectory = true,
                    Filter = "Image Files (*.bmp; *.jpeg; *.jpg; *.gif; *.png)|*.bmp;*.jpeg;*.jpg;`*.gif;*.png|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = true,
                };
            });

            return dialog;
        });

        private DelegateCommand addImageCommand;

        public DelegateCommand AddImageCommand =>
            this.addImageCommand ?? (this.addImageCommand = new DelegateCommand(this.ExecuteAddImageCommand));

        private async void ExecuteAddImageCommand()
        {
            LazyOpenFileDialog.Value.InitialDirectory = Config.Instance.ImageFileDirectory;
            LazyOpenFileDialog.Value.FileName = string.Empty;

            var result = await WPFHelper.Dispatcher.InvokeAsync(() =>
                LazyOpenFileDialog.Value.ShowDialog(WPFHelper.MainWindow) ?? false);
            if (!result)
            {
                return;
            }

            foreach (var fileName in LazyOpenFileDialog.Value.FileNames)
            {
                Config.Instance.ImageFileDirectory = Path.GetFileName(fileName);

                var storeFileName = $"{Guid.NewGuid()}.png";
                var storeFilePath = Path.Combine(@".\images", storeFileName);

                // PNG形式に変換して保存する
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
                    Parent = this,
                    FileName = storeFileName
                });
            }

            Notes.Instance.EnqueueSave();
        }

        #endregion AddImage

        #region RemoveImage

        private DelegateCommand<NoteImage> removeImageCommand;

        public DelegateCommand<NoteImage> RemoveImageCommand =>
            this.removeImageCommand ?? (this.removeImageCommand = new DelegateCommand<NoteImage>(this.ExecuteRemoveImageCommand));

        private void ExecuteRemoveImageCommand(
            NoteImage image)
        {
            if (image == null)
            {
                return;
            }

            this.Images.Remove(image);

            Notes.Instance.EnqueueSave();
        }

        #endregion RemoveImage

        #region Refresh Callback

        [XmlIgnore]
        public Action RefreshCallback { get; set; }

        private DelegateCommand refreshCommand;

        public DelegateCommand RefreshCommand =>
            this.refreshCommand ?? (this.refreshCommand = new DelegateCommand(this.ExecuteRefreshCommand));

        private async void ExecuteRefreshCommand()
        {
            if (!this.IsWidget)
            {
                return;
            }

            await Task.Run(() => this.RefreshCallback?.Invoke());
        }

        #endregion Refresh Callback

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
        public void SetAutoSave()
        {
            this.PropertyChanged += (_, __) => Notes.Instance.EnqueueSave();
        }

        [XmlIgnore]
        public Note Parent { get; set; }

        private string fileName;

        [XmlAttribute(AttributeName = "File")]
        public string FileName
        {
            get => this.fileName;
            set => this.SetProperty(ref this.fileName, value);
        }

        private ImageSource imageSource;

        [XmlIgnore]
        public ImageSource ImageSource => this.imageSource ?? (this.imageSource = this.CreateImageSource());

        [XmlIgnore]
        public DelegateCommand<NoteImage> RemoveImageCommand => this.Parent?.RemoveImageCommand;

        private ImageSource CreateImageSource()
        {
            if (string.IsNullOrEmpty(this.FileName))
            {
                return null;
            }

            var img = default(ImageSource);

            var path = Path.Combine(
                @".\images",
                this.FileName);

            if (File.Exists(path))
            {
                using (var ms = new WrappingStream(new MemoryStream(File.ReadAllBytes(path))))
                {
                    img = new WriteableBitmap(BitmapFrame.Create(ms));
                }

                img.Freeze();
            }

            return img;
        }
    }
}
