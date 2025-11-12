using StructureMap;

namespace Game.Simple.UI.WPF
{
    public class MainWindowViewModelRegistry : Registry
    {
        public MainWindowViewModelRegistry()
        {
            Scan(_ =>
            {
                _.WithDefaultConventions();
                _.AssembliesFromApplicationBaseDirectory(d => d.FullName.StartsWith("Craft"));
                _.LookForRegistries();
            });
        }
    }
}
