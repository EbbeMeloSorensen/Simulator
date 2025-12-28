using Craft.Simulation.Bodies;

namespace Game.DarkAlliance.ViewModel.Bodies
{
    public class Probe : CircularBody
    {
        public Probe(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}
