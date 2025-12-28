using Craft.Simulation.Bodies;

namespace Game.DarkAlliance.ViewModel.Bodies
{
    public class Player : CircularBody
    {
        public Player(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}
