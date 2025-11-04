using Simulator.Domain;
using Simulator.Domain.Bodies;

namespace Game.TowerDefense.ViewModel.Bodies.Enemies;

public class Enemy : CircularBody
{
    public Enemy(
        int id,
        double radius) : base(id, radius, 1, false)
    {
    }
}