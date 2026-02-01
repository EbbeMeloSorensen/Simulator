namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;

public abstract class SiteComponent
{
    public string ModelId { get; init; }

    protected SiteComponent(
        string modelId)
    {
        ModelId = modelId;
    }
}