using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Game.DarkAlliance.ViewModel
{
    public static class MeshBuilder
    {
        public static MeshGeometry3D CreateQuad(
            Point3D p0,
            Point3D p1,
            Point3D p2,
            Point3D p3)
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

        /// <summary>
        /// Creates a UV sphere with correct outward-facing normals.
        /// </summary>
        public static MeshGeometry3D CreateSphere(
            Point3D center,
            double radius,
            int longitudeDivisions,
            int latitudeDivisions)
        {
            var mesh = new MeshGeometry3D();

            // Vertices + normals
            for (var lat = 0; lat <= latitudeDivisions; lat++)
            {
                var v = (double)lat / latitudeDivisions;
                var phi = Math.PI * v;

                var y = Math.Cos(phi);
                var r = Math.Sin(phi);

                for (var lon = 0; lon <= longitudeDivisions; lon++)
                {
                    var u = (double)lon / longitudeDivisions;
                    var theta = 2.0 * Math.PI * u;

                    var x = r * Math.Cos(theta);
                    var z = r * Math.Sin(theta);

                    // Normal is unit vector from center
                    var normal = new Vector3D(x, y, z);
                    normal.Normalize();

                    // Position is normal * radius + center
                    mesh.Positions.Add(new Point3D(
                        center.X + radius * normal.X,
                        center.Y + radius * normal.Y,
                        center.Z + radius * normal.Z));

                    mesh.Normals.Add(normal);
                }
            }

            var stride = longitudeDivisions + 1;

            // Indices (counter-clockwise winding, outward-facing)
            for (var lat = 0; lat < latitudeDivisions; lat++)
            {
                for (var lon = 0; lon < longitudeDivisions; lon++)
                {
                    var p0 = lat * stride + lon;
                    var p1 = p0 + 1;
                    var p2 = p0 + stride;
                    var p3 = p2 + 1;

                    // Triangle 1
                    mesh.TriangleIndices.Add(p0);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p1);

                    // Triangle 2
                    mesh.TriangleIndices.Add(p1);
                    mesh.TriangleIndices.Add(p2);
                    mesh.TriangleIndices.Add(p3);
                }
            }

            return mesh;
        }
    }
}
