using Craft.Math;
using Simulator.Domain.BodyStates;
using Simulator.Domain.Boundaries;

namespace Simulator.Domain
{
    public class BoundaryCollisionReport
    {
        public BodyState BodyState { get; }
        public IBoundary Boundary { get; }
        public Vector2D EffectiveSurfaceNormal { get; }

        public BoundaryCollisionReport(
            BodyState bodyState,
            IBoundary boundary,
            Vector2D effectiveSurfaceNormal)
        {
            BodyState = bodyState;
            Boundary = boundary;
            EffectiveSurfaceNormal = effectiveSurfaceNormal;
        }
    }
}