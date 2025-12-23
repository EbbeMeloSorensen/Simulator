using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class ScenePart
{
    public string ModelId { get; init; }
    public Transform3D Transform { get; init; }
}