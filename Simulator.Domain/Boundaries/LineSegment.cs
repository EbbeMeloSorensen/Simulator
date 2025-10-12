using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class LineSegment : ILineSegment
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public Vector2D Point1 { get; }
        public Vector2D Point2 { get; }
        public Vector2D SurfaceNormal { get; }

        public LineSegment(
            Vector2D point1, 
            Vector2D point2,
            string tag = null)
        {
            Visible = true;
            Tag = tag;
            Point1 = point1;
            Point2 = point2;

            SurfaceNormal = (point2 - point1).Hat().Normalize();
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            var lineSegment2D = new LineSegment2D(
                Point1.AsPoint2D(),
                Point2.AsPoint2D());

            return lineSegment2D.DistanceTo(point.AsPoint2D());
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

        public LineSegmentPart ClosestPartOfLineSegment(
            Vector2D point)
        {
            var lineSegment2D = new LineSegment2D(
                Point1.AsPoint2D(),
                Point2.AsPoint2D());

            return lineSegment2D.ClosestPartOfLineSegment(point.AsPoint2D());
        }

        public double CalculateOvershootDistance(
            BodyState bodyState)
        {
            throw new NotImplementedException();
        }

        public double ProjectVectorOntoSurfaceNormal(
            Vector2D vector)
        {
            return Vector2D.DotProduct(SurfaceNormal, vector);
        }

        public bool IsVectorPointingInSameDirectionAsLineSegmentVector(
            Vector2D vector)
        {
            return Vector2D.DotProduct(vector, Point2 - Point1) > 0;
        }

        public bool Intersects(
            BodyState bodyState)
        {
            return !(DistanceToBody(bodyState) > 0.0);
        }
    }
}
