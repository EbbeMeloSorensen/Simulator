using Craft.Math;

namespace Simulator.Domain
{
    public class MouseClickPosition
    {
        public Point2D Position { get; }

        public MouseClickPosition(
            Point2D position)
        {
            Position = position;
        }
    }
}
