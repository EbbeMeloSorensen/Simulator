using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;

public class Quad : SiteComponent
{
    public Vector3D Point1 { get; set; }
    public Vector3D Point2 { get; set; }
    public Vector3D Point3 { get; set; }
    public Vector3D Point4 { get; set; }

    public Quad(string modelId) : base(modelId)
    {
    }
}