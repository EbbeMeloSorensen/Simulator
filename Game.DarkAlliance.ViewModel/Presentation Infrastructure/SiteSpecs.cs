using Craft.Math;
using Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents;
using Barrier = Game.DarkAlliance.ViewModel.Presentation_Infrastructure.SiteComponents.Barrier;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace Game.DarkAlliance.ViewModel.Presentation_Infrastructure
{
    public class SiteSpecs
    {
        private List<SiteComponent> _siteComponents;

        public IReadOnlyList<SiteComponent> SiteComponents => _siteComponents;

        public SiteSpecs()
        {
            // Notice that these coordinates are in "map" coordinates where Z is up
            // In order to generate the 3D scene, these coordinates are transformed into 3D coordinates where Y is up (by convention)
            // The animation engine uses a coordinate system where Y points downwards

            _siteComponents = new List<SiteComponent>();

            var siteExtent = 20.0;

            AddQuad(
                new Point3D(siteExtent, siteExtent, 0),
                new Point3D(-siteExtent, siteExtent, 0),
                new Point3D(-siteExtent, -siteExtent, 0),
                new Point3D(siteExtent, -siteExtent, 0));

            AddQuad(
                new Point3D(siteExtent, siteExtent, 1),
                new Point3D(siteExtent, -siteExtent, 1),
                new Point3D(-siteExtent, -siteExtent, 1),
                new Point3D(-siteExtent, siteExtent, 1));

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

            AddHumanMale("Adam", new Point2D(0, 0.2), 90);

            AddHumanFemale("Eve", new Point2D(0, -0.2));

            AddBarrel(new Point2D(-0.5, -0.5));
            AddBarrel(new Point2D(-0.5, 0.5));

            AddBall(new Point2D(-0.5, 0.5), 0.4);
        }

        public void AddHumanMale(
            string tag,
            Point2D position,
            double orientation = 0,
            double height = 0)
        {
            _siteComponents.Add(new NPC("human male")
            {
                Tag = tag,
                Position = new Vector3D(position.Y, height, position.X),
                Orientation = orientation
            });
        }

        public void AddHumanFemale(
            string tag,
            Point2D position,
            double orientation = 0,
            double height = 0)
        {
            _siteComponents.Add(new NPC("human female")
            {
                Tag = tag,
                Position = new Vector3D(position.Y, height, position.X),
                Orientation = orientation
            });
        }

        public void AddBarrel(
            Point2D position,
            double height = 0)
        {
            _siteComponents.Add(new Barrel("barrel")
            {
                Position = new Vector3D(position.Y, height, position.X)
            });
        }

        public void AddBall(
            Point2D position,
            double height = 0)
        {
            _siteComponents.Add(new SiteComponentPlaceable("ball")
            {
                Position = new Vector3D(position.Y, height, position.X)
            });
        }

        public void AddWall(
            IEnumerable<Point2D> wallPoints)
        {
            _siteComponents.Add(new Barrier("wall")
            {
                BarrierPoints = wallPoints.Select(_ => new Vector3D(_.Y, 0, _.X)).ToList()
            });
        }

        public void AddQuad(
            Point3D point1,
            Point3D point2,
            Point3D point3,
            Point3D point4)
        {
            var pt1 = new Vector3D(point1.Y, point1.Z, point1.X);

            _siteComponents.Add(new Quad("quad")
            {
                Point1 = new Vector3D(point1.Y, point1.Z, point1.X),
                Point2 = new Vector3D(point2.Y, point2.Z, point2.X),
                Point3 = new Vector3D(point3.Y, point3.Z, point3.X),
                Point4 = new Vector3D(point4.Y, point4.Z, point4.X)
            });
        }
    }
}
