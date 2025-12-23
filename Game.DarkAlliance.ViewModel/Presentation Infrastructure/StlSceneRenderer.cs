using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class StlSceneRenderer : ISceneRenderer
{
    public Model3D Build(
        SceneDefinition sceneDefinition)
    {
        var group = new Model3DGroup();

        foreach (var part in sceneDefinition.Parts)
        {
            var model = part.ModelId switch
            {
                "human male" => GenerateHumanMale(part.Position),
                "human female" => GenerateHumanFemale(part.Position),
                _ => throw new NotSupportedException($"Unknown Model ID '{part.ModelId}'.")
            };

            group.Children.Add(model);
        }

        return group;
    }

    private GeometryModel3D GenerateHumanMale(
        Vector3D position)
    {
        var mesh = StlMeshLoader.Load(@"Assets\male.stl");

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightPink));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material
        };

        // Basic transform to normalize the model in this coordinate system
        model.Rotate(new Vector3D(1, 0, 0), -90);
        model.Scale(0.003, 0.003, 0.003);

        // Position in this scene
        model.Translate(position.X, position.Y, position.Z);

        return model;
    }

    private GeometryModel3D GenerateHumanFemale(
        Vector3D position)
    {
        var mesh = StlMeshLoader.Load(@"Assets\female.stl");

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.DarkOrange));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material
        };

        // Basic transform to normalize the model in this coordinate system
        model.Rotate(new Vector3D(1, 0, 0), -90);
        model.Translate(-50, 0, -40);
        model.Scale(0.03, 0.03, 0.03);

        // Position in this scene
        model.Translate(position.X, position.Y, position.Z);

        return model;
    }
}