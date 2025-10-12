using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Craft.Utils;
using Craft.Logging;
using Craft.Math;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Simulator.Application;
using Simulator.Domain;
using Simulator.Domain.Boundaries;
using Simulator.ViewModel;
using Simulator.ViewModel.ShapeViewModels;
using Game.TowerDefense.ViewModel.Bodies;
using Game.TowerDefense.ViewModel.Bodies.Enemies;
using Game.TowerDefense.ViewModel.ShapeViewModels;
using Application = Simulator.Application.Application;
using ApplicationState = Craft.DataStructures.Graph.State;
using BodyStateCannon = Game.TowerDefense.ViewModel.BodyStates.BodyStateCannon;
using BodyStateEnemy = Game.TowerDefense.ViewModel.BodyStates.BodyStateEnemy;
using BodyStateProjectile = Game.TowerDefense.ViewModel.BodyStates.BodyStateProjectile;

namespace Game.TowerDefense.ViewModel
{
    public delegate void UpdateAuxFields(
        string aux1,
        string aux2);

    public class MainWindowViewModel : ViewModelBase
    {
        // Constants that apply to every level
        const double _initialBalance = 300.0;
        const double _initialHealth = 100.0;
        const double _radiusOfCannons = 0.4;
        const double _radiusOfProjectiles = 0.05;
        const double _priceOfCannon = 50.0;
        const double _priceForKilledEnemy = 20.0;
        const int _cannonCoolDown = 1000;
        const double _rangeOfCannons = 2.0;
        const double _projectileSpeed = 10.0;
        const int _projectileLifespan = 600;
        const double _enemyRadius = 0.3;
        const int _enemySpacing = 1000;
        const int _enemyLife = 20;
        const double _enemySpeed = 0.5;

        private ILogger _logger;
        private SceneViewManager _sceneViewManager;
        private bool _geometryEditorVisible = true;
        private Vector2D _worldWindowTranslation;
        private Stopwatch _stopwatch;
        private string _aux1;
        private string _aux2;

        private Dictionary<ApplicationState, List<Tuple<ApplicationState, ApplicationState>>> _transitionActivationMap;

        public string Aux1
        {
            get { return _aux1; }
            set
            {
                _aux1 = value;
                RaisePropertyChanged();
            }
        }

        public string Aux2
        {
            get { return _aux2; }
            set
            {
                _aux2 = value;
                RaisePropertyChanged();
            }
        }

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
            _stopwatch = new Stopwatch();

            UnlockedLevelsViewModel = new UnlockedLevelsViewModel();

            UnlockedLevelsViewModel.LevelSelected += (s, e) =>
            {
                Application.SwitchState(e.Level.Name);
            };

            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack = body =>
            {
                return OutcomeOfCollisionBetweenBodyAndBoundary.Block;
            };

            ShapeSelectorCallback shapeSelectorCallback = (bs) =>
            {
                if (!(bs.Body is CircularBody))
                {
                    throw new InvalidOperationException();
                }

                var circularBody = bs.Body as CircularBody;

                switch (bs)
                {
                    case BodyStateEnemy bsEnemy:
                        {
                            switch (bsEnemy.Body)
                            {
                                case Pig:
                                {
                                    return new PigViewModel
                                    {
                                        Width = 2 * circularBody.Radius,
                                        Height = 2 * circularBody.Radius,
                                        Tag = bsEnemy.Life.ToString()
                                    };
                                }
                                case Rabbit:
                                {
                                    return new RabbitViewModel
                                    {
                                        Width = 2 * circularBody.Radius,
                                        Height = 2 * circularBody.Radius,
                                        Tag = bsEnemy.Life.ToString()
                                    };
                                }
                                case FireDemon:
                                {
                                    return new FireDemonViewModel()
                                    {
                                        Width = 2 * circularBody.Radius,
                                        Height = 2 * circularBody.Radius,
                                        Tag = bsEnemy.Life.ToString()
                                    };
                                }
                            }

                            return new TaggedEllipseViewModel
                            {
                                Width = 2 * circularBody.Radius,
                                Height = 2 * circularBody.Radius,
                                Tag = bsEnemy.Life.ToString()
                            };
                        }
                    case BodyStateCannon cannon:
                    {
                        return new CannonViewModel
                        {
                            Width = 2 * circularBody.Radius,
                            Height = 2 * circularBody.Radius,
                            Orientation = 0
                        };
                    }
                    case BodyStateProjectile:
                        {
                            return new ProjectileViewModel
                            {
                                Width = 2 * circularBody.Radius,
                                Height = 2 * circularBody.Radius
                            };
                        }
                    default:
                        throw new ArgumentException();
                }
            };

