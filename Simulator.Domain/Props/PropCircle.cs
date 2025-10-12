using System;
using Craft.Math;

namespace Simulator.Domain.Props
{
    public class PropCircle : Prop
    {
        public double Diameter { get; }
        public Vector2D Position { get; }

        public PropCircle(
            int id,
            double diameter,
            Vector2D position) : base(id)
        {
            Diameter = diameter;
            Position = position;
        }

        public override double DistanceToPoint(
            Vector2D point)
        {
            return Math.Sqrt(point.SquaredDistanceTo(Position)) - Diameter / 2;
        }
    }
}