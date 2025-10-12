namespace Simulator.Domain
{
    public abstract class Body
    {
        public int Id { get; }
        public double Mass { get; private set; }
        public bool AffectedByGravity { get; private set; }

        public Body(
            int id,
            double mass,
            bool affectedByGravity)
        {
            Id = id;
            Mass = mass;
            AffectedByGravity = affectedByGravity;
        }
    }
}