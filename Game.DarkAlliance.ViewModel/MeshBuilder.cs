using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel
{
    public class MeshBuilder
    {
        private readonly List<Point3D> _positions = new();
        private readonly List<Vector3D> _normals = new();
        private readonly List<Point> _uv = new();
        private readonly List<int> _indices = new();

        public void AddTriangle(Point3D p0, Point3D p1, Point3D p2)
        {
            int i0 = AddVertex(p0);
            int i1 = AddVertex(p1);
            int i2 = AddVertex(p2);

            _indices.Add(i0);
            _indices.Add(i1);
            _indices.Add(i2);
        }

        public void AddQuad(Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            // First triangle
            AddTriangle(p0, p1, p2);
            // Second triangle
            AddTriangle(p0, p2, p3);
        }

        public void AddSphere(Point3D center, double radius, int slices = 20, int stacks = 10)
        {
            for (int i = 0; i <= stacks; i++)
            {
                double phi = Math.PI * i / stacks; // from 0 to pi

                for (int j = 0; j <= slices; j++)
                {
                    double theta = 2 * Math.PI * j / slices; // from 0 to 2pi

                    double x = radius * Math.Sin(phi) * Math.Cos(theta);
                    double y = radius * Math.Cos(phi);
                    double z = radius * Math.Sin(phi) * Math.Sin(theta);

                    AddVertex(center + new Vector3D(x, y, z));
                }
            }

            int vertsPerRow = slices + 1;

            for (int i = 0; i < stacks; i++)
            {
                for (int j = 0; j < slices; j++)
                {
                    int p0 = i * vertsPerRow + j;
                    int p1 = p0 + 1;
                    int p2 = p0 + vertsPerRow;
                    int p3 = p2 + 1;

                    // Two triangles per quad
                    AddIndexQuad(p0, p1, p2, p3);
                }
            }
        }

        private void AddIndexQuad(int p0, int p1, int p2, int p3)
        {
            _indices.Add(p0);
            _indices.Add(p2);
            _indices.Add(p1);

            _indices.Add(p1);
            _indices.Add(p2);
            _indices.Add(p3);
        }

        private int AddVertex(Point3D p)
        {
            int index = _positions.Count;
            _positions.Add(p);

            // Normals default to vector from origin → good for sphere; OK for flat meshes  
            Vector3D normal = (Vector3D)p;
            if (normal.LengthSquared > 0)
                normal.Normalize();
            _normals.Add(normal);

            _uv.Add(new Point(0, 0)); // can be improved if needed

            return index;
        }

        public MeshGeometry3D ToMesh()
        {
            return new MeshGeometry3D
            {
                Positions = new Point3DCollection(_positions),
                Normals = new Vector3DCollection(_normals),
                TriangleIndices = new Int32Collection(_indices),
                TextureCoordinates = new PointCollection(_uv)
            };
        }
    }
}
