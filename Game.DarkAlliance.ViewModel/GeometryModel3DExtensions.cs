using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel;

public static class GeometryModel3DExtensions
{
    public static GeometryModel3D Translate(
        this GeometryModel3D model,
        double x,
        double y,
        double z)
    {
        model
            .EnsureTransformGroup()
            .Children
            .Add(new TranslateTransform3D(x, y, z));

        return model;
    }

    public static GeometryModel3D Scale(
        this GeometryModel3D model,
        double x,
        double y,
        double z)
    {
        model
            .EnsureTransformGroup()
            .Children
            .Add(new ScaleTransform3D(x, y, z));

        return model;
    }

    public static GeometryModel3D Rotate(
        this GeometryModel3D model,
        Vector3D axis,
        double angleDegrees)
    {
        model
            .EnsureTransformGroup()
            .Children
            .Add(new RotateTransform3D(
                new AxisAngleRotation3D(axis, angleDegrees)));

        return model;
    }

    private static Transform3DGroup EnsureTransformGroup(
        this GeometryModel3D model)
    {
        if (model.Transform is Transform3DGroup group)
            return group;

        group = new Transform3DGroup();

        if (model.Transform != null && model.Transform != Transform3D.Identity)
            group.Children.Add(model.Transform);

        model.Transform = group;
        return group;
    }
}

