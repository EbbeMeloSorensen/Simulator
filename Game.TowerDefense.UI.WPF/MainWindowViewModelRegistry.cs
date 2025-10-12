using StructureMap;

namespace Game.TowerDefense.UI.WPF
{
    public class MainWindowViewModelRegistry : Registry
    {
        public MainWindowViewModelRegistry()
        {
            Scan(_ =>
            {
                _.WithDefaultConventions();
                _.AssembliesFromApplicationBaseDirectory(d => d.FullName.StartsWith("Craft"));
                _.AssembliesFromApplicationBaseDirectory(d => d.FullName.StartsWith("Game.FlappyBird"));
                _.LookForRegistries();
            });
        }
    }
}
