using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel
{
    public static class MeshBuilder
    {
        public static MeshGeometry3D CreateQuad(Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            // Compute single normal
            var normal = Vector3D.CrossProduct(p1 - p0, p2 - p0);
            normal.Normalize();

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);

            return mesh;
        }
    }
}
