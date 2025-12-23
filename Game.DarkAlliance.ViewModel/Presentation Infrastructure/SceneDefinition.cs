using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure
{
    public class SceneDefinition
    {
        public IReadOnlyList<ScenePart> Parts { get; init; }

        public SceneDefinition()
        {
            // For a start, just hardcode a scene. Later, we will read this from some data source.

            var parts = new List<ScenePart>();

            var transformGroup = new Transform3DGroup();

            transformGroup.Children.Add(new RotateTransform3D
            {
                Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)
            });

            var scaleFactor = 0.003;
            transformGroup.Children.Add(new ScaleTransform3D(scaleFactor, scaleFactor, scaleFactor));

            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.LightPink)));

            parts.Add(new ScenePart(
                @"Assets\low poly guy.stl",
                transformGroup,
                material,
                false));

            Parts = new List<ScenePart>(parts);
        }
    }
}
