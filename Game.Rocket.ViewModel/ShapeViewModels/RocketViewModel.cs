using Craft.ViewModels.Geometry2D.ScrollFree;

namespace Game.Rocket.ViewModel.ShapeViewModels
{
    public class RocketViewModel : RotatableEllipseViewModel
    {
        private static readonly System.Windows.Media.Matrix _correctionMatrix;

        private bool _ignited;

        public bool Ignited
        {
            get => _ignited;
            set
            {
                if (value.Equals(_ignited)) return;
                _ignited = value;
                RaisePropertyChanged();
            }
        }

        private System.Windows.Media.Matrix CorrectionMatrix => _correctionMatrix;

        static RocketViewModel()
        {
            _correctionMatrix = new System.Windows.Media.Matrix(2, 0, 0, 2, -0.05, 0.01);
        }

        protected override void UpdateRotationMatrix()
        {
            var cosAngle = System.Math.Cos(Orientation);
            var sinAngle = System.Math.Sin(Orientation);
            var x = Width / 2;
            var y = Height / 2;

            var T1 = new System.Windows.Media.Matrix(1, 0, 0, 1, -x, -y);
            var R = new System.Windows.Media.Matrix(cosAngle, -sinAngle, sinAngle, cosAngle, 0, 0);
            var T2 = new System.Windows.Media.Matrix(1, 0, 0, 1, x, y);

            RotationMatrix = T1 * CorrectionMatrix * R * T2;
        }
    }
}
