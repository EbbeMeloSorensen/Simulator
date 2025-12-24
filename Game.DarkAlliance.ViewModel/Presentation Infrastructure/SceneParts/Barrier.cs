using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;

public class Barrier : ScenePart
{
    public List<Vector3D> BarrierPoints { get; set; }

    public Barrier(string modelId) : base(modelId)
    {
    }
}