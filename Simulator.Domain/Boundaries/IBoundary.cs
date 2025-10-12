using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public interface IBoundary
    {
        bool Visible { get; }

        string Tag { get; }

        double DistanceToPoint(
            Vector2D point);

        double DistanceToBody(
            BodyState bodyState);

        bool Intersects(
            BodyState bodyState);
    }
}