            ShapeUpdateCallback shapeUpdateCallback = (shapeViewModel, bs) =>
            {
                shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);

                if (shapeViewModel is TaggedEllipseViewModel taggedEllipseViewModel &&
                    bs is BodyStateEnemy enemy)
                {
                    // Opdater position og life indikator for enemy
                    taggedEllipseViewModel.Tag = enemy.Life.ToString();
                }

                if (shapeViewModel is RotatableEllipseViewModel rotatableEllipseViewModel &&
                    bs is BodyStateCannon cannon)
                {
                    rotatableEllipseViewModel.Orientation = cannon.Orientation;
                }
            };

            var updateAuxFields = new UpdateAuxFields((aux1, aux2) =>
            {
                Aux1 = aux1;
                Aux2 = aux2;
            });

            var welcomeScreen = new ApplicationState("Welcome Screen");
            var unlockedLevelsScreen = new ApplicationState("Unlocked Levels Screen");

            var level1 = new Level("Level 1")
            {
                Scene = GenerateScene1(
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    updateAuxFields)
            };

            var level1Cleared = new ApplicationState("Level 1 Cleared");

            var level2 = new Level("Level 2")
            {
                Scene = GenerateScene2(
                    collisionBetweenBodyAndBoundaryOccuredCallBack)
            };

            var gameOver = new ApplicationState("Game Over");
            var youWin = new ApplicationState("You Win");

            Application = new Application(_logger, welcomeScreen);

            Application.AddApplicationState(unlockedLevelsScreen);
            Application.AddApplicationState(level1);
            Application.AddApplicationState(level1Cleared);
            Application.AddApplicationState(level2);
            Application.AddApplicationState(gameOver);
            Application.AddApplicationState(youWin);

            Application.AddApplicationStateTransition(welcomeScreen, level1);
            Application.AddApplicationStateTransition(level1, gameOver);
            Application.AddApplicationStateTransition(level1, level1Cleared);
            Application.AddApplicationStateTransition(level1Cleared, level2);
            Application.AddApplicationStateTransition(level2, gameOver);
            Application.AddApplicationStateTransition(level2, youWin);
            Application.AddApplicationStateTransition(gameOver, welcomeScreen);
            Application.AddApplicationStateTransition(youWin, welcomeScreen);

