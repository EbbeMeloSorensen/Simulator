using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Craft.Logging;
using Craft.Math;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Simulator.Domain;
using Simulator.Domain.Boundaries;
using Simulator.Domain.BodyStates;
using Simulator.Application;
using Simulator.ViewModel;

namespace Game.FlappyBird.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ILogger _logger;
        private SceneViewManager _sceneViewManager;
        private string _outcome;

        public Application Application { get; }
        public GeometryEditorViewModel GeometryEditorViewModel { get; }

        private RelayCommand _startOrResumeAnimationCommand;
        private RelayCommand _resetAnimationCommand;

        public string Outcome
        {
            get { return _outcome; }
            set
            {
                _outcome = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand StartOrResumeAnimationCommand =>
            _startOrResumeAnimationCommand ?? (_startOrResumeAnimationCommand =
                new RelayCommand(StartOrResumeAnimation, CanStartOrResumeAnimation));

        public RelayCommand ResetAnimationCommand =>
            _resetAnimationCommand ?? (_resetAnimationCommand =
                new RelayCommand(ResetAnimation, CanResetAnimation));

        public MainWindowViewModel(
            ILogger logger)
        {
            _logger = logger;
            _logger = null; // Disable logging (it should only be used for debugging purposes)

            Application = new Application(_logger);
            Application.AnimationCompleted += (s, e) =>
            {
                Outcome = Application.Engine.Outcome;
                RefreshButtons();
            };

            // Bemærk: Det er et ALMINDELIGT view og altså ikke et "matematisk"
            GeometryEditorViewModel = new GeometryEditorViewModel(1)
            {
                UpdateModelCallBack = Application.UpdateModel
            };

            _sceneViewManager = new SceneViewManager(Application, GeometryEditorViewModel);

            var scene = GenerateScene();

            GeometryEditorViewModel.InitializeWorldWindow(
                scene.InitialWorldWindowFocus(),
                scene.InitialWorldWindowSize(),
                false);

            _sceneViewManager.ActiveScene = scene;
        }

        public void HandleLoaded()
        {
            _sceneViewManager.ResetScene();
        }

        private void StartOrResumeAnimation()
        {
            Application.StartOrResumeAnimation();
            RefreshButtons();
        }

        private void ResetAnimation()
        {
            _sceneViewManager.ResetScene();
            RefreshButtons();
            Outcome = null;
        }

        private bool CanStartOrResumeAnimation()
        {
            return Application.CanStartOrResumeAnimation;
        }

        private bool CanResetAnimation()
        {
            return Application.CanResetAnimation;
        }

        private void RefreshButtons()
        {
            StartOrResumeAnimationCommand.RaiseCanExecuteChanged();
            ResetAnimationCommand.RaiseCanExecuteChanged();
        }

        private Scene GenerateScene()
        {
            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(
                new CircularBody(1, 0.2, 1, true), 
                new Vector2D(1, -0.1))
                {
                    NaturalVelocity = new Vector2D(1.5, -3)
                });

            var scene = new Scene("Flappy Bird", new Point2D(-1, -1.1), new Point2D(5, 3.1),
                initialState, 9.82, 0, 0, 1, false, 0.005, SceneViewMode.MaintainFocusInVicinityOfPoint,
                double.MinValue, double.MinValue, double.MaxValue, double.MaxValue, 0, 1E200, 0.25);

            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack = body => OutcomeOfCollisionBetweenBodyAndBoundary.Block;

            scene.PostPropagationCallBack = (propagatedState, boundaryCollisionReports, bodyCollisionReports) =>
            {
                var response = new PostPropagationResponse();

                if (boundaryCollisionReports.Any())
                {
                    response.Outcome = boundaryCollisionReports
                        .Any(bcr => bcr.Boundary.Tag == "Exit")
                        ? "You Win"
                        : "Game Over";

                    response.IndexOfLastState = propagatedState.Index + 10;
                }

                return response;
            };

            scene.InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
            {
                if (!keyboardState.UpArrowDown) return false;

                currentState.BodyStates.First().NaturalVelocity = new Vector2D(1.5, -3);
                return true;
            };

            var floorLevel = 3;
            var ceilingLevel = -1;

            // Local Function
            void AddObstacleFloor(double x, double width, double height)
            {
                scene.AddRectangularBoundary(x - width / 2, x + width / 2, floorLevel - height, floorLevel);
            }

            // Local Function
            void AddObstacleCeiling(double x, double width, double height)
            {
                scene.AddRectangularBoundary(x - width / 2, x + width / 2, ceilingLevel, ceilingLevel + height);
            }

            // Local Function
            void AddObstacleGate(double x, double width, double gateCenterHeight, double gateHeight)
            {
                AddObstacleFloor(x, width, gateCenterHeight - gateHeight / 2);
                AddObstacleCeiling(x, width, floorLevel - ceilingLevel - gateCenterHeight - gateHeight / 2);
            }

            scene.AddEnclosureOfHalfPlanes(-1, 100, ceilingLevel, floorLevel);

            var xPos = 1;
            AddObstacleFloor(xPos += 2, 1, 1);
            AddObstacleFloor(xPos += 2, 1, 1.5);
            AddObstacleFloor(xPos += 2, 1, 2);
            AddObstacleFloor(xPos += 2, 1, 2.5);
            AddObstacleCeiling(xPos += 2, 1, 1);
            AddObstacleCeiling(xPos += 2, 1, 1.5);
            AddObstacleCeiling(xPos += 2, 1, 2);
            AddObstacleCeiling(xPos += 2, 1, 2.5);
            AddObstacleGate(xPos += 2, 1, 2, 3);
            AddObstacleGate(xPos += 2, 1, 2, 2);
            AddObstacleGate(xPos += 2, 1, 2, 1.5);
            AddObstacleGate(xPos += 3, 1, 3, 1.5);
            AddObstacleGate(xPos += 3, 1, 1, 1.5);

            scene.AddBoundary(new LeftFacingHalfPlane(xPos + 1, "Exit") { Visible = false });

            return scene;
        }
    }
}
