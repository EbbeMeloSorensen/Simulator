using Craft.ViewModels.Geometry2D.ScrollFree;

namespace Simulator.Laboratory.ViewModel.ShapeViewModels
{
    public class WalkerViewModel : RectangleViewModel
    {
        private string _imagePath;
        private double _scaleX;

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                RaisePropertyChanged();
            }
        }

        public double ScaleX
        {
            get => _scaleX;
            set
            {
                _scaleX = value;
                RaisePropertyChanged();
            }
        }
    }
}
