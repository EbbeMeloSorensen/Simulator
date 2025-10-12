using Craft.Math;
using Simulator.Domain.BodyStates.Interfaces;

namespace Simulator.Domain.BodyStates
{
    public class BodyStateWalker : BodyState, IArtificial, ICyclic
    {
        public Vector2D ArtificialVelocity { get; set; }
        public double Cycle { get; set; }


        public override Vector2D Velocity
        {
            get => NaturalVelocity + ArtificialVelocity;
        }

        protected BodyStateWalker(
            Body body) : base(body)
        {
            ArtificialVelocity = _zeroVector;
        }

        public BodyStateWalker(
            Body body,
            Vector2D position) : base(body, position)
        {
            ArtificialVelocity = _zeroVector;
        }

        public override BodyState Clone()
        {
            return new BodyStateWalker(Body, Position)
            {
                NaturalVelocity = NaturalVelocity,
                ArtificialVelocity = ArtificialVelocity,
                Cycle = Cycle
            };
        }

        public override BodyState Propagate(
            double time,
            Vector2D force)
        {
            var acceleration = force / Body.Mass;
            var nextNaturalVelocity = NaturalVelocity + time * acceleration;
            var nextPosition = Position + time * (NaturalVelocity + ArtificialVelocity);
            var nextCycle = Cycle + 0.003;

            if (nextCycle >= 1.0)
            {
                nextCycle = 0.0;
            }

            return new BodyStateWalker(Body)
            {
                Position = nextPosition,
                NaturalVelocity = nextNaturalVelocity,
                ArtificialVelocity = ArtificialVelocity,
                Cycle = nextCycle
            };
        }
    }
}