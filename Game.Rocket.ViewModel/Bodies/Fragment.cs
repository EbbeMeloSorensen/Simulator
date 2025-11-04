using Simulator.Domain;
using Simulator.Domain.Bodies;

namespace Game.Rocket.ViewModel.Bodies
{
    public class Fragment : CircularBody
    {
        public Fragment(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}