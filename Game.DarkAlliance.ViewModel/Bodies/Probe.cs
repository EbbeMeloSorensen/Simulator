using Craft.Simulation.Bodies;

namespace Game.DarkAlliance.ViewModel.Bodies
{
    public class Probe : CircularBody
    {
        public Probe(
            int id,
            double radius,
            double mass,
            bool affectedByGravity,
            bool affectedByBoundaries) : base(id, radius, mass, affectedByGravity, affectedByBoundaries)
        {
        }
    }
}
