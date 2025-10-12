using System;
using Craft.Math;

namespace Simulator.Domain.Props
{
    public class PropRotatableRectangle : Prop
    {
        public double Width { get; }
        public double Height { get; }
        public Vector2D Position { get; }
        public double Orientation { get; }

        public PropRotatableRectangle(
            int id,
            double width,
            double height,
            Vector2D position,
            double orientation) : base(id)
        {
            Width = width;
            Height = height;
            Position = position;
            Orientation = orientation;
        }

        public override double DistanceToPoint(
            Vector2D point)
        {
            // Mon ikke det smarteste er at rotere punktet på samme måde som rektanglet,
            // og så bare bruge den samme beregning som for et simpelt rektangel?

            // 1: translater punktet
            var pt1 = point - Position;
            var cosAngle = Math.Cos(-Orientation);
            var sinAngle = Math.Sin(-Orientation);

            var pt2 = new Vector2D(
                pt1.X * cosAngle + pt1.Y * sinAngle,
                pt1.Y * cosAngle - pt1.X * sinAngle);

            return DistanceToPoint_Helper(pt2);
        }

        private double DistanceToPoint_Helper(
            Vector2D point)
        {
            var x = point.X;
            var y = point.Y;

            var x1 = Width / 2;
            var x0 = -x1;
            var y1 = Height / 2;
            var y0 = -y1;

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