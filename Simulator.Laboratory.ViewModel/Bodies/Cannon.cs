using Simulator.Domain;

namespace Simulator.Laboratory.ViewModel.Bodies
{
    public class Cannon : CircularBody
    {
        public Cannon(
            int id,
            double radius) : base(id, radius, 1.0, false)
        {
        }
    }
}
