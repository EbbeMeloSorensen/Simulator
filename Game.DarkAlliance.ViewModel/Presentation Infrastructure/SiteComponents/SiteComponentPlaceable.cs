using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;

public class SiteComponentPlaceable : SiteComponent
{
    public Vector3D Position { get; set; }

    public SiteComponentPlaceable(
        string modelId) : base(modelId)
    {
    }
}