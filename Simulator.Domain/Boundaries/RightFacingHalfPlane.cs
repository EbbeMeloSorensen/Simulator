using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class RightFacingHalfPlane : IHalfPlane
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public static Vector2D _surfaceNormal;
        public double X { get; }

        static RightFacingHalfPlane()
        {
            _surfaceNormal = new Vector2D(1, 0);
        }

        public Vector2D SurfaceNormal
        {
            get
            {
                return _surfaceNormal;
            }
        }

        public RightFacingHalfPlane(
            double x,
            string tag = null)
        {
            Visible = true;
            X = x;
            Tag = tag;
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            return point.X - X;
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
                    return bodyState.Position.X - body.Width / 2 - X;
                }
                default:
                    throw new ArgumentException();
            }
        }

        public double ProjectVectorOntoSurfaceNormal(
            Vector2D vector)
        {
            return vector.X;
        }

        public bool Intersects(
            BodyState bodyState)
        {
            return !(DistanceToBody(bodyState) > 0.0);
        }
    }
}