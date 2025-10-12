using System;
using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public class DownFacingHalfPlane : IHalfPlane
    {
        public bool Visible { get; set; }
        public string Tag { get; }
        public static Vector2D _surfaceNormal;
        public double Y { get; }

        static DownFacingHalfPlane()
        {
            _surfaceNormal = new Vector2D(0, 1);
        }

        public Vector2D SurfaceNormal
        {
            get
            {
                return _surfaceNormal;
            }
        }

        public DownFacingHalfPlane(
            double y,
            string tag = null)
        {
            Visible = true;
            Y = y;
            Tag = tag;
        }

        public double DistanceToPoint(
            Vector2D point)
        {
            return point.Y - Y;
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
                    return bodyState.Position.Y - body.Height / 2 - Y;
                }
                default:
                    throw new ArgumentException();
            }
        }

        public double ProjectVectorOntoSurfaceNormal(
            Vector2D vector)
        {
            return vector.Y;
        }

        public bool Intersects(
            BodyState bodyState)
        {
            return !(DistanceToBody(bodyState) > 0.0);
        }
    }
}