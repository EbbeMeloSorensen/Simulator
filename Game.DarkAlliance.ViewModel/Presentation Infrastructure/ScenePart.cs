using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class ScenePart
{
    public string ModelId { get; init; }
    public Vector3D Position { get; init; }
    public double Orientation { get; init; }

    public ScenePart(
        string modelId,
        Vector3D position,
        double orientation)
    {
        ModelId = modelId;
        Position = position;
        Orientation = orientation;
    }
}