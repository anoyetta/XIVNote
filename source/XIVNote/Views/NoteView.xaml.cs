using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using aframe.Views;
using XIVNote.ViewModels;

namespace XIVNote.Views
{
    /// <summary>
    /// NoteView.xaml の相互作用ロジック
    /// </summary>
    public partial class NoteView : Window, INoteOverlay
    {
        public NoteView()
        {
            this.InitializeComponent();

            this.MinWidth = Note.MinWidth;
            this.MinHeight = Note.MinHeight;

            this.Image.MouseLeftButtonDown += (_, __) => this.DragMove();
            this.ToolBarGrid.MouseLeftButtonDown += (_, __) => this.DragMove();

            this.MouseEnter += (_, __) => this.ToolBarGrid.Visibility = Visibility.Visible;
            this.MouseLeave += (_, __) => this.ToolBarGrid.Visibility = Visibility.Collapsed;

            this.Loaded += (_, __) =>
            {
                this.ViewModel.Model.PropertyChanged += this.Model_PropertyChanged;

                this.InitializeOverlayVisible(
                    ref this.overlayVisible,
                    this.ViewModel.Model?.IsVisible ?? true);

                this.ToolBarGrid.Visibility = this.IsMouseOver ?
                    Visibility.Visible :
                    Visibility.Collapsed;

                this.SubscribeZOrderCorrector();
            };

            this.Closed += (_, __) =>
            {
                this.ViewModel.Model.PropertyChanged -= this.Model_PropertyChanged;
            };

            this.LeftThumb.DragDelta += (_, e) =>
            {
                this.Left += e.HorizontalChange;
                this.Width -= e.HorizontalChange;
            };

            this.RightThumb.DragDelta += (_, e) =>
            {
                this.Width += e.HorizontalChange;
            };

            this.TopThumb.DragDelta += (_, e) =>
            {
                this.Top += e.VerticalChange;
                this.Height -= e.VerticalChange;
            };

            this.BottomThumb.DragDelta += (_, e) =>
            {
                this.Height += e.VerticalChange;
            };
        }

        public NoteViewModel ViewModel => this.DataContext as NoteViewModel;

        public Guid ID => this.ViewModel?.Model?.ID ?? Guid.Empty;

        public Note Note
        {
            get => this.ViewModel.Model;
            set => this.ViewModel.Model = value;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var model = sender as Note;

            switch (e.PropertyName)
            {
                case nameof(Note.IsVisible):
                    this.OverlayVisible = model.IsVisible;
                    break;

                default:
                    break;
            }

            NoteViewModel.IsSaveQueue = true;
        }

        private void NoteTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => this.ViewModel.Model.Text = this.NoteTextBox.Text;

        #region IOverlay

        public int ZOrder => 0;

        private bool overlayVisible;

        public bool OverlayVisible
        {
            get => this.overlayVisible;
            set => this.SetOverlayVisible(ref this.overlayVisible, value);
        }

        #endregion IOverlay
    }
}
