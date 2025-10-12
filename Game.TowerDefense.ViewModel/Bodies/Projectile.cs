using Simulator.Domain;

namespace Game.TowerDefense.ViewModel.Bodies;

public class Projectile : CircularBody
{
    public Projectile(
        int id,
        double radius) : base(id, radius, 1, false)
    {
    }
}