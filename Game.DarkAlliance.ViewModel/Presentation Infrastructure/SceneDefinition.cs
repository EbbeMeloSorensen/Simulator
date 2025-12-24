using Craft.Math;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure
{
    public class SceneDefinition
    {
        private List<ScenePart> _sceneParts;
        private List<List<Vector2D>> _boundaries;

        public IReadOnlyList<ScenePart> SceneParts => _sceneParts;
        public IReadOnlyList<IReadOnlyList<Vector2D>> Boundaries => _boundaries;

        public SceneDefinition()
        {
            // For a start, just hardcode a scene. Later, we will read this from some data source.

            _sceneParts = [];
            _boundaries = [];

            AddHumanMale(new Vector3D(0.15, 0, 0), 90);

            AddHumanFemale(new Vector3D(-0.15, 0, 0));

            AddBarrel(new Vector3D(-0.5, 0, -0.5));
            AddBarrel(new Vector3D(0.5, 0, -0.5));
            AddBarrel(new Vector3D(0, 0, 0.5));

            AddBall(new Vector3D(0.5, 0.3, -0.5));
        }

        public void AddHumanMale(
            Vector3D position,
            double orientation = 0)
        {
            _sceneParts.Add(new RotatableScenePart("human male")
            {
                Position = position,
                Orientation = orientation
            });

            var personRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.Z, position.X),
                personRadius);
        }

        public void AddHumanFemale(
            Vector3D position,
            double orientation = 0)
        {
            _sceneParts.Add(new RotatableScenePart("human female")
            {
                Position = position,
                Orientation = orientation
            });


            var personRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.Z, position.X),
                personRadius);
        }

        public void AddBarrel(
            Vector3D position)
        {
            _sceneParts.Add(new ScenePart("barrel")
            {
                Position = position
            });

            var barrelRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.Z, position.X),
                barrelRadius);
        }

        public void AddBall(
            Vector3D position)
        {
            _sceneParts.Add(new ScenePart("ball")
            {
                Position = position
            });
        }

        private void AddCircularBoundary(
            Vector2D center,
            double radius)
        {
            var nBoundarySegments = 8;

            var temp = Enumerable.Range(0, nBoundarySegments + 1)
                .Select(_ => _ * 2 * Math.PI / nBoundarySegments)
                .Select(angle => new Vector2D(
                    center.X + radius * Math.Sin(angle),
                    -center.Y + radius * Math.Cos(angle)))
                .ToList();

            _boundaries.Add(temp);
        }
    }
}
