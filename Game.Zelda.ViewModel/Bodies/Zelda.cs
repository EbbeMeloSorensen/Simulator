using Simulator.Domain;
using Simulator.Domain.Bodies;

namespace Game.Zelda.ViewModel.Bodies
{
    public class Zelda : CircularBody
    {
        public Zelda(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}