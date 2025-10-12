using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class BoundaryPoint : IBoundary
    {
        public bool Visible { get; set; }

        public string Tag { get; }

        public Vector2D Point { get; }

        public BoundaryPoint(
            Vector2D point,
            string tag = null)
        {
            Visible = true;
            Point = point;
            Tag = tag;
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            var dx = Point.X - point.X;
            var dy = Point.Y - point.Y;

            if (dx != 0 || dy != 0)
            {
                return Math.Sqrt(dx * dx + dy * dy);
            }

            return 0.0;
        }

        public double DistanceToBody(
            BodyState bodyState)
        {
            switch (bodyState.Body)
            {
                case CircularBody body:
                {
                    return DistanceToPoint(bodyState.Position) - body.Radius;
                }
                case RectangularBody body:
                {
                    throw new NotImplementedException();
                }
                default:
                    throw new ArgumentException();
            }
        }

        public bool Intersects(
            BodyState bodyState)
        {
            switch (bodyState.Body)
            {
                case CircularBody body:
                {
                    return !(DistanceToPoint(bodyState.Position) - body.Radius > 0.0);
                }
                case RectangularBody body:
                {
                    return !(Point.X < bodyState.Position.X - body.Width / 2) && 
                           !(Point.X > bodyState.Position.X + body.Width / 2) && 
                           !(Point.Y < bodyState.Position.Y - body.Height / 2) && 
                           !(Point.Y > bodyState.Position.Y + body.Height / 2);
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
}