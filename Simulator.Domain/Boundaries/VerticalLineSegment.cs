using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class VerticalLineSegment : ILineSegment
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public static Vector2D _surfaceNormal;
        public double X { get; }
        public double Y0 { get; }
        public double Y1 { get; }

        static VerticalLineSegment()
        {
            _surfaceNormal = new Vector2D(1, 0);
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            var dx = X - point.X;

            if (point.Y < Y0)
            {
                var dy = Y0 - point.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            if (point.Y > Y1)
            {
                var dy = Y1 - point.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            return Math.Abs(dx);
        }

        public double DistanceToBody(
            BodyState bodyState)
        {
            throw new NotImplementedException();
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
                    var x0 = bodyState.Position.X - body.Width / 2;
                    var x1 = bodyState.Position.X + body.Width / 2;
                    var y0 = bodyState.Position.Y - body.Height / 2;
                    var y1 = bodyState.Position.Y + body.Height / 2;

                    return !(X > x1 || 
                             X < x0 || 
                             Y0 > y1 || 
                             Y1 < y0);
                }
                default:
                    throw new ArgumentException();
            }
        }

        public Vector2D Point1 => new Vector2D(X, Y0);

        public Vector2D Point2 => new Vector2D(X, Y1);

        public Vector2D SurfaceNormal => _surfaceNormal;

        public VerticalLineSegment(
            double x,
            double y0,
            double y1,
            string tag = null)
        {
            Visible = true;
            Tag = tag;
            X = x;
            Y0 = y0;
            Y1 = y1;
        }

        public double ProjectVectorOntoSurfaceNormal(
            Vector2D vector)
        {
            return vector.X;
        }

        public bool IsVectorPointingInSameDirectionAsLineSegmentVector(
            Vector2D vector)
        {
            return vector.Y > 0 ? Y1 > Y0 : Y0 > Y1;
        }

        public LineSegmentPart ClosestPartOfLineSegment(
            Vector2D point)
        {
            if (point.Y < Y0)
            {
                return LineSegmentPart.Point1;
            }

            if (point.Y > Y1)
            {
                return LineSegmentPart.Point2;
            }

            return LineSegmentPart.MiddleSection;
        }

        public double CalculateOvershootDistance(
            BodyState bodyState)
        {
            switch (bodyState.Body)
            {
                case CircularBody body:
                {
                    throw new NotImplementedException();
                }
                case RectangularBody body:
                {
                    if (bodyState.NaturalVelocity.X > 0)
                    {
                        return bodyState.Position.X + body.Width / 2 - X;
                    }

                    return X - bodyState.Position.X + body.Width / 2;
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
}