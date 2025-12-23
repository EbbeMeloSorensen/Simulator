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
            var mesh = StlMeshLoader.Load(@"Assets\low poly guy.stl");

            var humanMaterial = new MaterialGroup();
            humanMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.LightPink)));

            var model = new GeometryModel3D
            {
                Geometry = mesh,
                Material = humanMaterial,
                BackMaterial = humanMaterial,
                Transform = part.Transform
            };

            group.Children.Add(model);
        }

        return group;
    }
}