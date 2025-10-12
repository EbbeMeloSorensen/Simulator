using System;
using Craft.Math;
using Simulator.Domain.BodyStates.Interfaces;

namespace Simulator.Domain.BodyStates
{
    public class BodyStateCannon : BodyState, IOrientation, ICoolDown
    {
        public double Orientation { get; set; }

        public int CoolDown { get; set; }

        public override Vector2D Velocity 
        { 
            get => NaturalVelocity; 
        }

        protected BodyStateCannon(
            Body body) : base(body)
        {
        }

        public BodyStateCannon(
            Body body,
            Vector2D position) : base(body, position)
        {
        }

        public override BodyState Clone()
        {
            return new BodyStateCannon(Body, Position)
            {
                NaturalVelocity = NaturalVelocity,
                Orientation = Orientation,
                CoolDown = CoolDown
            };
        }

        public override BodyState Propagate(
            double time,
            Vector2D force)
        {
            var acceleration = force / Body.Mass;
            var nextNaturalVelocity = NaturalVelocity + time * acceleration;
            var nextPosition = Position + time * NaturalVelocity;

            return new BodyStateCannon(Body)
            {
                Position = nextPosition,
                NaturalVelocity = nextNaturalVelocity,
                Orientation = Orientation,
                CoolDown = Math.Max(0, CoolDown - 1)
            };
        }
    }
}
