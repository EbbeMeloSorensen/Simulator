using Craft.Logging;
using Craft.Math;
using Craft.Simulation;
using Craft.Simulation.Bodies;
using Craft.Simulation.BodyStates;
using Craft.Simulation.Boundaries;
using Craft.Simulation.Engine;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Craft.ViewModels.Simulation;
using GalaSoft.MvvmLight;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ILogger _logger;
        private SceneViewController _sceneViewController;
        private System.Windows.Media.Media3D.Point3D _cameraPosition;
        private System.Windows.Media.Media3D.Vector3D _lookDirection;

        public System.Windows.Media.Media3D.Point3D CameraPosition
        {
            get => _cameraPosition;
            set
            {
                _cameraPosition = value;
                RaisePropertyChanged();
            }
        }

        public System.Windows.Media.Media3D.Vector3D LookDirection
        {
            get => _lookDirection;
            set
            {
                _lookDirection = value;
                RaisePropertyChanged();
            }
        }

        public Engine Engine { get; }
        public GeometryEditorViewModel GeometryEditorViewModel { get; }

        public MainWindowViewModel(
            ILogger logger)
        {
            _logger = logger;
            _logger = null; // Disable logging (it should only be used for debugging purposes)

            Engine = new Engine(_logger);

            GeometryEditorViewModel = new GeometryEditorViewModel(1)
            {
                UpdateModelCallBack = Engine.UpdateModel
            };

            // Bemærk de følgende 2 callbacks, som bruges til at give kuglen et andet skin end det, som er default.
            // Pågældende skin er taget fra Craft, men man kan også override, ligesom det er gjort for Flappybird, Rocket og Zelda
            ShapeSelectorCallback shapeSelectorCallback = (bs) =>
            {
                if (!(bs.Body is CircularBody))
                {
                    throw new InvalidOperationException();
                }

                var circularBody = bs.Body as CircularBody;

                var bsc = bs as BodyStateClassic;
                var orientation = bsc == null ? 0 : bsc.Orientation;

                return new RotatableEllipseViewModel
                {
                    Width = 2 * circularBody.Radius,
                    Height = 2 * circularBody.Radius,
                    Orientation = orientation
                };
            };

            ShapeUpdateCallback shapeUpdateCallback = (shapeViewModel, bs) =>
            {
                // Her opdaterer vi POSITIONEN af shapeviewmodellen
                shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);

                // Her opdaterer vi ORIENTERINGEN af shapeviewmodellen
                if (shapeViewModel is RotatableEllipseViewModel)
                {
                    var bsc = bs as BodyStateClassic;
                    var orientation = bsc == null ? 0 : bsc.Orientation;

                    var rotatableEllipseViewModel = shapeViewModel as RotatableEllipseViewModel;
                    rotatableEllipseViewModel.Orientation = orientation;
                }
            };

            _sceneViewController = new SceneViewController(
                Engine,
                GeometryEditorViewModel,
                shapeSelectorCallback,
                shapeUpdateCallback);

            var scene = GenerateScene();

            GeometryEditorViewModel.InitializeWorldWindow(
                scene.InitialWorldWindowFocus(),
                scene.InitialWorldWindowSize(),
                false);

            _sceneViewController.ActiveScene = scene;

            //CameraPosition = new Point3D(0, 1, 2);
            //LookDirection = new Vector3D(0, 0, -1);

            Engine.CurrentStateChanged += (s, e) =>
            {
                var bodyStateOfProtagonist = e.State.BodyStates.First() as BodyStateClassic;
                var position = bodyStateOfProtagonist.Position;
                var orientation = bodyStateOfProtagonist.Orientation;

                CameraPosition = new Point3D(
                    -position.Y,
                    0.5,
                    position.X);

                LookDirection = new Vector3D(Math.Sin(orientation), 0, Math.Cos(orientation));
            };
        }

        public void HandleLoaded()
        {
            _sceneViewController.ResetScene();
            Engine.StartOrResumeAnimation();
        }

        private Scene GenerateScene()
        {
            var ballRadius = 0.16;
            var initialBallPosition = new Vector2D(0, 0);

            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new CircularBody(1, ballRadius, 1, false), initialBallPosition)
            {
                Orientation = Math.PI
            });

            var name = "Exploration";
            var standardGravity = 0.0;
            var initialWorldWindowUpperLeft = new Point2D(-1.4, -1.3);
            var initialWorldWindowLowerRight = new Point2D(5, 3);
            var gravitationalConstant = 0.0;
            var coefficientOfFriction = 0.0;
            var timeFactor = 1.0;
            var handleBodyCollisions = false;
            var deltaT = 0.001;
            var viewMode = SceneViewMode.FocusOnFirstBody;

            var scene = new Scene(
                name,
                initialWorldWindowUpperLeft,
                initialWorldWindowLowerRight,
                initialState,
                standardGravity,
                gravitationalConstant,
                coefficientOfFriction,
                timeFactor,
                handleBodyCollisions,
                deltaT,
                viewMode);

            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack = body => OutcomeOfCollisionBetweenBodyAndBoundary.Block;

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                var currentStateOfMainBody = currentState.BodyStates.First() as BodyStateClassic;
                var currentRotationalSpeed = currentStateOfMainBody.RotationalSpeed;
                var currentArtificialSpeed = currentStateOfMainBody.ArtificialVelocity.Length;

                var newRotationalSpeed = 0.0;

                if (keyboardState.LeftArrowDown)
                {
                    newRotationalSpeed += Math.PI;
                }

                if (keyboardState.RightArrowDown)
                {
                    newRotationalSpeed -= Math.PI;
                }

                var newArtificialSpeed = 0.0;

                if (keyboardState.UpArrowDown)
                {
                    newArtificialSpeed += 1.5;
                }

                if (keyboardState.DownArrowDown)
                {
                    newArtificialSpeed -= 1.5;
                }

                currentStateOfMainBody.RotationalSpeed = newRotationalSpeed;
                currentStateOfMainBody.ArtificialVelocity = new Vector2D(newArtificialSpeed, 0);

                if (Math.Abs(newRotationalSpeed - currentRotationalSpeed) < 0.01 &&
                    Math.Abs(newArtificialSpeed - currentArtificialSpeed) < 0.01)
                {
                    return false;
                }

                return true;
            };

            scene.AddBoundary(new LineSegment(new Vector2D(-2, 2), new Vector2D(2, 2)));
            scene.AddBoundary(new LineSegment(new Vector2D(2, 2), new Vector2D(2, -2)));
            scene.AddBoundary(new LineSegment(new Vector2D(2, -2), new Vector2D(-2, -2)));
            scene.AddBoundary(new LineSegment(new Vector2D(-2, -2), new Vector2D(-2, 0)));
            scene.AddBoundary(new LineSegment(new Vector2D(-2, 0), new Vector2D(-3, 0)));
            scene.AddBoundary(new LineSegment(new Vector2D(-3, 0), new Vector2D(-3, 4)));
            scene.AddBoundary(new LineSegment(new Vector2D(-3, 4), new Vector2D(-6, 4)));
            scene.AddBoundary(new LineSegment(new Vector2D(-6, 4), new Vector2D(-6, 2)));
            scene.AddBoundary(new LineSegment(new Vector2D(-6, 2), new Vector2D(-14, 2)));
            scene.AddBoundary(new LineSegment(new Vector2D(-14, 2), new Vector2D(-16, 1)));
            scene.AddBoundary(new LineSegment(new Vector2D(-16, 1), new Vector2D(-16, -1)));
            scene.AddBoundary(new LineSegment(new Vector2D(-17, -1), new Vector2D(-17, 1)));
            scene.AddBoundary(new LineSegment(new Vector2D(-17, 1), new Vector2D(-18, 2)));
            scene.AddBoundary(new LineSegment(new Vector2D(-18, 2), new Vector2D(-18, 3)));
            scene.AddBoundary(new LineSegment(new Vector2D(-18, 3), new Vector2D(-19, 3)));
            scene.AddBoundary(new LineSegment(new Vector2D(-19, 4), new Vector2D(-18, 4)));
            scene.AddBoundary(new LineSegment(new Vector2D(-18, 4), new Vector2D(-17, 5)));
            scene.AddBoundary(new LineSegment(new Vector2D(-17, 5), new Vector2D(-17, 16)));
            scene.AddBoundary(new LineSegment(new Vector2D(-16, 18), new Vector2D(-16, 5)));
            scene.AddBoundary(new LineSegment(new Vector2D(-16, 5), new Vector2D(-14, 4)));
            scene.AddBoundary(new LineSegment(new Vector2D(-14, 4), new Vector2D(-7, 4)));
            scene.AddBoundary(new LineSegment(new Vector2D(-7, 4), new Vector2D(-7, 5)));
            scene.AddBoundary(new LineSegment(new Vector2D(-7, 5), new Vector2D(-2, 5)));
            scene.AddBoundary(new LineSegment(new Vector2D(-2, 5), new Vector2D(-2, 2)));

            return scene;
        }
    }
}
