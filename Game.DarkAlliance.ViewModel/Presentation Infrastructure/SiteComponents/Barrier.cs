using Craft.Math;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;

public class Barrier : SiteComponent
{
    public List<Vector3D> BarrierPoints { get; set; }

    public IEnumerable<Vector2D> BoundaryPoints => BarrierPoints.Select(_ => new Vector2D(_.Z, -_.X));

    public Barrier(string modelId) : base(modelId)
    {
    }
}