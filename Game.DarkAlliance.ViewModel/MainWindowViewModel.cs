using Craft.Logging;
using Craft.Math;
using Craft.Simulation;
using Craft.Simulation.Bodies;
using Craft.Simulation.BodyStates;
using Craft.Simulation.Engine;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Craft.ViewModels.Simulation;
using GalaSoft.MvvmLight;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using LineSegment = Craft.Simulation.Boundaries.LineSegment;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ILogger _logger;
        private SceneViewController _sceneViewController;
        private Point3D _cameraPosition;
        private Point3D _lightPosition;
        private Vector3D _lookDirection;

        public Point3D CameraPosition
        {
            get => _cameraPosition;
            set
            {
                _cameraPosition = value;
                RaisePropertyChanged();
            }
        }

        public Point3D LightPosition
        {
            get => _lightPosition;
            set
            {
                _lightPosition = value;
                RaisePropertyChanged();
            }
        }

        public Vector3D LookDirection
        {
            get => _lookDirection;
            set
            {
                _lookDirection = value;
                RaisePropertyChanged();
            }
        }

        private Model3DGroup _scene3D;

        public Model3DGroup Scene3D
        {
            get => _scene3D;
            private set
            {
                _scene3D = value;
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

            Engine.CurrentStateChanged += (s, e) =>
            {
                var bodyStateOfProtagonist = e.State.BodyStates.First() as BodyStateClassic;
                var position = bodyStateOfProtagonist.Position;
                var orientation = bodyStateOfProtagonist.Orientation;

                CameraPosition = new Point3D(
                    -position.Y,
                    0.5,
                    position.X);

                LightPosition = new Point3D(
                    -position.Y,
                    1,
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
            initialState.AddBodyState(
                new BodyStateClassic(new CircularBody(1, ballRadius, 1, false), initialBallPosition)
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

            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack =
                body => OutcomeOfCollisionBetweenBodyAndBoundary.Block;

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

            // Liniestykker defineres i et normalt xy koordinatsystem
            var lineSegments = new List<LineSegment2D>
            {
                new(new Point2D(-2, 2), new Point2D(-2, 0)),
                new(new Point2D(-3, 0), new Point2D(-3, -4)),
                new(new Point2D(-2, 0), new Point2D(-3, 0)),
                new(new Point2D(2, 2), new Point2D(-2, 2)),
                new(new Point2D(2, -2), new Point2D(2, 2)),
                new(new Point2D(-2, -2), new Point2D(2, -2)),
            };

            var group = new Model3DGroup();

            var materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.DarkSlateGray) { Opacity = 0.99 }));
            materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 100));

            foreach (var lineSegment in lineSegments)
            {
                scene.AddBoundary(new LineSegment(
                    new Vector2D(lineSegment.Point1.X, -lineSegment.Point1.Y),
                    new Vector2D(lineSegment.Point2.X, -lineSegment.Point2.Y)));

                var rectangleMesh = CreateWall(
                    new Point2D(lineSegment.Point1.Y, lineSegment.Point1.X),
                    new Point2D(lineSegment.Point2.Y, lineSegment.Point2.X));

                var rectangleModel = new GeometryModel3D(rectangleMesh, materialGroup);
                group.Children.Add(rectangleModel);
            }

            Scene3D = group;



            // Old
            //scene.AddBoundary(new LineSegment(new Vector2D(-2, 2), new Vector2D(2, 2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(2, 2), new Vector2D(2, -2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(2, -2), new Vector2D(-2, -2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-2, 0), new Vector2D(-3, 0)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-3, 0), new Vector2D(-3, 4)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-3, 4), new Vector2D(-6, 4)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-6, 4), new Vector2D(-6, 2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-6, 2), new Vector2D(-14, 2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-14, 2), new Vector2D(-16, 1)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-16, 1), new Vector2D(-16, -1)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-17, -1), new Vector2D(-17, 1)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-17, 1), new Vector2D(-18, 2)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-18, 2), new Vector2D(-18, 3)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-18, 3), new Vector2D(-19, 3)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-19, 4), new Vector2D(-18, 4)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-18, 4), new Vector2D(-17, 5)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-17, 5), new Vector2D(-17, 16)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-16, 18), new Vector2D(-16, 5)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-16, 5), new Vector2D(-14, 4)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-14, 4), new Vector2D(-7, 4)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-7, 4), new Vector2D(-7, 5)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-7, 5), new Vector2D(-2, 5)));
            //scene.AddBoundary(new LineSegment(new Vector2D(-2, 5), new Vector2D(-2, 2)));

            return scene;
        }

        private MeshGeometry3D CreateWall(
            Point2D p1,
            Point2D p2)
        {
            return MeshBuilder.CreateQuad(
                new Point3D(p1.X, 1, p1.Y),
                new Point3D(p2.X, 1, p2.Y),
                new Point3D(p2.X, 0, p2.Y),
                new Point3D(p1.X, 0, p1.Y));
        }
    }
}