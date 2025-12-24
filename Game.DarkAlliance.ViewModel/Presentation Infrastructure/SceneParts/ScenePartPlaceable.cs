using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;

public class ScenePartPlaceable : ScenePart
{
    public Vector3D Position { get; set; }

    public ScenePartPlaceable(
        string modelId) : base(modelId)
    {
    }
}