using Craft.Math;
using Simulator.Domain.BodyStates;

namespace Simulator.Domain.Boundaries
{
    public interface ILineSegment : IBoundary
    {
        Vector2D Point1 { get; }
        Vector2D Point2 { get; }
        Vector2D SurfaceNormal { get; }

        double ProjectVectorOntoSurfaceNormal(
            Vector2D vector);

        bool IsVectorPointingInSameDirectionAsLineSegmentVector(
            Vector2D vector);

        LineSegmentPart ClosestPartOfLineSegment(
            Vector2D point);

        double CalculateOvershootDistance(
            BodyState bodyState);
    }
}