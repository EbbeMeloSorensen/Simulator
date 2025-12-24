using Craft.Math;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts;
using System.Windows.Media.Media3D;
using Craft.Utils.Linq;
using Barrier = Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SceneParts.Barrier;
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

            //AddWall([
            //    new Point2D(-1, 1),
            //    new Point2D(-1, -1)
            //]);

            AddHumanMale(new Point2D(0, 0.15), 90);

            AddHumanFemale(new Point2D(0, -0.15));

            AddBarrel(new Point2D(-0.5, -0.5));
            AddBarrel(new Point2D(-0.5, 0.5));

            AddBall(new Point2D(-0.5, 0.5), 0.4);
        }

        public void AddHumanMale(
            Point2D position,
            double orientation = 0,
            double height = 0)
        {
            _sceneParts.Add(new RotatableScenePart("human male")
            {
                Position = new Vector3D(position.Y, height, position.X),
                Orientation = orientation
            });

            var personRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.X, position.Y),
                personRadius);
        }

        public void AddHumanFemale(
            Point2D position,
            double orientation = 0,
            double height = 0)
        {
            _sceneParts.Add(new RotatableScenePart("human female")
            {
                Position = new Vector3D(position.Y, height, position.X),
                Orientation = orientation
            });

            var personRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.X, position.Y),
                personRadius);
        }

        public void AddBarrel(
            Point2D position,
            double height = 0)
        {
            _sceneParts.Add(new ScenePartPlaceable("barrel")
            {
                Position = new Vector3D(position.Y, height, position.X)
            });

            var barrelRadius = 0.1;

            AddCircularBoundary(
                new Vector2D(position.X, position.Y),
                barrelRadius);
        }

        public void AddBall(
            Point2D position,
            double height = 0)
        {
            _sceneParts.Add(new ScenePartPlaceable("ball")
            {
                Position = new Vector3D(position.Y, height, position.X)
            });
        }

        public void AddWall(
            IEnumerable<Point2D> wallPoints)
        {
            _sceneParts.Add(new Barrier("wall")
            {
                BarrierPoints = wallPoints.Select(_ => new Vector3D(_.Y, 0, _.X)).ToList()
            });

            //AddPolylineBoundary(wallPoints);
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

        private void AddPolylineBoundary(
            IEnumerable<Point2D> points)
        {
            //points.AdjacentPairs((p1, p2) =>
            //{

            //})

        }
    }
}
