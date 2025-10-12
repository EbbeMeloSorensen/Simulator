namespace Simulator.Domain
{
    public class RectangularBody : Body
    {
        public double Width { get; }
        public double Height { get; }

        public RectangularBody(
            int id,
            double width,
            double height,
            double mass,
            bool affectedByGravity) : base(id, mass, affectedByGravity)
        {
            Width = width;
            Height = height;
        }
    }
}