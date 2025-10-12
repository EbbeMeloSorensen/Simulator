using System;
using System.Linq;
using System.Collections.Generic;
using Craft.Math;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;
using Simulator.Domain.Props;

namespace Simulator.Domain
{
    public enum OutcomeOfCollisionBetweenBodyAndBoundary
    {
        Block,
        Reflect
    }

    public enum OutcomeOfCollisionBetweenTwoBodies
    {
        ElasticCollision,
        Block,
        Ignore
    }

    public delegate void InitializationCallback(
        State initialState,
        string message);

    public delegate bool InteractionCallBack(
        KeyboardState keyboardState,
        KeyboardState keyboardEvents,
        MouseClickPosition? mouseClickPosition,
        Dictionary<int, List<BoundaryCollisionReport>> collisions, // Collisions since last consumption of current state
        State currentState);

    public delegate OutcomeOfCollisionBetweenBodyAndBoundary CollisionBetweenBodyAndBoundaryOccuredCallBack(
        Body body);

    public delegate bool CheckForCollisionBetweenBodiesCallback(
        Body body1, 
        Body body2);

    public delegate OutcomeOfCollisionBetweenTwoBodies CollisionBetweenTwoBodiesOccuredCallBack(
        Body body1, 
        Body body2);

    public delegate PostPropagationResponse PostPropagationCallBack(
        State propagatedState, 
        List<BoundaryCollisionReport> boundaryCollisionReports,
        List<BodyCollisionReport> bodyCollisionReports);

    public enum StandardInteractionCallback
    {
        DungeonCrawler8Directions,
        Platformer
    }

    public enum SceneViewMode
    {
        Stationary,
        FocusOnCenterOfMass,
        FocusOnFirstBody,
        MaintainFocusInVicinityOfPoint
    }

    public class Scene
    {
        private StandardInteractionCallback _standardInteractionCallback;

        public string Name { get; }
        public Point2D InitialWorldWindowUpperLeft { get; }
        public Point2D InitialWorldWindowLowerRight { get; }
        public List<IBoundary> Boundaries { get; }
        public List<Prop> Props { get; }
        public State InitialState { get; }
        public double StandardGravity { get; }
        public double GravitationalConstant { get; }
        public double CoefficientOfFriction { get; }
        public double TimeFactor { get; }
        public bool HandleBodyCollisions { get; }
        public double DeltaT { get; }
        public SceneViewMode ViewMode { get; }
        public Point2D WorldWindowUpperLeftLimit { get; }
        public Point2D WorldWindowBottomRightLimit { get; }
        public double MaxOffsetXFraction { get; }
        public double MaxOffsetYFraction { get; }
        public double CorrectionXFraction { get; }
        public double CorrectionYFraction { get; }

        public bool IncludeCustomForces { get; set; }
        public int FinalStateIndex { get; set; }

        public InitializationCallback InitializationCallback { get; set; }

        public CheckForCollisionBetweenBodiesCallback CheckForCollisionBetweenBodiesCallback { get; set; }

        public CollisionBetweenBodyAndBoundaryOccuredCallBack CollisionBetweenBodyAndBoundaryOccuredCallBack { get; set; }

        public CollisionBetweenTwoBodiesOccuredCallBack CollisionBetweenTwoBodiesOccuredCallBack { get; set; }

        public PostPropagationCallBack PostPropagationCallBack { get; set; }

        public InteractionCallBack InteractionCallBack { get; set; }

        public StandardInteractionCallback StandardInteractionCallback
        {
            get { return _standardInteractionCallback; }
            set
            {
                _standardInteractionCallback = value;

                switch(_standardInteractionCallback)
                {
                    case StandardInteractionCallback.DungeonCrawler8Directions:
                    {
                        InteractionCallBack = (keyboardState, keyboardEvents, mouseClickPosition, collisions, currentState) =>
                        {
                            var currentStateOfMainBody = currentState.BodyStates.First() as BodyStateClassic;
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
                                var speed = 2;
                                newArtificialVelocity = speed * newMovementDirection.Normalize();
                            }

                            if ((newArtificialVelocity - currentArtificialVelocity).Length < 0.01)
                            {
                                return false;
                            }

                            currentStateOfMainBody.ArtificialVelocity = newArtificialVelocity;

                            return true;
                        };

                        break;
                    }
                    case StandardInteractionCallback.Platformer:
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public Scene(
            string name,
            Point2D initialWorldWindowUpperLeft,
            Point2D initialWorldWindowLowerRight,
            State initialState,
            double standardGravity = 9.82,
            double gravitationalConstant = 6.674E-11,
            double coefficientOfFriction = 0.0,
            double timeFactor = 1.0,
            bool handleBodyCollisions = true,
            double deltaT = 0.001,
            SceneViewMode viewMode = SceneViewMode.Stationary,
            double worldWindowUpperLeftLimitX = double.MinValue,
            double worldWindowUpperLeftLimitY = double.MinValue,
            double worldWindowBottomRightLimitX = double.MaxValue,
            double worldWindowBottomRightLimitY = double.MaxValue,
            double maxOffsetXFraction = 0.25,
            double maxOffsetYFraction = 0.25,
            double correctionXFraction = 0.5,
            double correctionYFraction = 0.5)
        {
            Name = name;
            InitialWorldWindowUpperLeft = initialWorldWindowUpperLeft;
            InitialWorldWindowLowerRight = initialWorldWindowLowerRight;
            InitialState = initialState;
            Boundaries = new List<IBoundary>();
            Props = new List<Prop>();
            StandardGravity = standardGravity;
            GravitationalConstant = gravitationalConstant;
            CoefficientOfFriction = coefficientOfFriction;
            TimeFactor = timeFactor;
            HandleBodyCollisions = handleBodyCollisions;
            DeltaT = deltaT;
            ViewMode = viewMode;
            WorldWindowUpperLeftLimit = new Point2D(worldWindowUpperLeftLimitX, worldWindowUpperLeftLimitY);
            WorldWindowBottomRightLimit = new Point2D(worldWindowBottomRightLimitX, worldWindowBottomRightLimitY);
            MaxOffsetXFraction = maxOffsetXFraction;
            MaxOffsetYFraction = maxOffsetYFraction;
            CorrectionXFraction = correctionXFraction;
            CorrectionYFraction = correctionYFraction;
            FinalStateIndex = int.MaxValue;
        }

        public void AddBoundary(
            IBoundary boundary)
        {
            Boundaries.Add(boundary);
        }

        public void AddEnclosureOfHalfPlanes(
            double x0,
            double x1,
            double y0,
            double y1)
        {
            AddBoundary(new RightFacingHalfPlane(x0));
            AddBoundary(new LeftFacingHalfPlane(x1));
            AddBoundary(new DownFacingHalfPlane(y0));
            AddBoundary(new UpFacingHalfPlane(y1));
        }

        public void AddRectangularBoundary(
            double x0,
            double x1,
            double y0,
            double y1,
            bool visible = true)
        {
            AddBoundary(new HorizontalLineSegment(y0, x0, x1) { Visible = visible });
            AddBoundary(new HorizontalLineSegment(y1, x0, x1) { Visible = visible });
            AddBoundary(new VerticalLineSegment(x0, y0, y1) { Visible = visible });
            AddBoundary(new VerticalLineSegment(x1, y0, y1) { Visible = visible });
        }
    }
}
