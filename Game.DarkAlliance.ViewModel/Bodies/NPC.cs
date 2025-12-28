using Craft.Simulation.Bodies;

namespace Game.DarkAlliance.ViewModel.Bodies
{
    public class NPC : CircularBody
    {
        public NPC(
            int id,
            double radius,
            string tag) : base(id, radius, 1, false, false, tag)
        {
        }
    }
}
