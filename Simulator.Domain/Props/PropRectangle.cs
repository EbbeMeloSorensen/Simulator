using Craft.Math;

namespace Simulator.Domain.Props
{
    public class PropRectangle : Prop
    {
        public PropRectangle(
            int id,
            double width,
            double height,
            Vector2D position) : base(id)
        {
            Width = width;
            Height = height;
            Position = position;
        }

        public double Width { get; }
        public double Height { get; }
        public Vector2D Position { get; }

        public override double DistanceToPoint(
            Vector2D point)
        {
            // Dette burde kunne gøres billigere og simplere ved at transformere
            // punktet ind i rektanglets koordinatsystem
            var x = point.X;
            var y = point.Y;

            var x0 = Position.X - Width / 2;
            var x1 = Position.X + Width / 2;
            var y0 = Position.Y - Height / 2;
            var y1 = Position.Y + Height / 2;

            if (x < x0)
            {
                if (y < y0)
                {
                    // Lower left quadrant
                    var dx = x0 - x;
                    var dy = y0 - y;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
                
                if (y > y1)
                {
                    // Upper left quadrant
                    var dx = x0 - x;
                    var dy = y - y1;
                    return Math.Sqrt(dx * dx + dy * dy);
                }

                // To the left
                return x0 - x;
            }
            
            if (x > x1)
            {
                if (y < y0)
                {
                    // Lower right quadrant
                    var dx = x - x1;
                    var dy = y0 - y;
                    return Math.Sqrt(dx * dx + dy * dy);
                }

                if (y > y1)
                {
                    // Upper right quadrant
                    var dx = x - x1;
                    var dy = y - y1;
                    return Math.Sqrt(dx * dx + dy * dy);
                }

                // To the right
                return x - x1;
            }
            
            if (y < y0)
            {
                // Below
                return y0 - y;
            }
            
            if (y > y1)
            {
                // Above
                return y - y1;
            }

            // Within
            return 0.0;
        }
    }
}