using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class HalfPlane : IHalfPlane
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public Vector2D Point { get; }
        public Vector2D SurfaceNormal { get; }

        public HalfPlane(
            Vector2D point,
            Vector2D surfaceNormal,
            string tag = null)
        {
            Visible = true;
            Point = point;
            SurfaceNormal = surfaceNormal.Normalize();
            Tag = tag;
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            return Vector2D.DotProduct(SurfaceNormal, point - Point);
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
            return !(DistanceToBody(bodyState) > 0.0);
        }

        public double ProjectVectorOntoSurfaceNormal(Vector2D vector)
        {
            return Vector2D.DotProduct(SurfaceNormal, vector);
        }
    }
}
