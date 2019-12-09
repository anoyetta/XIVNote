using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using aframe;
using aframe.Updater;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace XIVNote
{
    public partial class Config : JsonConfigBase
    {
        #region Lazy Singleton

        private readonly static Lazy<Config> instance = new Lazy<Config>(Load);

        public static Config Instance => instance.Value;

        public Config()
        {
        }

        #endregion Lazy Singleton

        public static string FileName => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.config.json");

        public static Config Load()
        {
            MigrateConfig(FileName);

            var config = Config.Load<Config>(
                FileName,
                out bool isFirstLoad);

            return config;
        }

        public void Save() => this.Save(FileName);

        private volatile bool isSaving;

        public void StartAutoSave()
        {
            this.PropertyChanged += async (_, __) =>
            {
                if (this.isSaving)
                {
                    return;
                }

                this.isSaving = true;

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await Task.Run(() => this.Save(FileName));
                }
                finally
                {
                    this.isSaving = false;
                }
            };
        }

        #region Migration

        /// <summary>
        /// バージョンアップ等による設定ファイルの追加、変更を反映する
        /// </summary>
        private static void MigrateConfig(
            string fileName)
        {
            // NO-OP
        }

        #endregion Migration

        #region Data

        [JsonIgnore]
        public string AppName => Assembly.GetExecutingAssembly().GetTitle();

        [JsonIgnore]
        public string AppNameWithVersion => $"{this.AppName} - {this.AppVersionString}";

        [JsonIgnore]
        public Version AppVersion => Assembly.GetExecutingAssembly().GetVersion();

        [JsonIgnore]
        public ReleaseChannels AppReleaseChannel => Assembly.GetExecutingAssembly().GetReleaseChannels();

        [JsonIgnore]
        public string AppVersionString => $"v{this.AppVersion.ToString()}";

        private double scale = 1.0;

        [JsonProperty(PropertyName = "scale")]
        public double Scale
        {
            get => this.scale;
            set => this.SetProperty(ref this.scale, Math.Round(value, 2));
        }

        private double x;

        [JsonProperty(PropertyName = "X")]
        public double X
        {
            get => this.x;
            set => this.SetProperty(ref this.x, Math.Round(value, 1));
        }

        private double y;

        [JsonProperty(PropertyName = "Y")]
        public double Y
        {
            get => this.y;
            set => this.SetProperty(ref this.y, Math.Round(value, 1));
        }

        private double w = DefaultWidth;

        [JsonProperty(PropertyName = "W")]
        public double W
        {
            get => this.w;
            set
            {
                if (App.Current.MainWindow?.WindowState != WindowState.Minimized)
                {
                    this.SetProperty(ref this.w, Math.Round(value, 1));
                }
            }
        }

        private double h = DefaultHeight;

        [JsonProperty(PropertyName = "H")]
        public double H
        {
            get => this.h;
            set
            {
                if (App.Current.MainWindow?.WindowState != WindowState.Minimized)
                {
                    this.SetProperty(ref this.h, Math.Round(value, 1));
                }
            }
        }

        private bool isForceHide;

        [JsonProperty(PropertyName = "is_force_hide")]
        public bool IsForceHide
        {
            get => this.isForceHide;
            set => this.SetProperty(ref this.isForceHide, value);
        }

        private bool isStartupWithWindows;

        [JsonProperty(PropertyName = "is_startup_with_windows")]
        public bool IsStartupWithWindows
        {
            get => this.isStartupWithWindows;
            set
            {
                if (this.SetProperty(ref this.isStartupWithWindows, value))
                {
                    this.SetStartup(value);
                }
            }
        }

        public async void SetStartup(
            bool isStartup) =>
            await Task.Run(() =>
            {
                using (var regkey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    true))
                {
                    if (isStartup)
                    {
                        regkey.SetValue(
                            Assembly.GetExecutingAssembly().GetProduct(),
                            $"\"{Assembly.GetExecutingAssembly().Location}\"");
                    }
                    else
                    {
                        regkey.DeleteValue(
                            Assembly.GetExecutingAssembly().GetProduct(),
                            false);
                    }
                }
            });

        private bool isMinimizeStartup;

        [JsonProperty(PropertyName = "is_minimize_startup")]
        public bool IsMinimizeStartup
        {
            get => this.isMinimizeStartup;
            set => this.SetProperty(ref this.isMinimizeStartup, value);
        }

        private bool isHideWhenNotExistsFFXIV;

        [JsonProperty(PropertyName = "is_hide_when_not_exists_ffxiv")]
        public bool IsHideWhenNotExistsFFXIV
        {
            get => this.isHideWhenNotExistsFFXIV;
            set => this.SetProperty(ref this.isHideWhenNotExistsFFXIV, value);
        }

        private string imageFileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        [JsonProperty(PropertyName = "image_file_directory")]
        public string ImageFileDirectory
        {
            get => this.imageFileDirectory;
            set => this.SetProperty(ref this.imageFileDirectory, value);
        }

        #endregion Data
    }
}
