using System.Windows.Media;
using System.Windows.Media.Media3D;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure;

public class SceneRenderer : ISceneRenderer
{
    public Model3D Build(
        SceneDefinition sceneDefinition)
    {
        var group = new Model3DGroup();

        foreach (var scenePart in sceneDefinition.SceneParts)
        {
            var model = scenePart.ModelId switch
            {
                "wall" => GenerateBall(scenePart),
                "barrel" => GenerateBarrel(scenePart),
                "ball" => GenerateBall(scenePart),
                "human male" => GenerateHumanMale(scenePart),
                "human female" => GenerateHumanFemale(scenePart),
                _ => throw new NotSupportedException($"Unknown Model ID '{scenePart.ModelId}'.")
            };

            group.Children.Add(model);
        }

        return group;
    }

    private GeometryModel3D GenerateHumanMale(
        ScenePart scenePart)
    {
        if (scenePart is not RotatableScenePart rotatableScenePart)
        {
            throw new InvalidOperationException("Must be a rotatable scene part");
        }

        return ImportMeshFromFile(
            @"Assets\male.stl",
            new DiffuseMaterial(new SolidColorBrush(Colors.LightPink)),
            new Vector3D(1, 0, 0),
            -90,
            new Vector3D(0, 0, 0),
            0.003,
            rotatableScenePart.Position,
            rotatableScenePart.Orientation);
    }

    private GeometryModel3D GenerateHumanFemale(
        ScenePart scenePart)
    {
        if (scenePart is not RotatableScenePart rotatableScenePart)
        {
            throw new InvalidOperationException("Must be a rotatable scene part");
        }

        return ImportMeshFromFile(
            @"Assets\female.stl",
            new DiffuseMaterial(new SolidColorBrush(Colors.LightPink)),
            new Vector3D(1, 0, 0),
            -90,
            new Vector3D(-132.5, 0, 101),
            0.015,
            rotatableScenePart.Position,
            rotatableScenePart.Orientation);
    }

    private GeometryModel3D GenerateBarrel(
        ScenePart scenePart)
    {
        if (scenePart is not ScenePartPlaceable scenePartPlaceable)
        {
            throw new InvalidOperationException("Must be a rotatable scene part");
        }

        var mesh = MeshBuilder.CreateCylinder(new Point3D(0, 0.2, 0), 0.2, 0.4, 20);

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.SaddleBrown));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };

        // Position in this scene
        model.Translate(
            scenePartPlaceable.Position.X,
            scenePartPlaceable.Position.Y,
            scenePartPlaceable.Position.Z);

        return model;
    }

    private GeometryModel3D GenerateBall(
        ScenePart scenePart)
    {
        if (scenePart is not ScenePartPlaceable scenePartPlaceable)
        {
            throw new InvalidOperationException("Must be a rotatable scene part");
        }

        var radius = 0.1;
        var mesh = MeshBuilder.CreateSphere(new Point3D(0, radius, 0), radius, 10, 10);

        var material = new DiffuseMaterial(new SolidColorBrush(Colors.Orange));

        var model = new GeometryModel3D
        {
            Geometry = mesh,
            Material = material,
            BackMaterial = material
        };

        // Position in this scene
        model.Translate(
            scenePartPlaceable.Position.X,
            scenePartPlaceable.Position.Y,
            scenePartPlaceable.Position.Z);

        return model;
    }

    private GeometryModel3D GenerateWall(
        ScenePart scenePart)
    {
        throw new NotImplementedException();
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
}