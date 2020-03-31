
using System.Collections.Generic;
using UnityEngine;

// Handles the preparation of submeshes
public static class MeshExtension
{
    private class Vertices
    {
        List<Vector3> verts = null;
        List<Vector2> uv1 = null;
        List<Vector2> uv2 = null;
        List<Vector2> uv3 = null;
        List<Vector2> uv4 = null;
        List<Vector3> normals = null;
        List<Vector4> tangents = null;

        public Vertices()
        {
            verts = new List<Vector3>();
        }

        public Vertices(Mesh mesh)
        {
            verts = CreateList(mesh.vertices);
            uv1 = CreateList(mesh.uv);
            uv2 = CreateList(mesh.uv2);
            uv3 = CreateList(mesh.uv3);
            uv4 = CreateList(mesh.uv4);
            normals = CreateList(mesh.normals);
            tangents = CreateList(mesh.tangents);
        }

        private List<T> CreateList<T>(T[] source)
        {
            if (source == null || source.Length == 0)
            {
                return null;
            }
            else

                return new List<T>(source);
        }

        private void Copy<T>(ref List<T> dest, List<T> source, int index)
        {
            if (source == null)
            {
                return;
            }

            if (dest == null)
            {
                return;
            }

            dest.Add(source[index]);
        }

        public int Add(Vertices other, int index)
        {
            int i = verts.Count;
            Copy(ref verts, other.verts, index);
            Copy(ref uv1, other.uv1, index);
            Copy(ref uv2, other.uv2, index);
            Copy(ref uv3, other.uv3, index);
            Copy(ref uv4, other.uv4, index);
            Copy(ref normals, other.normals, index);
            Copy(ref tangents, other.tangents, index);
            return i;
        }

        public void AssignTo(Mesh target)
        {
            if (verts.Count > 65535)
            {
                target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            target.SetVertices(verts);
            if (uv1 != null) target.SetUVs(0, uv1);
            if (uv2 != null) target.SetUVs(1, uv2);
            if (uv3 != null) target.SetUVs(2, uv3);
            if (uv4 != null) target.SetUVs(3, uv4);
            if (normals != null) target.SetNormals(normals);
            if (tangents != null) target.SetTangents(tangents);
        }
    }

    public static Mesh GetSubMesh(this Mesh mesh, int subMeshIndex)
    {
        if (subMeshIndex < 0 || subMeshIndex >= mesh.subMeshCount)
            return null;

        int[] indices = mesh.GetTriangles(subMeshIndex);
        Vertices source = new Vertices(mesh);
        Vertices dest = new Vertices();
        Dictionary<int, int> map = new Dictionary<int, int>();
        int[] newIndices = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            int o = indices[i];
            int n;
            if (!map.TryGetValue(o, out n))
            {
                n = dest.Add(source, o);
                map.Add(o, n);
            }
            newIndices[i] = n;
        }

        Mesh newMesh = new Mesh();
        dest.AssignTo(newMesh);
        newMesh.triangles = newIndices;
        return newMesh;
    }
}
