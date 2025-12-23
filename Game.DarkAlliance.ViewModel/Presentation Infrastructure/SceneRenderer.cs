using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class SceneRenderer : ISceneRenderer
{
    public Model3D Build(
        SceneDefinition sceneDefinition)
    {
        var group = new Model3DGroup();

        foreach (var part in sceneDefinition.Parts)
        {
            var model = part.ModelId switch
            {
                "human male" => GenerateHumanMale(part.Position, part.Orientation),
                "human female" => GenerateHumanFemale(part.Position, part.Orientation),
                "barrel" => GenerateBarrel(part.Position),
                _ => throw new NotSupportedException($"Unknown Model ID '{part.ModelId}'.")
            };

            group.Children.Add(model);
        }

        return group;
    }

    private GeometryModel3D GenerateHumanMale(
        Vector3D position,
        double orientation = 0)
    {
        //ImportMeshFromFile(
        //    @"Assets\male.stl",
        //    )

        var mesh = StlMeshLoader.Load(@"Assets\male.stl");

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightPink));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material
        };

        // Basic transform to normalize the model in this coordinate system
        model.Rotate(new Vector3D(1, 0, 0), -90);
        model.Translate(0, 0, 0);
        model.Scale(0.003, 0.003, 0.003);

        // Position in this scene
        if (Math.Abs(orientation) > 0.00001)
        {
            model.Rotate(new Vector3D(0, 1, 0), orientation);
        }

        model.Translate(position.X, position.Y, position.Z);

        return model;
    }

    private GeometryModel3D ImportMeshFromFile(
        string path,
        Material material,
        Vector3D basicRotationAxis,
        double basicRotationAngle,
        Vector3D basicTranslation,
        double basicScaleFactor,
        Vector3D position,
        double orientation = 0)
    {
        var mesh = StlMeshLoader.Load(path);

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material
        };

        // Basic transform to normalize the model in this coordinate system
        model.Rotate(basicRotationAxis, basicRotationAngle);
        model.Translate(basicTranslation.X, basicTranslation.Y, basicTranslation.Z);
        model.Scale(basicScaleFactor, basicScaleFactor, basicScaleFactor);

        // Position in this scene
        if (Math.Abs(orientation) > 0.00001)
        {
            model.Rotate(new Vector3D(0, 1, 0), orientation);
        }

        model.Translate(position.X, position.Y, position.Z);

        return model;
    }

    private GeometryModel3D GenerateHumanFemale(
        Vector3D position,
        double orientation = 0)
    {
        var mesh = StlMeshLoader.Load(@"Assets\female.stl");

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.LightPink));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material
        };

        // Basic transform to normalize the model in this coordinate system
        model.Rotate(new Vector3D(1, 0, 0), -90);
        model.Translate(-132.5, 0, 101);
        model.Scale(0.015, 0.015, 0.015);

        // Position in this scene
        if (Math.Abs(orientation) > 0.00001)
        {
            model.Rotate(new Vector3D(0, 1, 0), orientation);
        }

        model.Translate(position.X, position.Y, position.Z);

        return model;
    }

    private GeometryModel3D GenerateBarrel(
        Vector3D position)
    {
        var mesh = MeshBuilder.CreateCylinder(new Point3D(0, 0.15, 0), 0.1, 0.3, 20);

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.SaddleBrown));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };

        // Position in this scene
        model.Translate(position.X, position.Y, position.Z);

        return model;
    }
}