using System.Windows.Media;
using Simulator.ViewModel.ShapeViewModels;

namespace Game.TowerDefense.ViewModel.ShapeViewModels;

public abstract class EnemyViewModel : TaggedEllipseViewModel
{
    public abstract Matrix CorrectionMatrix
    {
        get;
    }

    public abstract string ImagePath
    {
        get;
    }
   
}