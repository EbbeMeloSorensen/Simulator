using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Craft.Logging;
using Craft.Math;
using Craft.Utils;
using Craft.ViewModels.Geometry2D.ScrollFree;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Game.Zelda.ViewModel.BodyStates;
using Simulator.Domain;
using Simulator.Domain.Boundaries;
using Simulator.Domain.Props;
using Simulator.Application;
using Simulator.ViewModel;
using Game.Zelda.ViewModel.ShapeViewModels;
using Application = Simulator.Application.Application;
using ApplicationState = Craft.DataStructures.Graph.State;

namespace Game.Zelda.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static double _zeldaRadius = 0.25;

        private static int _nextWallId = 100000;
        private static bool _boundariesVisible = false; // Set to true to inspect boundaries

        private ILogger _logger;
        private SceneViewManager _sceneViewManager;
        private bool _geometryEditorVisible = true;
        private Vector2D _worldWindowTranslation;
        private Stopwatch _stopwatch;

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

            var spaceKeyWasPressed = false;

            InteractionCallBack interactionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                spaceKeyWasPressed = keyboardEvents.SpaceDown && keyboardState.SpaceDown;

                var currentStateOfMainBody = currentState.BodyStates.First() as BodyStateZelda;
                var currentArtificialVelocity = currentStateOfMainBody.ArtificialVelocity;

                var newMovementDirection = new Vector2D(0, 0);

                if (keyboardState.LeftArrowDown)
                {
                    newMovementDirection += new Vector2D(-1, 0);
                }

                if (keyboardState.RightArrowDown)
                {
                    newMovementDirection += new Vector2D(1, 0);
                }

                if (keyboardState.UpArrowDown)
                {
                    newMovementDirection += new Vector2D(0, -1);
                }

                if (keyboardState.DownArrowDown)
                {
                    newMovementDirection += new Vector2D(0, 1);
                }

                var newArtificialVelocity = new Vector2D(0, 0);

                if (newMovementDirection.Length > 0.01)
                {
                    var speed = 3.0;
                    newArtificialVelocity = speed * newMovementDirection.Normalize();
                }

                if ((newArtificialVelocity - currentArtificialVelocity).Length < 0.01 && !spaceKeyWasPressed)
                {
                    return false;
                }

                currentStateOfMainBody.ArtificialVelocity = newArtificialVelocity;

                return true;
            };

            PostPropagationCallBack postPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                var response = new PostPropagationResponse();

                var zelda = propagatedState.TryGetBodyState(1) as BodyStateZelda;

                if (zelda == null) return response;

                if (!boundaryCollisionReports.Any()) return response;

                // Check if Zelda collided with anything
                var boundaryCollisionReport = boundaryCollisionReports
                    .FirstOrDefault(bcr => bcr.BodyState.Body is Bodies.Zelda);

                if (boundaryCollisionReport != null)
                {
                    if (!string.IsNullOrEmpty(boundaryCollisionReport.Boundary.Tag))
                    {
                        response.Outcome = boundaryCollisionReport.Boundary.Tag;
                        response.IndexOfLastState = propagatedState.Index + 1;
                    }
                }

                return response;
            };

            var welcomeScreen = new ApplicationState("Welcome Screen");
            var unlockedLevelsScreen = new ApplicationState("Unlocked Levels Screen");

            var level1a = new Level("Level 1")
            {
                Scene = GenerateScene1a(
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    postPropagationCallBack)
            };

            var level1b = new Level("Level 1b")
            {
                Scene = GenerateScene1b(
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    postPropagationCallBack)
            };

            var level1c = new Level("Level 1c")
            {
                Scene = GenerateScene1c(
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    postPropagationCallBack)
            };

            var level1Cleared = new ApplicationState("Level 1 Cleared");

            var level2 = new Level("Level 2")
            {
                Scene = GenerateScene2(
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    postPropagationCallBack)
            };

            var level2Cleared = new ApplicationState("Level 2 Cleared");

            var level3 = new Level("Level 3")
            {
                Scene = GenerateScene3(
                    interactionCallBack,
                    collisionBetweenBodyAndBoundaryOccuredCallBack,
                    postPropagationCallBack)
            };

            var gameOver = new ApplicationState("Game Over");
            var youWin = new ApplicationState("You Win");

            Application = new Application(_logger, welcomeScreen);

            Application.AddApplicationState(unlockedLevelsScreen);
            Application.AddApplicationState(level1a);
            Application.AddApplicationState(level1b);
            Application.AddApplicationState(level1c);
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
            Application.AddApplicationStateTransition(level1b, level1a);
            Application.AddApplicationStateTransition(level1b, level1c);
            Application.AddApplicationStateTransition(level1c, gameOver);
            Application.AddApplicationStateTransition(level1c, level1b);
            Application.AddApplicationStateTransition(level1c, level1Cleared);
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
                UpdateModelCallBack = UpdateModel
            };

            _sceneViewManager = new SceneViewManager(
                Application,
                GeometryEditorViewModel,
                (bs) =>
                {
                    switch (bs.Body)
                    {
                        case Bodies.Zelda zelda:
                            {
                                return new ZeldaViewModel(
                                    2 * zelda.Radius,
                                    2 * zelda.Radius);
                            }
                    }

                    throw new InvalidOperationException("Unknown Body Type - cannot select ShapeViewModel");
                },
                (shapeViewModel, bs) =>
                {
                    shapeViewModel.Point = new PointD(bs.Position.X, bs.Position.Y);
                });
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

        private static Scene GenerateScene1a(
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateZelda(new Bodies.Zelda(1, _zeldaRadius, 1, true), new Vector2D(5, 3.75)));

            var wallWidth = 0.3;
            var doorWidth = 1.2;
            var x0 = 0.0;
            var x1 = 16.0;
            var y0 = 0.0;
            var y1 = 8.0;

            var scene = new Scene("Scene 1a", 
                new Point2D(x0 - wallWidth / 2, y0 - wallWidth / 2), 
                new Point2D(x1 + wallWidth / 2, y1 + wallWidth / 2), 
                initialState, 0, 0, 0, 1, false)
            {
                IncludeCustomForces = true,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            scene.InitializationCallback = (state, message) =>
            {
                if (message == "Scene 1b")
                {
                    state.BodyStates.First().Position = new Vector2D(wallWidth / 2 + doorWidth / 2, 0.01);
                }
            };

            scene.AddBoundary(new RightFacingHalfPlane(x0 + wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new LeftFacingHalfPlane(x1 - wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new UpFacingHalfPlane(y1 - wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2 + doorWidth, -wallWidth / 2), 
                new Vector2D(wallWidth / 2 + doorWidth, wallWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2 + doorWidth, wallWidth / 2), 
                new Vector2D(x1 - wallWidth / 2, wallWidth / 2)) { Visible = _boundariesVisible });

            AddWall(scene, x0 + wallWidth / 2 + doorWidth, x1 + wallWidth / 2, y0 - wallWidth / 2, y0 + wallWidth / 2, false, false, false, false); // Øverste mur
            AddWall(scene, x0 - wallWidth / 2, x1 + wallWidth / 2, y1 - wallWidth / 2, y1 + wallWidth / 2, false, false, false, false); // Nederste mur
            AddWall(scene, x0 - wallWidth / 2, x0 + wallWidth / 2, y0 - wallWidth / 2, y1 - wallWidth / 2, false, false, false, false); // Venstre mur
            AddWall(scene, x1 - wallWidth / 2, x1 + wallWidth / 2, y0 + wallWidth / 2, y1 - wallWidth / 2, false, false, false, false); // Højre mur

            AddWall(scene, 8, 8 + wallWidth, y0 + wallWidth / 2, 3.2, true, true, false, true); // Ekstra mur
            AddWall(scene, 8, 8 + wallWidth, 4.8, y1 - wallWidth / 2, true, true, true, false); // Ekstra mur

            // Add exits
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2, -0.5), 
                new Vector2D(wallWidth / 2 + doorWidth, -0.5), "Level 1b") { Visible = _boundariesVisible });

            return scene;
        }

        private static Scene GenerateScene1b(
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateZelda(new Bodies.Zelda(1, _zeldaRadius, 1, true), new Vector2D(1, 15)));

            var wallWidth = 0.3;
            var doorWidth = 1.2;
            var x0 = 0.0;
            var x1 = 16.0;
            var y0 = -8.0;
            var y1 = 0;

            var scene = new Scene("Scene 1b",
                new Point2D(x0 - wallWidth / 2, y0 - wallWidth / 2),
                new Point2D(x1 + wallWidth / 2, y1 + wallWidth / 2),
                initialState, 0, 0, 0, 1, false)
            {
                IncludeCustomForces = true,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            scene.InitializationCallback = (state, message) =>
            {
                switch (message)
                {
                    case "Scene 1a":
                    {
                        state.BodyStates.First().Position = new Vector2D(wallWidth / 2 + doorWidth / 2, -0.01);
                        break;
                    }
                    case "Scene 1c":
                    {
                        state.BodyStates.First().Position = new Vector2D(0.01, -2);
                        break;
                    }
                }
            };

            scene.AddBoundary(new LeftFacingHalfPlane(x1 - wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new DownFacingHalfPlane(y0 + wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2 + doorWidth, -wallWidth / 2), 
                new Vector2D(wallWidth / 2 + doorWidth, wallWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2 + doorWidth, -wallWidth / 2), 
                new Vector2D(x1 + wallWidth / 2, -wallWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2, y0 + wallWidth / 2), 
                new Vector2D(wallWidth / 2, -4 - wallWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2, -4 + wallWidth / 2), 
                new Vector2D(wallWidth / 2, -2 - doorWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(wallWidth / 2, -2 + doorWidth / 2), 
                new Vector2D(wallWidth / 2, y1 + wallWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(-wallWidth / 2, -2 - doorWidth / 2), 
                new Vector2D(wallWidth / 2, -2 - doorWidth / 2)) { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(-wallWidth / 2, -2 + doorWidth / 2), 
                new Vector2D(wallWidth / 2, -2 + doorWidth / 2)) { Visible = _boundariesVisible });


            AddWall(scene, x0 - wallWidth / 2, x1 + wallWidth / 2, y0 - wallWidth / 2, y0 + wallWidth / 2, false, false, false, false); // Øverste mur
            AddWall(scene, x0 + wallWidth / 2 + doorWidth, x1 + wallWidth / 2, y1 - wallWidth / 2, y1 + wallWidth / 2, false, false, false, false); // Nederste mur
            AddWall(scene, x0 - wallWidth / 2, x0 + wallWidth / 2, y0 - wallWidth / 2, -2 - doorWidth / 2, false, false, false, false); // Venstre mur øverst
            AddWall(scene, x0 - wallWidth / 2, x0 + wallWidth / 2, -2 + doorWidth / 2, y1 + wallWidth / 2, false, false, false, false); // Venstre mur nederst
            AddWall(scene, x1 - wallWidth / 2, x1 + wallWidth / 2, y0 + wallWidth / 2, y1 - wallWidth / 2, false, false, false, false); // Højre mur

            AddWall(scene, x0 + wallWidth / 2, x0 + 7, -4 - wallWidth / 2, -4 + wallWidth / 2, false, true, true, true); // Ekstra mur
            AddWall(scene, x0 + wallWidth / 2 + 9, x1 - wallWidth / 2, -4 - wallWidth / 2, -4 + wallWidth / 2, true, false, true, true); // Ekstra mur

            // Add exits
            //scene.AddBoundary(new LineSegment(new Vector2D(3, -0.05), new Vector2D(4, -0.05), "Level 1 Cleared") { Visible = true });
            scene.AddBoundary(new LineSegment(
                new Vector2D(0, 0.5), 
                new Vector2D(doorWidth, 0.5), 
                "Level 1") { Visible = _boundariesVisible });
            scene.AddBoundary(new LineSegment(
                new Vector2D(-0.5, -2 - doorWidth / 2), 
                new Vector2D(-0.5, -2 + doorWidth / 2), 
                "Level 1c") { Visible = _boundariesVisible });

            return scene;
        }

        private static Scene GenerateScene1c(
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateZelda(new Bodies.Zelda(1, _zeldaRadius, 1, true), new Vector2D(1, 15)));

            var wallWidth = 0.3;
            var doorWidth = 1.2;
            var x0 = -16.0;
            var x1 = 0.0;
            var y0 = -8.0;
            var y1 = 0;

            var scene = new Scene("Scene 1c",
                new Point2D(x0 - wallWidth / 2, y0 - wallWidth / 2),
                new Point2D(x1 + wallWidth / 2, y1 + wallWidth / 2),
                initialState, 0, 0, 0, 1, false)
            {
                IncludeCustomForces = true,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
                PostPropagationCallBack = postPropagationCallBack,
                InteractionCallBack = interactionCallBack
            };

            scene.InitializationCallback = (state, message) =>
            {
                if (message == "Scene 1b")
                {
                    state.BodyStates.First().Position = new Vector2D(-0.01, -2);
                }
            };

            scene.AddBoundary(new RightFacingHalfPlane(x0 + wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new UpFacingHalfPlane(y1 - wallWidth / 2) { Visible = _boundariesVisible });
            scene.AddBoundary(new DownFacingHalfPlane(y0 + wallWidth / 2) { Visible = _boundariesVisible });

            AddWall(scene, x0 - wallWidth / 2, x1 + wallWidth / 2, y0 - wallWidth / 2, y0 + wallWidth / 2, false, false, false, false); // Øverste mur
            AddWall(scene, x0 - wallWidth / 2, x1 + wallWidth / 2, y1 - wallWidth / 2, y1 + wallWidth / 2, false, false, false, false); // Nederste mur
            AddWall(scene, x0 - wallWidth / 2, x0 + wallWidth / 2, y0 + wallWidth / 2, y1 - wallWidth / 2, false, false, false, false); // Venstre mur
            AddWall(scene, x1 - wallWidth / 2, x1 + wallWidth / 2, y0 + wallWidth / 2, -2 - doorWidth / 2, true, false, false, true); // Højre mur, øverst
            AddWall(scene, x1 - wallWidth / 2, x1 + wallWidth / 2, -2 + doorWidth / 2, y1 - wallWidth / 2, true, false, true, false); // Højre mur, nederst

            // Add exits
            scene.AddBoundary(new LineSegment(
                new Vector2D(0.5, -2 - doorWidth / 2), 
                new Vector2D(0.5, -2 + doorWidth / 2), "Level 1b") { Visible = _boundariesVisible });

            return scene;
        }

        private static Scene GenerateScene2(
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateZelda(new Bodies.Zelda(1, 0.125, 1, true), new Vector2D(-1.5, -0.5)));

            var scene = new Scene("Scene 2", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, 0, 0, 0, 1, false)
            {
                IncludeCustomForces = true,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
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
            scene.AddBoundary(new LineSegment(new Vector2D(4.5, 2.5), new Vector2D(5, 2.5), "Level 2 Cleared") { Visible = true });

            return scene;
        }

        private static Scene GenerateScene3(
            InteractionCallBack interactionCallBack,
            CollisionBetweenBodyAndBoundaryOccuredCallBack collisionBetweenBodyAndBoundaryOccuredCallBack,
            PostPropagationCallBack postPropagationCallBack)
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateZelda(new Bodies.Zelda(1, 0.125, 1, true), new Vector2D(-1.5, -0.5)));

            var scene = new Scene("Scene 2", 
                new Point2D(-1.9321428571428569, -1.0321428571428573), new Point2D(5, 3), initialState, 0, 0, 0, 1, false)
            {
                IncludeCustomForces = true,
                CollisionBetweenBodyAndBoundaryOccuredCallBack = collisionBetweenBodyAndBoundaryOccuredCallBack,
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

        // Scene building helper
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
