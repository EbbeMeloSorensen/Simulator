using Simulator.Domain;

namespace Game.TowerDefense.ViewModel
{
    public class Level : Craft.DataStructures.Graph.State
    {
        public Scene Scene { get; set; }

        public Level(
            string name) : base(name)
        {
        }
    }
}
