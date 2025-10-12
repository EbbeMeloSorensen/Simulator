using Craft.Math;
using Simulator.Domain.BodyStates.Interfaces;

namespace Simulator.Domain.BodyStates
{
    public class BodyStateEnemy : BodyState, ILife
    {
        private int _indexOfNextWayPoint;

        public Path Path { get; set; }
        public double Speed { get; set; }
        public double DistanceCovered { get; set; }
        public int Life { get; set; }

        protected BodyStateEnemy(
            Body body) : base(body)
        {
        }

        public BodyStateEnemy(
            Body body, 
            Vector2D position) : base(body, position)
        {
        }

        public override BodyState Clone()
        {
            return new BodyStateEnemy(Body, Position)
            {
                _indexOfNextWayPoint = _indexOfNextWayPoint,
                Path = Path,
                Speed = Speed,
                NaturalVelocity = NaturalVelocity,
                DistanceCovered = DistanceCovered,
                Life = Life
            };
        }

        public override BodyState Propagate(
            double time,
            Vector2D force)
        {
            var nextWayPoint = Path.WayPoints[_indexOfNextWayPoint];
            var vectorToNextWayPoint = nextWayPoint - Position;
            var distanceToNextWayPoint = vectorToNextWayPoint.Length;
            var distanceIncrement = time * Speed;

            if (distanceIncrement >= distanceToNextWayPoint)
            {
                return new BodyStateEnemy(Body)
                {
                    Position = nextWayPoint,
                    NaturalVelocity = NaturalVelocity,
                    _indexOfNextWayPoint = (_indexOfNextWayPoint + 1) % Path.WayPoints.Count,
                    Path = Path,
                    Speed = Speed,
                    DistanceCovered = DistanceCovered + distanceToNextWayPoint,
                    Life = Life
                };
            }

            var displacement = distanceIncrement * vectorToNextWayPoint.Normalize();

            var nextPosition = Position + displacement;

            return new BodyStateEnemy(Body)
            {
                Position = nextPosition,
                NaturalVelocity = NaturalVelocity,
                _indexOfNextWayPoint = _indexOfNextWayPoint,
                Path = Path,
                Speed = Speed,
                DistanceCovered = DistanceCovered + displacement.Length,
                Life = Life
            };
        }
    }
}
