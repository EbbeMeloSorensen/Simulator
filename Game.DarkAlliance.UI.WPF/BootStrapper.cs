using StructureMap;
using Game.DarkAlliance.ViewModel;

namespace Game.DarkAlliance.UI.WPF
{
    public class BootStrapper
    {
        public MainWindowViewModel MainWindowViewModel
        {
            get
            {
                return Container.For<MainWindowViewModelRegistry>().GetInstance<MainWindowViewModel>();
            }
        }
    }
}
