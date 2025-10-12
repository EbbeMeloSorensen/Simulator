using Craft.Math;
using Simulator.Domain;
using Simulator.Domain.BodyStates.Interfaces;
using Simulator.Domain.BodyStates;

namespace Game.Zelda.ViewModel.BodyStates
{
    public class BodyStateZelda : BodyState, IArtificial
    {
        public Vector2D ArtificialVelocity { get; set; }

        public override Vector2D Velocity
        {
            get => NaturalVelocity + ArtificialVelocity;
        }

        protected BodyStateZelda(
            Body body) : base(body)
        {
            ArtificialVelocity = _zeroVector;
        }

        public BodyStateZelda(
            Body body, 
            Vector2D position) : base(body, position)
        {
            ArtificialVelocity = _zeroVector;
        }

        public override BodyState Clone()
        {
            return new BodyStateZelda(Body, Position)
            {
                NaturalVelocity = NaturalVelocity,
                ArtificialVelocity = ArtificialVelocity
            };
        }

        public override BodyState Propagate(
            double time,
            Vector2D force)
        {
            var acceleration = force / Body.Mass;
            var nextNaturalVelocity = NaturalVelocity + time * acceleration;
            var nextPosition = Position + time * ((NaturalVelocity + nextNaturalVelocity) / 2 + ArtificialVelocity);

            return new BodyStateZelda(Body)
            {
                Position = nextPosition,
                NaturalVelocity = nextNaturalVelocity,
                ArtificialVelocity = ArtificialVelocity
            };
        }
    }
}
