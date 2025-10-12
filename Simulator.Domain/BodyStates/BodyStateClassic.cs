using Craft.Math;
using Simulator.Domain.BodyStates.Interfaces;

namespace Simulator.Domain.BodyStates
{
    public class BodyStateClassic : BodyState, IArtificial, IOrientation, ILife
    {
        // Dette er en velocity, som kan sættes UAFHÆNGIGT AF HVILKE KRÆFTER, der virker på en body.
        // Den bruges både for bodies med orientering og til at styre en body med keyboardet 
        // Den bruges bl.a. i følgende scener:
        //   Rotation2: En kugle med orientering, der automatisk bevæger sig rundt i en cirkel
        //   Rotation4: Hvor man STYRER en kugle med orientering
        //   Platformer-spillene: Hvor man styrer en kasse
        //   Shoot'em up-spillene: Hvor man styrer en kugle
        public Vector2D ArtificialVelocity { get; set; }

        // Denne bruges indtil videre kun i rocket. Der er imidlertid en property ved navn
        // EffectiveCustomForce i denne klasse, der trækker på den, og som kaldes fra Calculatoren
        public Vector2D CustomForce { get; set; }

        // Dette er en størrelse, som viewmodellen kan sætte arbitrært. Det er normalt at ændre på
        // den i handleren PostPropagation callback funktionen, og hvis den ryger ned på 0, kan man
        // f.eks. fjerne den fra tilstanden (som hvis en enemy ryger ned på 0) eller sågar stoppe
        // animationen (som hvis en player ryger ned på 0)
        public int Life { get; set; }

        public double Orientation { get; set; }

        public double RotationalSpeed { get; set; }

        public override Vector2D Velocity
        {
            get => NaturalVelocity + ArtificialVelocity.Rotate(-Orientation);
        }

        // Custom Force defineres kun for bodies med en orientering såsom den i rocket spillet.
        // Det er en mulighed for at tilføje en kraft ud over dem, der ellers gælder, som f.eks. drivkraften
        // fra en raketmotor. Den bruges i Calculatoren PropagateState method 
        public Vector2D EffectiveCustomForce => CustomForce.Rotate(-Orientation);

        protected BodyStateClassic(
            Body body) : base(body)
        {
            ArtificialVelocity = _zeroVector;
            CustomForce = _zeroVector;
        }

        public BodyStateClassic(
            Body body, 
            Vector2D position) : base(body, position)
        {
            ArtificialVelocity = _zeroVector;
            CustomForce = _zeroVector;
        }

        public override BodyState Clone()
        {
            return new BodyStateClassic(Body, Position)
            {
                NaturalVelocity = NaturalVelocity,
                ArtificialVelocity = ArtificialVelocity,
                Orientation = Orientation,
                RotationalSpeed = RotationalSpeed,
                CustomForce = CustomForce,
                Life = Life
            };
        }

        public override BodyState Propagate(
            double time,
            Vector2D force)
        {
            // Todo: Propagate in a manner similar to that of the base class BodyState, i.e. where you average the velocity

            var acceleration = force / Body.Mass;
            var nextNaturalVelocity = NaturalVelocity + time * acceleration;
            var nextPosition = Position + time * (NaturalVelocity + ArtificialVelocity.Rotate(-Orientation));
            var nextOrientation = Orientation + time * RotationalSpeed;

            return new BodyStateClassic(Body)
            {
                Position = nextPosition,
                NaturalVelocity = nextNaturalVelocity,
                ArtificialVelocity = ArtificialVelocity,
                Orientation = nextOrientation,
                RotationalSpeed = RotationalSpeed,
                CustomForce = CustomForce,
                Life = Life
            };
        }
    }
}
