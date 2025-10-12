using Simulator.Domain;

namespace Simulator.Laboratory.ViewModel.Bodies
{
    public class Player1Circular : CircularBody
    {
        public Player1Circular(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}