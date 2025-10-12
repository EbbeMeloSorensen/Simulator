using System.Windows.Media;

namespace Game.TowerDefense.ViewModel.ShapeViewModels;

public class RabbitViewModel : EnemyViewModel
{
    private static readonly Matrix _correctionMatrix;

    public override Matrix CorrectionMatrix => _correctionMatrix;
    public override string ImagePath => @"..\Images\Enemy2.png";

    static RabbitViewModel()
    {
        _correctionMatrix = new Matrix(1.5, 0, 0, 1.5, 0.06, -0.26);
    }
}