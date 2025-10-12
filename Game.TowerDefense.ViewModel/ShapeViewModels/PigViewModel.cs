using System.Windows.Media;

namespace Game.TowerDefense.ViewModel.ShapeViewModels;

public class PigViewModel : EnemyViewModel
{
    private static readonly Matrix _correctionMatrix;

    public override Matrix CorrectionMatrix => _correctionMatrix;
    public override string ImagePath => @"..\Images\Enemy1.png";

    static PigViewModel()
    {
        _correctionMatrix = new Matrix(1, 0, 0, 1, 0, 0);
    }
}