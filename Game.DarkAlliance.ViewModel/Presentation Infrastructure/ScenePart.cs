using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class ScenePart
{
    public string ModelId { get; init; }
    public Transform3D Transform { get; init; }
    public Material Material { get; init; }

    public ScenePart(
        string modelId,
        Transform3D transform,
        Material material)
    {
        ModelId = modelId;
        Transform = transform;
        Material = material;
    }
}