namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;

public class RotatableSiteComponent : SiteComponentPlaceable
{
    public double Orientation { get; init; }

    public RotatableSiteComponent(
        string modelId) : base(modelId)
    {
    }
}