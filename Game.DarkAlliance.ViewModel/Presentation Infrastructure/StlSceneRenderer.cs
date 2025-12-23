using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class StlSceneRenderer : ISceneRenderer
{
    public Model3D Build(SceneDefinition sceneDefinition)
    {
        var group = new Model3DGroup();

        foreach (var part in sceneDefinition.Parts)
        {
            var mesh = StlMeshLoader.Load(part.ModelId);

            var model = new GeometryModel3D
            {
                Geometry = mesh,
                Material = part.Material,
                BackMaterial = part.Material,
                Transform = part.Transform
            };

            group.Children.Add(model);
        }

        return group;
    }
}