            _transitionActivationMap = new Dictionary<ApplicationState, List<Tuple<ApplicationState, ApplicationState>>>
            {
                {level1Cleared, new List<Tuple<ApplicationState, ApplicationState>>
                {
                    new (welcomeScreen, unlockedLevelsScreen),
                    new (unlockedLevelsScreen, level1),
                    new (unlockedLevelsScreen, level2)
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
                // Applikationen har skiftet tilstand (det, der vedligeholdes af state maskinen)
                if (Application.State.Object is Level level)
                {
                    if (Application.PreviousState is Level)
                    {
                        // Nu vil vi så gerne "slide" fra, hvor World Window pt er, og hen til det fokus, der gør sig gældende for næste levels scene
                        var previousWorldWindowFocus = GeometryEditorViewModel.WorldWindowFocus;
                        var nextWorldWindowFocus = level.Scene.InitialWorldWindowFocus();

                        _worldWindowTranslation = new Vector2D(
                            nextWorldWindowFocus.X - previousWorldWindowFocus.X,
                            nextWorldWindowFocus.Y - previousWorldWindowFocus.Y);

                        _stopwatch.Start();

                        // (I starten så lever vi med at den bare nuker nuværende scene,
                        // men senere vil vi gerne vente til den faktisk er landet på næste level
                    }
                    else
                    {
                        GeometryEditorViewModel.InitializeWorldWindow(
                            level.Scene.InitialWorldWindowFocus(),
                            level.Scene.InitialWorldWindowSize(),
                            false);
                    }

                    // Dette kald udvirker, at WorldWindow bliver sat
                    _sceneViewManager.ActiveScene = level.Scene;
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
                AspectRatioLocked = true,
                XAxisLocked = true,
                YAxisLocked = true,
                UpdateModelCallBack = UpdateModel
            };

            GeometryEditorViewModel.MouseClickOccured += (s, e) =>
            {
                Application.HandleMouseClickEvent(new Point2D(
                    e.CursorWorldPosition.X,
                    e.CursorWorldPosition.Y));
            };

            _sceneViewManager = new SceneViewManager(
                Application,
                GeometryEditorViewModel,
                shapeSelectorCallback,  
                shapeUpdateCallback);
        }

        private void UpdateModel()
        {
            if (Application.AnimationRunning)
            {
                Application.UpdateModel();
            }
            else
            {
                if (_sceneViewManager.ActiveScene != null)
                {
                    if (_stopwatch.IsRunning)
                    {
                        var slideDuration = 0.5;
                        var fraction = Math.Max(0.0, 1.0 - _stopwatch.Elapsed.TotalSeconds / slideDuration);

                        var wwFocus = new Point(
                            _sceneViewManager.ActiveScene.InitialWorldWindowFocus().X - _worldWindowTranslation.X * fraction,
                            _sceneViewManager.ActiveScene.InitialWorldWindowFocus().Y - _worldWindowTranslation.Y * fraction);

                        GeometryEditorViewModel.InitializeWorldWindow(
                            wwFocus,
                            _sceneViewManager.ActiveScene.InitialWorldWindowSize(),
                            false);

                        if (fraction > 0.0) return;

                        _stopwatch.Stop();
                        _stopwatch.Reset();
                    }
                    else
                    {
                        StartOrResumeAnimationCommand.Execute(null);
                    }
                }
            }
        }

        private void StartOrResumeAnimation()
        {
            Application.StartOrResumeAnimation();
        }

        private bool CanStartOrResumeAnimation()
        {
            return Application.CanStartOrResumeAnimation;
        }

        private static Scene GenerateScene1(
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            UpdateAuxFields updateAuxFields)
        {
            var path = new Path
            {
                WayPoints = new List<Vector2D>
                {
                    new Vector2D(-1, 2),
                    new Vector2D(12, 2),
                    new Vector2D(14, 4),
                    new Vector2D(4, 4),
                    new Vector2D(2, 6),
                    new Vector2D(17, 6)
                }
            };

            var nextCannonId = 1000;
            var nextPropId = 10000;
            var nextEnemyId = 100000;
            var nextProjectileId = 1000000;

            var initialState = new State();

            var standardGravity = 0.0;
            var gravitationalConstant = 0.0;
            var handleBodyCollisions = true;
            var coefficientOfFriction = 0.0;
            var balance = _initialBalance;
            var health = _initialHealth;

            var x0 = 0.0;
            var x1 = 16.0;
            var y0 = 0.0;
            var y1 = 8.0;

            var scene = new Scene("Scene 1",
                new Point2D(x0, y0),
                new Point2D(x1, y1),
                initialState, 0, 0, 0, 1, true)
            {
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack
            };

            scene.InitializationCallback = (state, message) =>
            {
                balance = _initialBalance;
                health = _initialHealth;

                nextCannonId = 1000;
                nextPropId = 10000;
                nextEnemyId = 100000;
                nextProjectileId = 1000000;
            };

            scene.CheckForCollisionBetweenBodiesCallback = (body1, body2) =>
            {
                if (body1 is Enemy || body2 is Enemy)
                {
                    if (body1 is Projectile || body2 is Projectile)
                    {
                        return true;
                    }
                }

                return false;
            };

            scene.CollisionBetweenTwoBodiesOccuredCallBack = (body1, body2) =>
            {
                if (body1 is Enemy || body2 is Enemy)
                {
                    if (body1 is Projectile || body2 is Projectile)
                    {
                        return OutcomeOfCollisionBetweenTwoBodies.Ignore;
                    }
                }

                return OutcomeOfCollisionBetweenTwoBodies.Block;
            };

            Point2D mousePos = null;

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                updateAuxFields($"{health}", $"{balance}");

                if (mouseClickPosition == null)
                {
                    mousePos = null;
                    return false;
                }

                mousePos = mouseClickPosition.Position;
                return true;
            };

            var enemies = new Dictionary<int, List<BodyStateEnemy>>();

            nextEnemyId = enemies.AddRabbitWave(1, path, _enemySpeed * 3, _enemyLife / 2, 5, _enemySpacing, _enemyRadius * 0.8, nextEnemyId);
            nextEnemyId = enemies.AddPigWave(1, path, _enemySpeed, _enemyLife, 3, _enemySpacing, _enemyRadius, nextEnemyId);
            nextEnemyId = enemies.AddPigWave(5000, path, _enemySpeed, _enemyLife, 3, _enemySpacing, _enemyRadius, nextEnemyId);
            nextEnemyId = enemies.AddPigWave(10000, path, _enemySpeed, _enemyLife, 5, _enemySpacing, _enemyRadius, nextEnemyId);
            nextEnemyId = enemies.AddRabbitWave(10000, path, _enemySpeed * 3, _enemyLife / 2, 5, _enemySpacing, _enemyRadius * 0.8, nextEnemyId);
            nextEnemyId = enemies.AddFireDemonWave(20000, path, _enemySpeed * 0.75, _enemyLife * 10, 1, _enemySpacing, _enemyRadius, nextEnemyId);

            scene.PostPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                var response = new PostPropagationResponse();

                // Remove projectiles due to hitting enemies
                if (boundaryCollisionReports.Any())
                {
                    // Remove enemies, when they get to the exit
                    propagatedState.RemoveBodyStates(boundaryCollisionReports
                        .Where(bcr => bcr.BodyState.Body is Enemy)
                        .Select(bcr => bcr.BodyState.Body.Id));

                    health -= boundaryCollisionReports.Count * 30.0;

                    if (health <= 0)
                    {
                        response.IndexOfLastState = propagatedState.Index;
                        response.Outcome = "Game Over";
                    }
                }

                var hitEnemies = new HashSet<BodyStateEnemy>();

                bodyCollisionReports.ForEach(bcr =>
                {
                    if (bcr.Body1 is Projectile || bcr.Body2 is Projectile)
                    {
                        // Projectile collided with enemy
                        if (bcr.Body1 is Projectile)
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body1.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body2.Id) as BodyStateEnemy;
                            hitEnemies.Add(bodyState);
                        }
                        else
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body2.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body1.Id) as BodyStateEnemy;
                            hitEnemies.Add(bodyState);
                        }
                    }
                });

                hitEnemies.ToList().ForEach(e =>
                {
                    e.Life -= 1;

                    if (e.Life <= 0.1)
                    {
                        balance += _priceForKilledEnemy;
                        propagatedState.RemoveBodyStates(new List<int> { e.Body.Id });

                        if (!propagatedState.BodyStates.Any(bs => bs.Body is Enemy))
                        {
                            // All enemies are dead, so player wins
                            response.IndexOfLastState = propagatedState.Index;
                            response.Outcome = "Level 1 Cleared";
                        }
                    }
                });

                if (boundaryCollisionReports.Any())
                {
                    propagatedState.RemoveBodyStates(boundaryCollisionReports.Select(bcr => bcr.BodyState.Body.Id));
                }

                // Add a new cannon?
                if (mousePos != null)
                {
                    var mousePosAsVector = mousePos.AsVector2D();

                    var cannonCenters = propagatedState.BodyStates
                        .Where(_ => _.Body is Cannon)
                        .Select(_ => _.Position);

                    if (balance >= _priceOfCannon &&
                        cannonCenters.DistanceToClosestPoint(mousePosAsVector) > 2 * _radiusOfCannons &&
                        scene.Props.Min(_ => _.DistanceToPoint(mousePosAsVector) - _radiusOfCannons > 0.0))
                    {
                        propagatedState.AddBodyState(new BodyStateCannon(
                            new Cannon(nextCannonId++, _radiusOfCannons), mousePosAsVector)
                        {
                            CoolDown = _cannonCoolDown
                        });

                        balance -= _priceOfCannon;
                    }

                    mousePos = null;
                }

                // Remove projectiles due to limited lifespan?
                var disposableProjectiles = propagatedState.BodyStates
                    .Where(_ =>
                        _.Body is Projectile &&
                        (_ as BodyStateProjectile).LifeSpan <= 0)
                    .Select(_ => _.Body.Id)
                    .ToList();

                propagatedState.RemoveBodyStates(disposableProjectiles);

                // Add projectiles?
                var bodyStatesOfCannonsThatMayShoot = propagatedState.BodyStates
                    .Where(_ =>
                        _.Body is Cannon &&
                        (_ as BodyStateCannon).CoolDown <= 0)
                    .ToList();

                bodyStatesOfCannonsThatMayShoot.ForEach(bodyState =>
                {
                    // This cannon can shoot

                    var rangeOfCannonsSquared = _rangeOfCannons * _rangeOfCannons;

                    var target = propagatedState.BodyStates
                        .Where(_ => _ is BodyStateEnemy)
                        .Select(_ => _ as BodyStateEnemy)
                        .Where(_ => _.Position.SquaredDistanceTo(bodyState.Position) < rangeOfCannonsSquared)
                        .Select(_ => new { BodyState = _, _.DistanceCovered })
                        .OrderByDescending(_ => _.DistanceCovered)
                        .FirstOrDefault();

                    if (target == null)
                    {
                        return;
                    }

                    var projectileVelocity = (target.BodyState.Position - bodyState.Position).Normalize() * _projectileSpeed;

                    (bodyState as BodyStateCannon).Orientation = -projectileVelocity.AsPolarVector().Angle;

                    propagatedState.AddBodyState(new BodyStateProjectile(
                        new Projectile(
                            nextProjectileId++,
                            _radiusOfProjectiles),
                        bodyState.Position)
                    {
                        NaturalVelocity = projectileVelocity,
                        LifeSpan = _projectileLifespan
                    });

                    (bodyState as BodyStateCannon).CoolDown = _cannonCoolDown;
                });

                // Add an enemy?
                if (enemies.ContainsKey(propagatedState.Index))
                {
                    enemies[propagatedState.Index].ForEach(_ => propagatedState.AddBodyState(_));
                }

                return response;
            };

            scene.AddPath(path, _enemyRadius * 2.5, nextPropId);
            scene.AddBoundary(new LeftFacingHalfPlane(17));

            return scene;
        }

        private static Scene GenerateScene2(
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack)
        {
            var path = new Path
            {
                WayPoints = new List<Vector2D>
                {
                    new Vector2D(-1, 3),
                    new Vector2D(8, 3),
                    new Vector2D(8, 1),
                    new Vector2D(5, 1),
                    new Vector2D(5, 7),
                    new Vector2D(2, 7),
                    new Vector2D(2, 5),
                    new Vector2D(11, 5),
                    new Vector2D(11, 2),
                    new Vector2D(14, 2),
                    new Vector2D(14, 7),
                    new Vector2D(8, 7),
                    new Vector2D(8, 9)
                }
            };

            var nextCannonId = 1000;
            var nextProjectileId = 10000;
            var nextPropId = 100000;

            var initialState = new State();

            var standardGravity = 0.0;
            var gravitationalConstant = 0.0;
            var handleBodyCollisions = true;
            var coefficientOfFriction = 0.0;
            var balance = _initialBalance;
            var health = _initialHealth;

            var x0 = 0.0;
            var x1 = 16.0;
            var y0 = 0.0;
            var y1 = 8.0;

            var scene = new Scene("Scene 2",
                new Point2D(x0, y0),
                new Point2D(x1, y1),
                initialState, 0, 0, 0, 1, true)
            {
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack
            };

            scene.InitializationCallback = (state, message) =>
            {
                balance = _initialBalance;
                health = _initialHealth;
                nextCannonId = 1000;
                nextProjectileId = 10000;
                nextPropId = 100000;
            };

            scene.CheckForCollisionBetweenBodiesCallback = (body1, body2) =>
            {
                if (body1 is Enemy || body2 is Enemy)
                {
                    if (body1 is Projectile || body2 is Projectile)
                    {
                        return true;
                    }
                }

                return false;
            };

            scene.CollisionBetweenTwoBodiesOccuredCallBack = (body1, body2) =>
            {
                if (body1 is Enemy || body2 is Enemy)
                {
                    if (body1 is Projectile || body2 is Projectile)
                    {
                        return OutcomeOfCollisionBetweenTwoBodies.Ignore;
                    }
                }

                return OutcomeOfCollisionBetweenTwoBodies.Block;
            };

            Point2D mousePos = null;

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                //updateAuxFields($"Life: {health}", $"$: {balance}");

                if (mouseClickPosition == null)
                {
                    mousePos = null;
                    return false;
                }

                mousePos = mouseClickPosition.Position;
                return true;
            };

            var enemies = Enumerable.Range(1, 10)
                .Select(i => new
                {
                    StateIndex = i * _enemySpacing,
                    BodyState = new BodyStateEnemy(new Enemy(i, _enemyRadius), new Vector2D(-1, 3))
                    {
                        Path = path,
                        Speed = _enemySpeed,
                        NaturalVelocity = new Vector2D(0.2, 0),
                        Life = _enemyLife
                    }
                })
                .ToDictionary(_ => _.StateIndex, _ => _.BodyState);

            scene.PostPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                var response = new PostPropagationResponse();

                // Remove projectiles due to hitting enemies
                if (boundaryCollisionReports.Any())
                {
                    // Remove enemies, when they get to the exit
                    propagatedState.RemoveBodyStates(boundaryCollisionReports
                        .Where(bcr => bcr.BodyState.Body is Enemy)
                        .Select(bcr => bcr.BodyState.Body.Id));

                    health -= boundaryCollisionReports.Count * 30.0;

                    if (health <= 0)
                    {
                        response.IndexOfLastState = propagatedState.Index;
                        response.Outcome = "Game Over";
                    }
                }

                var hitEnemies = new HashSet<BodyStateEnemy>();

                bodyCollisionReports.ForEach(bcr =>
                {
                    if (bcr.Body1 is Projectile || bcr.Body2 is Projectile)
                    {
                        // Projectile collided with enemy
                        if (bcr.Body1 is Projectile)
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body1.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body2.Id) as BodyStateEnemy;
                            hitEnemies.Add(bodyState);
                        }
                        else
                        {
                            propagatedState.RemoveBodyStates(new List<int> { bcr.Body2.Id });
                            var bodyState = propagatedState.TryGetBodyState(bcr.Body1.Id) as BodyStateEnemy;
                            hitEnemies.Add(bodyState);
                        }
                    }
                });

                hitEnemies.ToList().ForEach(e =>
                {
                    e.Life -= 1;

                    if (e.Life <= 0.1)
                    {
                        balance += _priceForKilledEnemy;
                        propagatedState.RemoveBodyStates(new List<int> { e.Body.Id });

                        if (!propagatedState.BodyStates.Any(bs => bs.Body is Enemy))
                        {
                            // All enemies are dead, so player wins
                            response.IndexOfLastState = propagatedState.Index;
                            response.Outcome = "You Win";
                        }
                    }
                });

                if (boundaryCollisionReports.Any())
                {
                    propagatedState.RemoveBodyStates(boundaryCollisionReports.Select(bcr => bcr.BodyState.Body.Id));
                }

                // Add a new cannon?
                if (mousePos != null)
                {
                    var mousePosAsVector = mousePos.AsVector2D();

                    var cannonCenters = propagatedState.BodyStates
                        .Where(_ => _.Body is Cannon)
                        .Select(_ => _.Position);

                    if (balance >= _priceOfCannon &&
                        cannonCenters.DistanceToClosestPoint(mousePosAsVector) > 2 * _radiusOfCannons &&
                        scene.Props.Min(_ => _.DistanceToPoint(mousePosAsVector) - _radiusOfCannons > 0.0))
                    {
                        propagatedState.AddBodyState(new BodyStateCannon(
                            new Cannon(nextCannonId++, _radiusOfCannons), mousePosAsVector)
                        {
                            CoolDown = _cannonCoolDown
                        });

                        balance -= _priceOfCannon;
                    }

                    mousePos = null;
                }

                // Remove projectiles due to limited lifespan?
                var disposableProjectiles = propagatedState.BodyStates
                    .Where(_ =>
                        _.Body is Projectile &&
                        (_ as BodyStateProjectile).LifeSpan <= 0)
                    .Select(_ => _.Body.Id)
                    .ToList();

                propagatedState.RemoveBodyStates(disposableProjectiles);

                // Add projectiles?
                var bodyStatesOfCannonsThatMayShoot = propagatedState.BodyStates
                    .Where(_ =>
                        _.Body is Cannon &&
                        (_ as BodyStateCannon).CoolDown <= 0)
                    .ToList();

                bodyStatesOfCannonsThatMayShoot.ForEach(bodyState =>
                {
                    // This cannon can shoot

                    var rangeOfCannonsSquared = _rangeOfCannons * _rangeOfCannons;

                    var target = propagatedState.BodyStates
                        .Where(_ => _ is BodyStateEnemy)
                        .Select(_ => _ as BodyStateEnemy)
                        .Where(_ => _.Position.SquaredDistanceTo(bodyState.Position) < rangeOfCannonsSquared)
                        .Select(_ => new { BodyState = _, _.DistanceCovered })
                        .OrderByDescending(_ => _.DistanceCovered)
                        .FirstOrDefault();

                    if (target == null)
                    {
                        return;
                    }

                    var projectileVelocity = (target.BodyState.Position - bodyState.Position).Normalize() * _projectileSpeed;

                    (bodyState as BodyStateCannon).Orientation = -projectileVelocity.AsPolarVector().Angle;

                    propagatedState.AddBodyState(new BodyStateProjectile(
                        new Projectile(
                            nextProjectileId++,
                            _radiusOfProjectiles),
                        bodyState.Position)
                    {
                        NaturalVelocity = projectileVelocity,
                        LifeSpan = _projectileLifespan
                    });

                    (bodyState as BodyStateCannon).CoolDown = _cannonCoolDown;
                });

                // Add an enemy?
                if (enemies.ContainsKey(propagatedState.Index))
                {
                    propagatedState.AddBodyState(enemies[propagatedState.Index]);
                }

                return response;
            };

            scene.AddPath(path, _enemyRadius * 2.5, nextPropId);
            scene.AddBoundary(new UpFacingHalfPlane(9));

            return scene;
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
