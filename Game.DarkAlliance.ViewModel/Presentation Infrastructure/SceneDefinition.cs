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

            _sceneParts = new List<ScenePart>();
            _boundaries = new List<List<Vector2D>>();

            AddWall(new List<Point2D>
            {
                new Point2D(-16, -6),
                new Point2D(-16, -5),
                new Point2D(-14, -4),
                new Point2D(-7, -4),
                new Point2D(-7, -5),
                new Point2D(-2, -5),
                new Point2D(-2, -2),
                new Point2D(2, -2),
                new Point2D(2, 2),
                new Point2D(-2, 2),
                new Point2D(-2, 0),
                new Point2D(-3, 0),
                new Point2D(-3, -4),
                new Point2D(-6, -4),
                new Point2D(-6, -2),
                new Point2D(-14, -2),
                new Point2D(-16, -1),
                new Point2D(-16, 0)
            });

            AddWall(new List<Point2D>
            {
                new Point2D(-17, 0),
                new Point2D(-17, -1),
                new Point2D(-18, -2),
                new Point2D(-18, -3),
                new Point2D(-19, -3)
            });

            AddWall(new List<Point2D>
            {
                new Point2D(-19, -4),
                new Point2D(-18, -4),
                new Point2D(-17, -5),
                new Point2D(-17, -6)
            });

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

            AddPolylineBoundary(wallPoints.Select(_ => new Vector2D(_.X, -_.Y)));
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
            IEnumerable<Vector2D> points)
        {
            _boundaries.Add(points.ToList());
        }
    }
}
