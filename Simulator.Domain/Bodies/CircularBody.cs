namespace Simulator.Domain.Bodies
{
    public class CircularBody : Body
    {
        public double Radius { get; }

        public CircularBody(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, mass, affectedByGravity)
        {
            Radius = radius;
        }
    }
}
