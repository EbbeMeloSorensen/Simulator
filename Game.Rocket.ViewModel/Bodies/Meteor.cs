using Simulator.Domain;

namespace Game.Rocket.ViewModel.Bodies
{
    public class Meteor : CircularBody
    {
        public Meteor(
            int id,
            double radius,
            double mass,
            bool affectedByGravity) : base(id, radius, mass, affectedByGravity)
        {
        }
    }
}