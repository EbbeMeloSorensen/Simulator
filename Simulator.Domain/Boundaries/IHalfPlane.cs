using Craft.Math;

namespace Simulator.Domain.Boundaries
{
    public interface IHalfPlane : IBoundary
    {
        Vector2D SurfaceNormal { get; }

        double ProjectVectorOntoSurfaceNormal(
            Vector2D vector);
    }
}