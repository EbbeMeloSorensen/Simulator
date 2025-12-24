namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;

public class RotatableScenePart : ScenePartPlaceable
{
    public double Orientation { get; init; }

    public RotatableScenePart(
        string modelId) : base(modelId)
    {
    }
}