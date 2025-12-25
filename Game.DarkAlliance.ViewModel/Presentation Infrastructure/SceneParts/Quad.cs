using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;

public class Quad : ScenePart
{
    public Vector3D Point1 { get; set; }
    public Vector3D Point2 { get; set; }
    public Vector3D Point3 { get; set; }
    public Vector3D Point4 { get; set; }

    public Quad(string modelId) : base(modelId)
    {
    }
}