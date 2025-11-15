using GalaSoft.MvvmLight;
using Craft.Logging;
using Craft.Math;
using Craft.Simulation;
using Craft.Simulation.Bodies;
using Craft.Simulation.BodyStates;
using Craft.Simulation.Boundaries;
using Craft.Simulation.Engine;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Craft.ViewModels.Simulation;

namespace Game.Simple.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ILogger _logger;
        private SceneViewController _sceneViewController;

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

            _sceneViewController = new SceneViewController(Engine, GeometryEditorViewModel);

            var scene = GenerateScene();

            GeometryEditorViewModel.InitializeWorldWindow(
                scene.InitialWorldWindowFocus(),
                scene.InitialWorldWindowSize(),
                false);

            _sceneViewController.ActiveScene = scene;

            Engine.StartOrResumeAnimation();
        }

        private Scene GenerateScene()
        {
            var ballRadius = 0.125;
            var initialBallPosition = new Vector2D(1, -0.125);
            var initialBallVelocity = new Vector2D(2, 0);

            var initialState = new State();
            initialState.AddBodyState(new BodyState(new CircularBody(1, ballRadius, 1, true), initialBallPosition) { NaturalVelocity = initialBallVelocity });

            var name = "Simple Game";
            var standardGravity = 9.82;
            var initialWorldWindowUpperLeft = new Point2D(-1.4, -1.3);
            var initialWorldWindowLowerRight = new Point2D(5, 3);
            var gravitationalConstant = 0.0;
            var coefficientOfFriction = 0.0;
            var timeFactor = 1.0;
            var handleBodyCollisions = false;
            var deltaT = 0.001;

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
                deltaT);

            scene.CollisionBetweenBodyAndBoundaryOccuredCallBack = body => OutcomeOfCollisionBetweenBodyAndBoundary.Reflect;

            scene.AddBoundary(new HalfPlane(new Vector2D(3, -0.3), new Vector2D(-1, 0)));
            scene.AddBoundary(new HalfPlane(new Vector2D(3, 1), new Vector2D(0, -1)));
            scene.AddBoundary(new HalfPlane(new Vector2D(-1, 1), new Vector2D(1, 0)));

            return scene;
        }
    }
}
