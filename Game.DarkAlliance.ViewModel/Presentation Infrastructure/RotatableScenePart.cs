using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class RotatableScenePart : ScenePart
{
    public double Orientation { get; init; }

    public RotatableScenePart(
        string modelId) : base(modelId)
    {
    }
}