using Craft.Math;
using Simulator.Domain;
using Simulator.Domain.BodyStates;
using Simulator.Domain.BodyStates.Interfaces;
using Simulator.Domain.Boundaries;

namespace Simulator.Application
{
    public static class BodyStateExtensions
    {
        public static void HandleElasticCollision(
            this BodyState bodyState1,
            BodyState bodyState2)
        {
            var body1 = bodyState1.Body;
            var body2 = bodyState2.Body;

            var m1 = body1.Mass;
            var m2 = body2.Mass;
            var p1 = bodyState1.Position;
            var p2 = bodyState2.Position;
            var v1 = bodyState1.NaturalVelocity;
            var v2 = bodyState2.NaturalVelocity;

            // Formula from https://en.wikipedia.org/wiki/Elastic_collision (elastic collision in 2d)
            var cmDistSquared = (p1 - p2).SqrLength;
            var factor1 = 2 * m2 / (m1 + m2);
            var factor2 = 2 * m1 / (m1 + m2);
            var v1New = v1 - factor1 * Vector2D.DotProduct(v1 - v2, p1 - p2) * (p1 - p2) / cmDistSquared;
            var v2New = v2 - factor2 * Vector2D.DotProduct(v2 - v1, p2 - p1) * (p2 - p1) / cmDistSquared;

            bodyState1.NaturalVelocity = v1New;
            bodyState2.NaturalVelocity = v2New;
        }

        public static void ReflectVelocity(
            this BodyState bodyState,
            IBoundary boundary,
            Vector2D lineSegmentEndPointInvolvedInCollision,
            Vector2D effectiveSurfaceNormalForCollision)
        {
            if (effectiveSurfaceNormalForCollision != null)
            {
                bodyState.ReflectVelocityAgainstLinearSurface(effectiveSurfaceNormalForCollision);
            }
            else if (boundary is ILineSegment lineSegment)
            {
                if (lineSegmentEndPointInvolvedInCollision == null)
                {
                    bodyState.ReflectVelocityAgainstLinearSurface(lineSegment.SurfaceNormal);
                }
                else
                {
                    bodyState.ReflectVelocityAgainstPoint(lineSegmentEndPointInvolvedInCollision);
                }
            }
            else if (boundary is IHalfPlane halfPlane)
            {
                bodyState.ReflectVelocityAgainstLinearSurface(halfPlane.SurfaceNormal);
            }
            else if (boundary is BoundaryPoint boundaryPoint)
            {
                bodyState.ReflectVelocityAgainstPoint(boundaryPoint.Point);
            }
        }

        // Practically we pass a negated surface normal to this method
        public static void EliminateVelocityComponentTowardsGivenSurfaceNormal(
            this BodyState bodyState,
            Vector2D surfaceNormal)
        {
            if (surfaceNormal == null)
            {
                bodyState.NaturalVelocity = new Vector2D(0, 0);

                if (bodyState is IArtificial)
                {
                    (bodyState as IArtificial).ArtificialVelocity = new Vector2D(0, 0);
                }

                return;
            }

            // Possibly correct the natural velocity
            var dotProduct1 = Vector2D.DotProduct(bodyState.NaturalVelocity, -surfaceNormal);

            if (dotProduct1 > 0)
            {
                bodyState.NaturalVelocity += dotProduct1 * surfaceNormal;
            }

            if (bodyState is IArtificial bsa)
            {
                // Correct the artificial velocity

                if (bodyState is IOrientation bso)
                {
                    var effectiveArtificialVelocity =
                        bsa.ArtificialVelocity.Rotate(-bso.Orientation);

                    var dotProduct2 = Vector2D.DotProduct(effectiveArtificialVelocity, -surfaceNormal);

                    if (dotProduct2 > 0)
                    {
                        effectiveArtificialVelocity += dotProduct2 * surfaceNormal;
                        bsa.ArtificialVelocity = effectiveArtificialVelocity.Rotate(bso.Orientation);
                    }
                }
                else
                {
                    var dotProduct2 = Vector2D.DotProduct(bsa.ArtificialVelocity, -surfaceNormal);

                    if (dotProduct2 > 0)
                    {
                        bsa.ArtificialVelocity += dotProduct2 * surfaceNormal;
                    }
                }
            }
        }

        private static void ReflectVelocityAgainstLinearSurface(
            this BodyState bodyState,
            Vector2D surfaceNormal)
        {
            bodyState.NaturalVelocity -= Vector2D.DotProduct(bodyState.NaturalVelocity, surfaceNormal) * 2 * surfaceNormal;
        }

        // Dette er en variation af den formel, som vi bruger til stød mellem kugler,
        // hvor vi effektivt opererer med at den ene kugle er uendeligt tung
        private static void ReflectVelocityAgainstPoint(
            this BodyState bodyState,
            Vector2D point)
        {
            switch (bodyState.Body)
            {
                case CircularBody body:
                    {
                        var p1 = bodyState.Position;
                        var p2 = point;
                        var v1 = bodyState.NaturalVelocity;

                        var cmDistSquared = (p1 - p2).SqrLength;
                        bodyState.NaturalVelocity = v1 - 2 * Vector2D.DotProduct(v1, p1 - p2) * (p1 - p2) / cmDistSquared;
                        break;
                    }
                case RectangularBody body:
                    {
                        bodyState.NaturalVelocity = -bodyState.NaturalVelocity;
                        break;
                    }

            }
        }
    }
}
