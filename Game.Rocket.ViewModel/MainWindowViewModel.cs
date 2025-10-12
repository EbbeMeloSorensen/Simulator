using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Craft.Logging;
using Craft.Math;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Simulator.Domain;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;
using Simulator.Domain.Props;
using Simulator.Application;
using Simulator.ViewModel;
using Game.Rocket.ViewModel.Bodies;
using Game.Rocket.ViewModel.ShapeViewModels;
using ApplicationState = Craft.DataStructures.Graph.State;
using Application = Simulator.Application.Application;

namespace Game.Rocket.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static int _nextWallId = 100000;

        private const int _initialMagnification = 240;

        private ILogger _logger;   
        private SceneViewManager _sceneViewManager;
        private bool _rocketIgnited;
        private bool _geometryEditorVisible = true;

        private Dictionary<ApplicationState, List<Tuple<ApplicationState, ApplicationState>>> _transitionActivationMap;

        public Application Application { get; }

        public UnlockedLevelsViewModel UnlockedLevelsViewModel { get; }
        public GeometryEditorViewModel GeometryEditorViewModel { get; }

        private RelayCommand _startOrResumeAnimationCommand;

        public RelayCommand StartOrResumeAnimationCommand =>
            _startOrResumeAnimationCommand ?? (_startOrResumeAnimationCommand =
                new RelayCommand(StartOrResumeAnimation, CanStartOrResumeAnimation));

        public bool GeometryEditorVisible
        {
            get { return _geometryEditorVisible; }
            set
            {
                _geometryEditorVisible = value;
                RaisePropertyChanged();
            }
        }

        public MainWindowViewModel(
            ILogger logger)
        {
            _logger = logger;
            _logger = null; // Disable logging (it should only be used for debugging purposes)

            UnlockedLevelsViewModel = new UnlockedLevelsViewModel();

            UnlockedLevelsViewModel.LevelSelected += (s, e) =>
            {
                Application.SwitchState(e.Level.Name);
            };

            // General purpose interaction callbacks that works for all scenes
            var spaceKeyIsDown = false;
            var stateIndexOfFirstShotInBurst = -1000;
            var stateIndexOfLastShotInBurst = -1000;
            var nextProjectileId = 1000;
            var disposeProjectilesMap = new Dictionary<int, int>();
            var rateOfFire = 30;
            var nextFragmentId = 1000;
            var nextMeteorId = 10000;

            InitializationCallback initializationCallback = (state, message) =>
            {
                spaceKeyIsDown = false;
                stateIndexOfFirstShotInBurst = -1000;
                stateIndexOfLastShotInBurst = -1000;
                nextProjectileId = 1000;
                disposeProjectilesMap.Clear();
                nextFragmentId = 100;
                nextMeteorId = 10000;
            };

            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack = body =>
            {
                return body is Meteor 
                    ? OutcomeOfCollisionBetweenBodyAndBoundary.Reflect 
                    : OutcomeOfCollisionBetweenBodyAndBoundary.Block;
            };

            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback = (body1, body2) =>
            {
                if (body1 is Meteor || body2 is Meteor)
                {
                    if (body1 is Projectile || body2 is Projectile)
                    {
                        return true;
                    }

                    if (body1 is Bodies.Rocket || body2 is Bodies.Rocket)
                    {
                        return true;
                    }
                }

                return false;
            };

            CollisionBetweenTwoBodiesOccuredCallBack collisionBetweenTwoBodiesOccuredCallBack =
                (body1, body2) => OutcomeOfCollisionBetweenTwoBodies.Ignore;

            InteractionCallBack interactionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                spaceKeyIsDown = keyboardState.SpaceDown;

                var currentStateOfMainBody = currentState.BodyStates.FirstOrDefault() as BodyStateClassic;

                if (currentStateOfMainBody == null || currentStateOfMainBody.Body.Id != 1)
                {
                    return false;
                }

                var currentRotationalSpeed = currentStateOfMainBody.RotationalSpeed;
                var currentCustomForce = currentStateOfMainBody.CustomForce;

                var newRotationalSpeed = 0.0;
                var newCustomForce = new Vector2D(0, 0);

                if (keyboardState.LeftArrowDown)
                {
                    newRotationalSpeed += Math.PI;
                }

                if (keyboardState.RightArrowDown)
                {
                    newRotationalSpeed -= Math.PI;
                }

                if (keyboardState.UpArrowDown)
                {
                    _rocketIgnited = true;
                    newCustomForce = new Vector2D(3, 0);
                }
                else
                {
                    _rocketIgnited = false;
                }

                currentStateOfMainBody.RotationalSpeed = newRotationalSpeed;
                currentStateOfMainBody.CustomForce = newCustomForce;

                if (Math.Abs(newRotationalSpeed - currentRotationalSpeed) < 0.01 &&
                    (newCustomForce - currentCustomForce).Length < 0.01 &&
                    !keyboardEvents.SpaceDown)
                {
                    return false;
                }

                if (keyboardEvents.SpaceDown)
                {
                    if (keyboardState.SpaceDown)
                    {
                        if (currentState.Index > stateIndexOfLastShotInBurst + rateOfFire)
                        {
                            stateIndexOfFirstShotInBurst = currentState.Index + 1;
                        }
                    }
                    else
                    {
                        stateIndexOfLastShotInBurst = stateIndexOfFirstShotInBurst;
                        while (stateIndexOfLastShotInBurst < currentState.Index)
                        {
                            stateIndexOfLastShotInBurst += rateOfFire;
                        }

                        stateIndexOfLastShotInBurst -= rateOfFire;
                    }
                }

                return true;
            };

            var extraBodies = Enumerable.Range(2, 1)
                .Select(i => new
                {
                    StateIndex = i * 50,
                    BodyState = new BodyStateClassic(new Meteor(i, 0.15, 1, true), new Vector2D(-0.8, -0.8)) { Life = 5, NaturalVelocity = new Vector2D(0.8, 0.2) }
                })
                .ToDictionary(x => x.StateIndex, x => x.BodyState);

            var nRocketFragments = 16;
            var nMeteorFragments = 4;

            PostPropagationCallBack postPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                // Remove projectile due to expiration?
                if (disposeProjectilesMap.ContainsKey(propagatedState.Index))
                {
                    var projectile = propagatedState.TryGetBodyState(disposeProjectilesMap[propagatedState.Index]);
                    propagatedState?.RemoveBodyState(projectile);
                }

                var random = new Random();
                var response = new PostPropagationResponse();

                var rocket = propagatedState.TryGetBodyState(1) as BodyStateClassic;

                if (rocket == null) return response;

                // Remove projectile due to collision with boundary?
                if (boundaryCollisionReports.Any())
                {
                    propagatedState.RemoveBodyStates(boundaryCollisionReports
                        .Where(bcr => bcr.BodyState.Body is Projectile)
                        .Select(bcr => bcr.BodyState.Body.Id));
                }

                // Local function
                void DetonateRocket()
                {
                    propagatedState.RemoveBodyState(rocket);

                    Enumerable.Range(1, nRocketFragments).ToList().ForEach(i =>
                    {
                        var angle = 0.125 * Math.PI + 2.0 * Math.PI * i / nRocketFragments +
                                    (2 * random.NextDouble() - 1) * 0.1;
                        var velocity1 = (0.6 + (2 * random.NextDouble() - 1) * 0.2) *
                                        new Vector2D(Math.Cos(angle), Math.Sin(angle));
                        var velocity2 = (0.8 + (2 * random.NextDouble() - 1) * 0.2) *
                                        new Vector2D(Math.Cos(angle), Math.Sin(angle));

                        propagatedState.AddBodyState(new BodyStateClassic(new Fragment(nextFragmentId++, 0.1, 0.1, true),
                            rocket.Position){NaturalVelocity = velocity1});

                        propagatedState.AddBodyState(new BodyStateClassic(new Fragment(nextFragmentId++, 0.1, 0.1, true),
                            rocket.Position){NaturalVelocity = 2 * velocity2 });
                    });

                    response.Outcome = "Game Over";
                    response.IndexOfLastState = propagatedState.Index + 200;
                }

                var hitEnemies = new HashSet<BodyStateClassic>();

                bodyCollisionReports.ForEach(bcr =>
                {
                    if (bcr.Body1 is Bodies.Rocket || bcr.Body2 is Bodies.Rocket)
                    {
                        // Rocket collided with meteor
                        DetonateRocket();
                    }
                    else if (bcr.Body1 is Projectile || bcr.Body2 is Projectile)
                    {
                        // Projectile collided with meteor
                        if (bcr.Body1 is Projectile)
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body1.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body2.Id) as BodyStateClassic;
                            hitEnemies.Add(bodyState);
                        }
                        else
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body2.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body1.Id) as BodyStateClassic;
                            hitEnemies.Add(bodyState);
                        }
                    }
                });

                hitEnemies.ToList().ForEach(e =>
                {
                    e.Life -= 1;

                    if (e.Life <= 0.1)
                    {
                        propagatedState.RemoveBodyStates(new List<int> { e.Body.Id });

                        if (e.Body.Mass > 0.9)
                        {
                            Enumerable.Range(1, nMeteorFragments).ToList().ForEach(i =>
                            {
                                var angle = 0.125 * Math.PI + 2.0 * Math.PI * i / nMeteorFragments;
                                var velocity = 0.5 * new Vector2D(Math.Cos(angle), Math.Sin(angle));

                                propagatedState.AddBodyState(new BodyStateClassic(new Meteor(nextMeteorId++, 0.1, 0.1, true), e.Position){NaturalVelocity = velocity});
                            });
                        }
                    }
                });

                // Add a projectile from rocket?
                if (spaceKeyIsDown && (propagatedState.Index - stateIndexOfFirstShotInBurst) % rateOfFire == 0)
                {
                    nextProjectileId++;
                    disposeProjectilesMap[propagatedState.Index + 100] = nextProjectileId;
                    var projectileSpeed = 4;

                    var projectileVelocity = new Vector2D(
                        projectileSpeed * Math.Cos(rocket.Orientation),
                        projectileSpeed * -Math.Sin(rocket.Orientation));

                    propagatedState.AddBodyState(new BodyStateClassic(new Projectile(nextProjectileId, 0.025, 1, true), rocket.Position){NaturalVelocity = projectileVelocity });
                }

                // Add an enemy?
                if (extraBodies.ContainsKey(propagatedState.Index))
                {
                    propagatedState.AddBodyState(extraBodies[propagatedState.Index]);
                }

                if (!boundaryCollisionReports.Any()) return response;

                // Check if the rocket collided with anything
                var boundaryCollisionReport = boundaryCollisionReports
                    .FirstOrDefault(bcr => bcr.BodyState.Body is Bodies.Rocket);

                if (boundaryCollisionReport != null)
                {
                    if (string.IsNullOrEmpty(boundaryCollisionReport.Boundary.Tag))
                    {
                        DetonateRocket();
                    }
                    else
                    {
                        // Determine if the collided with an exit or landed safely on a landing platform

                        var tag = boundaryCollisionReport.Boundary.Tag;

                        if (tag.Length >= 8 && tag.Substring(0, 8) == "Platform")
                        {
                            // A platform

                            var lineSegment = boundaryCollisionReport.Boundary as LineSegment;
                            var bodyState = (boundaryCollisionReport.BodyState as BodyStateClassic);

                            var platformCenterX = (lineSegment.Point1.X + lineSegment.Point2.X) / 2;
                            var rocketOrientation = bodyState.Orientation;
                            var rocketVelocity = bodyState.NaturalVelocity;

                            if (Math.Abs(platformCenterX - bodyState.Position.X) > 0.4)
                            {
                                // Not on center of platform
                                DetonateRocket();
                            }
                            else if (Math.Abs(rocketOrientation - Math.PI / 2) > 0.2)
                            {
                                // Bad orientation
                                DetonateRocket();
                            }
                            else if (boundaryCollisionReport.EffectiveSurfaceNormal.Y != -1.0)
                            {
                                // Didnt land on top of the platform
                                DetonateRocket();
                            }
                            else if (rocketVelocity.Length > 0.75)
                            {
                                // Speed too high
                                DetonateRocket();
                            }
                            else
                            {
                                // Safe landing
                                response.Outcome = tag.Substring(9);
                                response.IndexOfLastState = propagatedState.Index + 1;
                            }
                        }
                        else
                        {
                            // Just a regular exit
                            response.Outcome = tag;
                            response.IndexOfLastState = propagatedState.Index + 1;
                        }
                    }
                }

                return response;
            };

            var welcomeScreen = new ApplicationState("Welcome Screen");
            var unlockedLevelsScreen = new ApplicationState("Unlocked Levels Screen");

            var level1a = new Level("Level 1")
            {
                Scene = GenerateScene1a(
                    initializationCallback,
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    checkForCollisionBetweenBodiesCallback,
                    collisionBetweenTwoBodiesOccuredCallBack,
                    postPropagationCallBack)
            };

            var level1b = new Level("Level 1b")
            {
                Scene = GenerateScene1b(
                    initializationCallback,
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    checkForCollisionBetweenBodiesCallback,
                    collisionBetweenTwoBodiesOccuredCallBack,
                    postPropagationCallBack)
            };

            var level1Cleared = new ApplicationState("Level 1 Cleared");

            var level2 = new Level("Level 2")
            {
                Scene = GenerateScene2(
                    initializationCallback,
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    checkForCollisionBetweenBodiesCallback,
                    collisionBetweenTwoBodiesOccuredCallBack,
                    postPropagationCallBack)
            };

            var level2Cleared = new ApplicationState("Level 2 Cleared");

            var level3 = new Level("Level 3")
            {
                Scene = GenerateScene3(
                    initializationCallback,
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    checkForCollisionBetweenBodiesCallback,
                    collisionBetweenTwoBodiesOccuredCallBack,
                    postPropagationCallBack)
            };

            var gameOver = new ApplicationState("Game Over");
            var youWin = new ApplicationState("You Win");

            Application = new Application(_logger, welcomeScreen);

            Application.AddApplicationState(unlockedLevelsScreen);
            Application.AddApplicationState(level1a);
            Application.AddApplicationState(level1b);
            Application.AddApplicationState(level1Cleared);
            Application.AddApplicationState(level2);
            Application.AddApplicationState(level2Cleared);
            Application.AddApplicationState(level3);
            Application.AddApplicationState(gameOver);
            Application.AddApplicationState(youWin);

            Application.AddApplicationStateTransition(welcomeScreen, level1a);
            Application.AddApplicationStateTransition(level1a, gameOver);
            Application.AddApplicationStateTransition(level1a, level1b);
            Application.AddApplicationStateTransition(level1b, gameOver);
            Application.AddApplicationStateTransition(level1b, level1Cleared);
            Application.AddApplicationStateTransition(level1Cleared, level2);
            Application.AddApplicationStateTransition(level2, gameOver);
            Application.AddApplicationStateTransition(level2, level2Cleared);
            Application.AddApplicationStateTransition(level2Cleared, level3);
            Application.AddApplicationStateTransition(level3, gameOver);
            Application.AddApplicationStateTransition(level3, youWin);
            Application.AddApplicationStateTransition(gameOver, welcomeScreen);
            Application.AddApplicationStateTransition(youWin, welcomeScreen);

            _transitionActivationMap = new Dictionary<ApplicationState, List<Tuple<ApplicationState, ApplicationState>>>
            {
                {level1Cleared, new List<Tuple<ApplicationState, ApplicationState>>
                {
                    new (welcomeScreen, unlockedLevelsScreen),
                    new (unlockedLevelsScreen, level1a),
                    new (unlockedLevelsScreen, level2)
                }},
                {level2Cleared, new List<Tuple<ApplicationState, ApplicationState>>
                {
                    new (unlockedLevelsScreen, level3)
                }}
            };

            // If the user hits the space key while the application is in a state that is not a level then switch application state
            Application.KeyEventOccured += (s, e) =>
            {
                if (e.KeyboardKey != KeyboardKey.Space ||
                    e.KeyEventType != KeyEventType.KeyPressed ||
                    Application.State.Object is Level ||
                    Application.State.Object == unlockedLevelsScreen)
                { 
                     return;
                }

                if (Application.State.Object == welcomeScreen &&
                    Application.ExitsFromCurrentApplicationState().Contains("Unlocked Levels Screen"))
                {
                    Application.SwitchState("Unlocked Levels Screen");
                }
                else
                {
                    Application.SwitchState();
                }
            };

            Application.State.PropertyChanged += (s, e) =>
            {
                if (Application.State.Object is Level level)
                {
                    // Dette kald udvirker, at WorldWindow bliver sat
                    _sceneViewManager.ActiveScene = level.Scene;

                    // Prøv lige at override her
                    var x0 = -1.9 - 0.3;
                    var x1 = 5.25 + 0.3;
                    var y0 = -1 - 0.3;
                    var y1 = 3 + 0.3;

                    var worldWindowFocus = new Point(
                        (x1 + x0) / 2,
                        (y1 + y0) / 2);

                    var worldWindowSize = new Size(
                        x1 - x0,
                        y1 - y0);

                    GeometryEditorViewModel.InitializeWorldWindow(worldWindowFocus, worldWindowSize, false);

                    StartOrResumeAnimationCommand.Execute(null);
                }
                else
                {
                    if (Application.State.Object == welcomeScreen)
                    {
                        _sceneViewManager.ActiveScene = null;
                    }
                }
            };

            Application.AnimationCompleted += (s, e) =>
            {
                Application.SwitchState(Application.Engine.Outcome);

                UnlockLevels(Application.State.Object);
            };

            // Aktiver nogle, så du ikke hele tiden skal gennemføre level 1
            //UnlockLevels(level1Cleared);
            //UnlockLevels(level2Cleared);

            GeometryEditorViewModel = new GeometryEditorViewModel()
            {
                UpdateModelCallBack = Application.UpdateModel
            };

            _sceneViewManager = new SceneViewManager(
                Application, 
                GeometryEditorViewModel, 
                (bs) =>
                {
                    switch (bs.Body)
                    {
                        case Bodies.Rocket rocket:
                            {
                                return new RocketViewModel
                                {
                                    Width = 2 * rocket.Radius,
                                    Height = 2 * rocket.Radius,
                                };
                            }
                        case Projectile projectile:
                        {
                            return new ProjectileViewModel()
                            {
                                Width = 2 * projectile.Radius,
                                Height = 2 * projectile.Radius,
                            };
                        }
                        case Fragment fragment:
                            {
                                return new FragmentViewModel
                                {
                                    Width = 2 * fragment.Radius,
                                    Height = 2 * fragment.Radius
                                };
                            }
                        case Meteor meteor:
                        {
                            return new MeteorViewModel
                            {
                                Width = 2 * meteor.Radius,
                                Height = 2 * meteor.Radius
                            };
                        }
                    }

                    throw new InvalidOperationException("Unknown Body Type - cannot select ShapeViewModel");
                },
                (shapeViewModel, bs) =>
                {
                    switch (bs.Body)
                    {
                        case Bodies.Rocket _:
                            {
                                if (shapeViewModel is RocketViewModel rocketViewModel)
                                {
                                    var bsc = bs as BodyStateClassic;
                                    var orientation = bsc == null ? 0 : bsc.Orientation;

                                    rocketViewModel.Orientation = orientation;
                                    rocketViewModel.Ignited = _rocketIgnited;
                                }

                                break;
                            }
                    }

                    shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);
                });
        }

        public void HandleLoaded()
        {
            //ApplicationStateListViewModel.CurrentApplicationState = Application.ApplicationStates.First();
        }

        private void StartOrResumeAnimation()
        {
            Application.StartOrResumeAnimation();
        }

        private bool CanStartOrResumeAnimation()
        {
            return Application.CanStartOrResumeAnimation;
        }

        private static Scene GenerateScene1a(
            InitializationCallback initializationCallback,
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback,
            CollisionBetweenTwoBodiesOccuredCallBack collisionBetweenTwoBodiesOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new Bodies.Rocket(1, 0.125, 1, true), new Vector2D(-1.5, -0.5))
            {
                NaturalVelocity = new Vector2D(0, 0),
                Orientation = 0.5 * Math.PI
            });

            var scene = new Scene("Scene 1a", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, 0, 0, 0, 1, true, 0.005)
            {
                IncludeCustomForces = true,
                InitializationCallback = initializationCallback,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                CheckForCollisionBetweenBodiesCallback = checkForCollisionBetweenBodiesCallback,
                CollisionBetweenTwoBodiesOccuredCallBack = collisionBetweenTwoBodiesOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            var margin = 0.3;
            scene.AddRectangularBoundary(-1.9, 5.25, -1, 3);
            AddWall(scene, -1.9 - margin, 5.25 + margin, -1 - margin, -1, false, false, false, false);
            AddWall(scene, -1.9 - margin, 5.25 + margin, 3, 3 + margin, false, false, false, false);
            AddWall(scene, -1.9 - margin, -1.9, -1, 3, false, false, false, false);
            AddWall(scene, 5.25, 5.25 + margin, -1, 3, false, false, false, false);

            // Add exits
            scene.AddBoundary(new LineSegment(new Vector2D(4, -0.95), new Vector2D(5.25, -0.95), "Level 1b") { Visible = true });

            return scene;
        }

        private static Scene GenerateScene1b(
            InitializationCallback initializationCallback,
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback,
            CollisionBetweenTwoBodiesOccuredCallBack collisionBetweenTwoBodiesOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var standardGravity = 0.5;

            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new Bodies.Rocket(1, 0.125, 1, true), new Vector2D(-1.5, -0.5))
            {
                NaturalVelocity = new Vector2D(0, 0),
                Orientation = 0.5 * Math.PI
            });

            var scene = new Scene("Scene 1b", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, standardGravity, 0, 0, 1, true, 0.005)
            {
                IncludeCustomForces = true,
                InitializationCallback = initializationCallback,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                CheckForCollisionBetweenBodiesCallback = checkForCollisionBetweenBodiesCallback,
                CollisionBetweenTwoBodiesOccuredCallBack = collisionBetweenTwoBodiesOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            var margin = 0.3;
            scene.AddRectangularBoundary(-1.9, 5.25, -1, 3);
            AddWall(scene, -1.9 - margin, 5.25 + margin, -1 - margin, -1, false, false, false, false);
            AddWall(scene, -1.9 - margin, 5.25 + margin, 3, 3 + margin, false, false, false, false);
            AddWall(scene, -1.9 - margin, -1.9, -1, 3, false, false, false, false);
            AddWall(scene, 5.25, 5.25 + margin, -1, 3, false, false, false, false);
            AddWall(scene, -1, -0.5, 2, 3);
            AddWall(scene, 0.5, 1, -1, 0);
            AddWall(scene, 2, 2.5, 2, 3);
            AddWall(scene, 3.5, 4, -1, 0);

            // Add exits
            scene.AddBoundary(new LineSegment(new Vector2D(4, -0.95), new Vector2D(5.25, -0.95), "Level 1 Cleared") { Visible = true });

            return scene;
        }

        private static Scene GenerateScene2(
            InitializationCallback initializationCallback,
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback,
            CollisionBetweenTwoBodiesOccuredCallBack collisionBetweenTwoBodiesOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var standardGravity = 0.5;

            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new Bodies.Rocket(1, 0.125, 1, true), new Vector2D(-1.5, -0.5))
            {
                NaturalVelocity = new Vector2D(0, 0),
                Orientation = 0.5 * Math.PI
            });

            var scene = new Scene("Scene 2", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, standardGravity, 0, 0, 1, true, 0.005)
            {
                IncludeCustomForces = true,
                InitializationCallback = initializationCallback,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                CheckForCollisionBetweenBodiesCallback = checkForCollisionBetweenBodiesCallback,
                CollisionBetweenTwoBodiesOccuredCallBack = collisionBetweenTwoBodiesOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            var margin = 0.3;
            scene.AddRectangularBoundary(-1.9, 5.25, -1, 3);
            AddWall(scene, -1.9 - margin, 5.25 + margin, -1 - margin, -1, false, false, false, false);
            AddWall(scene, -1.9 - margin, 5.25 + margin, 3, 3 + margin, false, false, false, false);
            AddWall(scene, -1.9 - margin, -1.9, -1, 3, false, false, false, false);
            AddWall(scene, 5.25, 5.25 + margin, -1, 3, false, false, false, false);

            AddWall(scene, -1.9, 1, 1.5, 2, false, true, true, true);
            AddWall(scene, 2, 5.25, 0, 0.5, true, false, true, true);

            // Add exits
            scene.AddBoundary(new LineSegment(new Vector2D(4.5, 2.5), new Vector2D(5, 2.5), "Platform-Level 2 Cleared") { Visible = true });

            return scene;
        }

        private static Scene GenerateScene3(
            InitializationCallback initializationCallback,
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            CheckForCollisionBetweenBodiesCallback checkForCollisionBetweenBodiesCallback,
            CollisionBetweenTwoBodiesOccuredCallBack collisionBetweenTwoBodiesOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var standardGravity = 0.5;

            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new Bodies.Rocket(1, 0.125, 1, true), new Vector2D(-1.5, -0.5))
            {
                NaturalVelocity = new Vector2D(0, 0),
                Orientation = 0.5 * Math.PI
            });

            var scene = new Scene("Scene 2", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, standardGravity, 0, 0, 1, true, 0.005)
            {
                IncludeCustomForces = true,
                InitializationCallback = initializationCallback,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                CheckForCollisionBetweenBodiesCallback = checkForCollisionBetweenBodiesCallback,
                CollisionBetweenTwoBodiesOccuredCallBack = collisionBetweenTwoBodiesOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            var margin = 0.3;
            scene.AddRectangularBoundary(-1.9, 5.25, -1, 3);
            AddWall(scene, -1.9 - margin, 5.25 + margin, -1 - margin, -1, false, false, false, false);
            AddWall(scene, -1.9 - margin, 5.25 + margin, 3, 3 + margin, false, false, false, false);
            AddWall(scene, -1.9 - margin, -1.9, -1, 3, false, false, false, false);
            AddWall(scene, 5.25, 5.25 + margin, -1, 3, false, false, false, false);
            var margin2 = 1;
            AddWall(scene, -1.9 + margin2, 5.25 - margin2, -1 + margin2, 3 - margin2);

            // Add exits
            scene.AddBoundary(new LineSegment(new Vector2D(4, -0.95), new Vector2D(5.25, -0.95), "You Win") { Visible = true });

            return scene;
        }

        // Scene building helpers
        private static void AddWall(
            Scene scene,
            double x0,
            double x1,
            double y0,
            double y1,
            bool boundaryLeft = true,
            bool boundaryRight = true,
            bool boundaryTop = true,
            bool boundaryBottom = true)
        {
            scene.Props.Add(new PropRectangle(_nextWallId++, x1 - x0, y1 - y0, new Vector2D((x0 + x1) / 2, (y0 + y1) / 2)));

            if (boundaryLeft)
            {
                scene.AddBoundary(new VerticalLineSegment(x0, y0, y1));
            }

            if (boundaryRight)
            {
                scene.AddBoundary(new VerticalLineSegment(x1, y0, y1));
            }

            if (boundaryTop)
            {
                scene.AddBoundary(new HorizontalLineSegment(y0, x0, x1));
            }

            if (boundaryBottom)
            {
                scene.AddBoundary(new HorizontalLineSegment(y1, x0, x1));
            }
        }

        private void UnlockLevels(
            ApplicationState applicationState)
        {
            if (!_transitionActivationMap.ContainsKey(applicationState)) return;

            _transitionActivationMap[applicationState].ForEach(_ =>
            {
                if (_.Item1.Name == "Unlocked Levels Screen")
                {
                    UnlockedLevelsViewModel.AddLevel(_.Item2 as Level);
                }

                Application.AddApplicationStateTransition(_.Item1, _.Item2);
            });

            _transitionActivationMap.Remove(applicationState);
        }
    }
}
