using Prism.Mvvm;

namespace XIVNote.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public Config Config => Config.Instance;
    }
}
