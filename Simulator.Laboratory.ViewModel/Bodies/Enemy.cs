using Simulator.Domain;
using Simulator.Domain.Bodies;

namespace Simulator.Laboratory.ViewModel.Bodies
{
    public class Enemy : CircularBody
    {
        public Enemy(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}