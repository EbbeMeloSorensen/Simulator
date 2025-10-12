using Craft.ViewModels.Geometry2D.ScrollFree;

namespace Game.Zelda.ViewModel.ShapeViewModels
{
    public class ZeldaViewModel : EllipseViewModel
    {
        private static readonly System.Windows.Media.Matrix _correctionMatrix;

        private System.Windows.Media.Matrix CorrectionMatrix => _correctionMatrix;

        public System.Windows.Media.Matrix LocalTransformationMatrix { get; }

        static ZeldaViewModel()
        {
            //_correctionMatrix = new System.Windows.Media.Matrix(1.8, 0, 0, 1.8, 0.0, -0.03);
            _correctionMatrix = new System.Windows.Media.Matrix(3.6, 0, 0, 3.6, 0.0, -0.03);
        }

        public ZeldaViewModel(
            double width,
            double height)
        {
            Width = width;
            Height = height;

            var x = Width / 2;
            var y = Height / 2;

            var T1 = new System.Windows.Media.Matrix(1, 0, 0, 1, -x, -y);
            var T2 = new System.Windows.Media.Matrix(1, 0, 0, 1, x, y);

            LocalTransformationMatrix = T1 * CorrectionMatrix * T2;
        }
    }
}
