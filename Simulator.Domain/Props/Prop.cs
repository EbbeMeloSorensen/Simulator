using Craft.Math;

namespace Simulator.Domain.Props
{
    // A prop is a view only part of the scene, i.e. it doesn't affect the simulation but is
    // only concerned with stuff that should be visible in the scene
    public abstract class Prop
    {
        public int Id { get; }

        protected Prop(
            int id)
        {
            Id = id;
        }

        public abstract double DistanceToPoint(
            Vector2D point);
    }
}
