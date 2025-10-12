using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class HorizontalLineSegment : ILineSegment
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public static Vector2D _surfaceNormal;
        public double Y { get; }
        public double X0 { get; }
        public double X1 { get; }

        static HorizontalLineSegment()
        {
            _surfaceNormal = new Vector2D(0, 1);
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            var dy = Y - point.Y;

            if (point.X < X0)
            {
                var dx = X0 - point.X;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            if (point.X > X1)
            {
                var dx = X1 - point.X;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            return Math.Abs(dy);
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

                        return !(Y > y1 ||
                                 Y < y0 ||
                                 X0 > x1 ||
                                 X1 < x0);
                    }
                default:
                    throw new ArgumentException();
            }
        }

        public Vector2D Point1 => new Vector2D(X0, Y);

        public Vector2D Point2 => new Vector2D(X1, Y);

        public Vector2D SurfaceNormal => _surfaceNormal;

        public HorizontalLineSegment(
            double y,
            double x0,
            double x1,
            string tag = null)
        {
            Visible = true;
            Tag = tag;
            Y = y;
            X0 = x0;
            X1 = x1;
        }

        public double ProjectVectorOntoSurfaceNormal(
            Vector2D vector)
        {
            return vector.Y;
        }

        public bool IsVectorPointingInSameDirectionAsLineSegmentVector(
            Vector2D vector)
        {
            return vector.X > 0 ? X1 > X0 : X0 > X1;
        }

        public LineSegmentPart ClosestPartOfLineSegment(
            Vector2D point)
        {
            if (point.X < X0)
            {
                return LineSegmentPart.Point1;
            }

            if (point.X > X1)
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
                        if (bodyState.Velocity.Y > 0)
                        {
                            return bodyState.Position.Y + body.Height / 2 - Y;
                        }

                        return Y - bodyState.Position.Y + body.Height / 2;
                    }
                default:
                    throw new ArgumentException();
            }
        }
    }
}