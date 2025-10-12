using Craft.Math;

namespace Simulator.Domain.BodyStates
{
    public class BodyState
    {
        protected static readonly Vector2D _zeroVector = new Vector2D(0, 0);

        public Body Body { get; }
        public Vector2D Position { get; set; }
        
        // Dette er den velocity, man initialiserer en bodystate med, og derudover er det den,
        // der påvirkes af acceleration og dermed af kræfter, der virker på bodyen
        public Vector2D NaturalVelocity { get; set; }

        // Dette er den "samlede" velocity, der afhænger af, hvilken bodystate, der er tale om.
        // Nogle af operatorer, der regner på staten, har brug for denne, og de skal helst ikke kende til detaljerne i
        // hvordan det regnes ud, så det bør køres polymorfisk
        public virtual Vector2D Velocity 
        {
            get => NaturalVelocity; 
        }

        protected BodyState(
            Body body)
        {
            Body = body;
            NaturalVelocity = _zeroVector;
        }

        public BodyState(
            Body body,
            Vector2D position)
        {
            Body = body;
            Position = position;
            NaturalVelocity = _zeroVector;
        }

        public virtual BodyState Clone()
        {
            return new BodyState(Body, Position)
            {
                NaturalVelocity = NaturalVelocity
            };
        }

        public virtual BodyState Propagate(
            double time,
            Vector2D force)
        {
            // Todo: Consider calcualting an average acceleration, like you do with velocity

            var acceleration = force / Body.Mass;
            var nextNaturalVelocity = NaturalVelocity + time * acceleration;

            // Notice that we propagate the body using the average of the former and the next velocity
            //var nextPosition = Position + time * nextNaturalVelocity; // Old - leads to big discrepancy between numerical and analytical result
            var nextPosition = Position + time * (NaturalVelocity + nextNaturalVelocity) / 2;

            return new BodyState(Body)
            {
                Position = nextPosition,
                NaturalVelocity = nextNaturalVelocity
            };
        }
    }
}
