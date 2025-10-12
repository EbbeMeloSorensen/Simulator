using Game.TowerDefense.ViewModel;
using StructureMap;

namespace Game.TowerDefense.UI.WPF
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
