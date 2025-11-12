using Craft.Logging;
using Craft.Math;
using Craft.ViewModels.Geometry2D.ScrollFree;
using GalaSoft.MvvmLight;
using Simulator.Domain;
using Simulator.Domain.Bodies;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;
using Simulator.Domain.Engine;
using Simulator.ViewModel;

namespace Game.DarkAlliance.ViewModel
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

            var scene = GenerateScene_Exploration();

            GeometryEditorViewModel.InitializeWorldWindow(
                scene.InitialWorldWindowFocus(),
                scene.InitialWorldWindowSize(),
                false);

            _sceneViewController.ActiveScene = scene;

            // Denne bruges til at erstatte default ellipsen, som er dark slate gray, med en, der er orange og med en rød stribe
            //ShapeSelectorCallback shapeSelectorCallback2 = (bs) =>
            //{
            //    if (!(bs.Body is CircularBody))
            //    {
            //        throw new InvalidOperationException();
            //    }

            //    var circularBody = bs.Body as CircularBody;

            //    var bsc = bs as BodyStateClassic;
            //    var orientation = bsc == null ? 0 : bsc.Orientation;

            //    return new RotatableEllipseViewModel
            //    {
            //        Width = 2 * circularBody.Radius,
            //        Height = 2 * circularBody.Radius,
            //        Orientation = orientation
            //    };
            //};
        }

        public void HandleLoaded()
        {
            _sceneViewController.ResetScene();
            Engine.StartOrResumeAnimation();
        }

        private Scene GenerateScene_BouncingBall()
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

        private Scene GenerateScene_Exploration()
        {
            var ballRadius = 0.125;
            var initialBallPosition = new Vector2D(1, 1.7);

            var initialState = new State();
            initialState.AddBodyState(new BodyStateClassic(new CircularBody(1, ballRadius, 1, false), initialBallPosition)
            {
                Orientation = 0.5 * Math.PI
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

            scene.AddRectangularBoundary(-1, 3, -0.3, 2);
            scene.AddRectangularBoundary(0, 2, 0.6, 1.1);

            return scene;
        }
    }
}
