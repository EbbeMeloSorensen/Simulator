using System.Windows.Media.Media3D;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using Craft.Math;
using Craft.Simulation;
using Craft.Simulation.Bodies;
using Craft.Simulation.BodyStates;
using Craft.Simulation.Engine;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Craft.ViewModels.Simulation;
using Craft.Utils.Linq;
using Game.DarkAlliance.ViewModel.Bodies;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure;
using LineSegment = Craft.Simulation.Boundaries.LineSegment;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ISceneRenderer _sceneRenderer;

        private DispatcherTimer _timer;
        private double _angle = 0;

        private SceneViewController _sceneViewController;
        private Point3D _cameraPosition;
        private Point3D _playerLightPosition;
        private Vector3D _lookDirection;
        private Vector3D _directionalLight1;
        private Vector3D _directionalLight2;
        private string _outcome;

        public Point3D CameraPosition
        {
            get => _cameraPosition;
            set
            {
                _cameraPosition = value;
                RaisePropertyChanged();
            }
        }

        public Point3D PlayerLightPosition
        {
            get => _playerLightPosition;
            set
            {
                _playerLightPosition = value;
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

        public Vector3D DirectionalLight1
        {
            get => _directionalLight1;
            set
            {
                _directionalLight1 = value;
                RaisePropertyChanged();
            }
        }

        public Vector3D DirectionalLight2
        {
            get => _directionalLight2;
            set
            {
                _directionalLight2 = value;
                RaisePropertyChanged();
            }
        }

        public string Outcome
        {
            get { return _outcome; }
            set
            {
                _outcome = value;
                RaisePropertyChanged();
            }
        }

        private Model3D _scene3D;

        public Model3D Scene3D
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

        public MainWindowViewModel()
        {
            _sceneRenderer = new SceneRenderer();

            Engine = new Engine(null);

            Engine.AnimationCompleted += (s, e) =>
            {
                Outcome = Engine.EngineCore.Outcome as string;
            };

            GeometryEditorViewModel = new GeometryEditorViewModel()
            {
                UpdateModelCallBack = Engine.UpdateModel
            };

            // Bemærk de følgende 2 callbacks, som bruges til at give bodies et andet skin end det, som er default.
            // Pågældende skin er taget fra Craft, men man kan også override, ligesom det er gjort for Flappybird, Rocket og Zelda
            ShapeSelectorCallback shapeSelectorCallback = (bs) =>
            {
                if (!(bs.Body is CircularBody))
                {
                    throw new InvalidOperationException();
                }

                var circularBody = bs.Body as CircularBody;

                switch (bs)
                {
                    case BodyStateClassic bsc:
                    {
                        var orientation = bsc.Orientation;

                        return new RotatableEllipseViewModel
                        {
                            Width = 2 * circularBody.Radius,
                            Height = 2 * circularBody.Radius,
                            Orientation = orientation
                        };
                    }
                    case BodyState:
                    {
                        return new EllipseViewModel
                        {
                            Width = 2 * circularBody.Radius,
                            Height = 2 * circularBody.Radius,
                        };
                    }
                    default:
                    {
                        throw new NotSupportedException();
                    }
                }
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

            // For a start, just hardcode a scene. Later, we will read this from some data source.
            var sceneDefinition = new SceneDefinition();
            Scene3D = _sceneRenderer.Build(sceneDefinition);
            var scene = GenerateScene(sceneDefinition);

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

                LookDirection = new Vector3D(Math.Sin(orientation), 0, Math.Cos(orientation));
                DirectionalLight1 = LookDirection + new Vector3D(0, -0.5, 0);
                PlayerLightPosition = CameraPosition + LookDirection * 3 + new Vector3D(0, -1, 0);
            };
        }

        public void HandleLoaded()
        {
            Engine.StartOrResumeAnimation();
        }

        private Scene GenerateScene(
            SceneDefinition sceneDefinition)
        {
            var ballRadius = 0.095;
            var initialBallPosition = new Vector2D(1.5, 0);

            var initialState = new State();

            // Add the player to the scene
            initialState.AddBodyState(
                new BodyStateClassic(new Player(1, ballRadius, 1, false), initialBallPosition)
                {
                    Orientation = Math.PI
                });

            var npcId = 2;

            // Add npc's to the scene
            foreach (var bodyPosition in sceneDefinition.Bodies)
            {
                initialState.AddBodyState(
                    new BodyState(new NPC(npcId, 0.08, "Bamse"), bodyPosition));

                npcId++;
            }

            var name = "Exploration";
            var standardGravity = 0.0;
            var initialWorldWindowUpperLeft = new Point2D(-1.4, -1.3);
            var initialWorldWindowLowerRight = new Point2D(5, 3);
            var gravitationalConstant = 0.0;
            var coefficientOfFriction = 0.0;
            var timeFactor = 1.0;
            var handleBodyCollisions = true;
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

            sceneDefinition.Boundaries
                .ToList()
                .ForEach(
                    boundary =>
                    {
                        boundary.AdjacentPairs().ToList().ForEach(_ =>
                        {
                            scene.AddBoundary(new LineSegment(
                                _.Item1,
                                _.Item2));
                        });
                    });

            // Denne callback returnerer en værdi, der angiver, hvad der skal ske, når en body kolliderer med en boundary
            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack =
                body => OutcomeOfCollisionBetweenBodyAndBoundary.Block;

            // Denne callback returnerer true, hvis der skal tjekkes for kollision mellem to bodies.
            // Det afhænger sædvanligvis af, hvilke bodies i typehierarkiet, der er tale om
            scene.CheckForCollisionBetweenBodiesCallback = (body1, body2) =>
            {
                // Vi checker for om en probe rammer en NPC
                if (body1 is NPC || body2 is NPC)
                {
                    if (body1 is NPC || body2 is NPC)
                    {
                        return true;
                    }

                    if (body1 is Player || body2 is Player)
                    {
                        return true;
                    }
                }

                // Ellers foretages ikke noget check
                return false;
            };

            // Denne callback returnerer en værdi, der angiver, hvad der skal ske, når to bodies kolliderer
            scene.CollisionBetweenTwoBodiesOccuredCallBack =
                (body1, body2) => OutcomeOfCollisionBetweenTwoBodies.Block;

            var spaceKeyWasPressed = false;

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                spaceKeyWasPressed = keyboardEvents.SpaceDown && keyboardState.SpaceDown;

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

            var nextProbeId = 1000;
            var bodyDisposalMap = new Dictionary<int, int>();

            // Denne callback kales, når tilstanden er propageret.
            // Her kan man manipulere den propagerede tilstand, f.eks. ved at fjerne eller tilføje bodies
            scene.PostPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                // Remove probe
                if (bodyDisposalMap.ContainsKey(propagatedState.Index))
                {
                    var projectile = propagatedState.TryGetBodyState(bodyDisposalMap[propagatedState.Index]);
                    propagatedState?.RemoveBodyState(projectile);
                }

                if (spaceKeyWasPressed)
                {
                    spaceKeyWasPressed = false;

                    bodyDisposalMap[propagatedState.Index + 100] = nextProbeId;

                    var protagonist = propagatedState.BodyStates.First() as BodyStateClassic;

                    var lookDirection = new Vector2D(
                        Math.Cos(protagonist!.Orientation),
                        -Math.Sin(protagonist!.Orientation));

                    propagatedState.AddBodyState(new BodyState(
                        new Probe(nextProbeId, 0.05, 1, true, false), protagonist!.Position)
                    {
                        NaturalVelocity = 3.0 * lookDirection
                    });

                    nextProbeId++;
                }

                var response = new PostPropagationResponse();

                if (bodyCollisionReports.Any())
                {
                    var bcr = bodyCollisionReports.First();
                    var tag = bcr.Body1 is NPC
                        ? bcr.Body1.Tag
                        : bcr.Body2.Tag;

                    response.Outcome = tag;
                    response.IndexOfLastState = propagatedState.Index + 10;
                }

                return response;
            };
            
            return scene;
        }

        private void StartLightAnimation()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);

            var radius = 2;
            
            _timer.Tick += (s, e) =>
            {
                _angle += 0.01;
                var x = radius * Math.Cos(_angle);
                var z = radius * Math.Sin(_angle);

                PlayerLightPosition = new Point3D(x, 1, z);
            };

            _timer.Start();
        }
    }
}