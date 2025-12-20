using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

public static class StlMeshLoader
{
    public static MeshGeometry3D Load(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var isAscii = IsAsciiStl(fs);

        fs.Seek(0, SeekOrigin.Begin);

        return isAscii
            ? LoadAsciiStl(fs)
            : LoadBinaryStl(fs);
    }

    private static bool IsAsciiStl(FileStream fs)
    {
        // look at the first 80 bytes; ASCII STL usually starts with "solid"
        Span<byte> header = stackalloc byte[80];
        fs.Read(header);
        var head = Encoding.ASCII.GetString(header);
        return head.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase);
    }

    private static MeshGeometry3D LoadAsciiStl(Stream stream)
    {
        var mesh = new MeshGeometry3D();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine()?.Trim();
            if (line?.StartsWith("facet normal") == true)
            {
                var normal = ParseVector(line.Substring(12));

                reader.ReadLine(); // "outer loop"
                var v1 = ParseVector(reader.ReadLine().Trim().Substring(6));
                var v2 = ParseVector(reader.ReadLine().Trim().Substring(6));
                var v3 = ParseVector(reader.ReadLine().Trim().Substring(6));
                reader.ReadLine(); // "endloop"
                reader.ReadLine(); // "endfacet"

                AddTriangle(mesh, v1, v2, v3, normal);
            }
        }

        mesh.Freeze();
        return mesh;
    }

    private static MeshGeometry3D LoadBinaryStl(Stream stream)
    {
        var mesh = new MeshGeometry3D();
        using var reader = new BinaryReader(stream);

        reader.ReadBytes(80); // header
        uint triCount = reader.ReadUInt32();

        for (int i = 0; i < triCount; i++)
        {
            var normal = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var v1 = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var v2 = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var v3 = new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            AddTriangle(mesh, v1, v2, v3, normal);
            reader.ReadUInt16(); // attribute byte count
        }

        mesh.Freeze();
        return mesh;
    }

    private static Vector3D ParseVector(string s)
    {
        var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new Vector3D(
            double.Parse(parts[0]),
            double.Parse(parts[1]),
            double.Parse(parts[2]));
    }

    private static void AddTriangle(MeshGeometry3D mesh, Vector3D v1, Vector3D v2, Vector3D v3, Vector3D normal)
    {
        mesh.Positions.Add((Point3D)v1);
        mesh.Positions.Add((Point3D)v2);
        mesh.Positions.Add((Point3D)v3);

        int idx = mesh.Positions.Count - 3;
        mesh.TriangleIndices.Add(idx);
        mesh.TriangleIndices.Add(idx + 1);
        mesh.TriangleIndices.Add(idx + 2);

        mesh.Normals.Add(normal);
        mesh.Normals.Add(normal);
        mesh.Normals.Add(normal);
    }
}