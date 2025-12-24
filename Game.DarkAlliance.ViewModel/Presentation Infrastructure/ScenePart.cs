namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public abstract class ScenePart
{
    public string ModelId { get; init; }

    public ScenePart(
        string modelId)
    {
        ModelId = modelId;
    }
}