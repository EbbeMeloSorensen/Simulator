
using Craft.Logging;
using Craft.ViewModels.Geometry2D.ScrollFree;
using GalaSoft.MvvmLight;

namespace Game.Simple.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _greeting;
        private ILogger _logger;

        public string Greeting
        {
            get => _greeting;
            set
            {
                if (value == _greeting) return;

                _greeting = value;
                RaisePropertyChanged();
            }
        }

        public GeometryEditorViewModel GeometryEditorViewModel { get; }

        public MainWindowViewModel(
            ILogger logger)
        {
            _greeting = "Hej fra Simple Game MainWindowViewModel";

            _logger = logger;
            _logger = null; // Disable logging (it should only be used for debugging purposes)

        }
    }
}
