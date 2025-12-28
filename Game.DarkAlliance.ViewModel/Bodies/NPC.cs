using Craft.Simulation.Bodies;

namespace Game.DarkAlliance.ViewModel.Bodies
{
    public class NPC : CircularBody
    {
        public NPC(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}
