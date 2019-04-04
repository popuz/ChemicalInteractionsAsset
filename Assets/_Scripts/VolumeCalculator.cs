using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VolumeCalculator : MonoBehaviour
{
  private Mesh _mesh;

  private void OnValidate() => _mesh = GetComponent<MeshFilter>().sharedMesh;
  private void Start() => PrintRealVolume();

  [ContextMenu("Print Volume")]
  public void PrintRealVolume() =>
    Debug.Log($"{_mesh.name.Split(' ')[0]}: volume = {VolumeOfMesh(_mesh)}");

  private float VolumeOfMesh(Mesh mesh)
  {
    Debug.Log(mesh.triangles.Length);

    var volume = 0f;
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;
    var _centerOfMass = GetCenterOfMass(mesh);

    for (var i = 0; i < mesh.triangles.Length; i += 3)
    {
      var p1 = transform.TransformPoint(vertices[triangles[i + 0]]);
      var p2 = transform.TransformPoint(vertices[triangles[i + 1]]);
      var p3 = transform.TransformPoint(vertices[triangles[i + 2]]);

      if (p1 == p2 || p2 == p3 || p1 == p3)
        Debug.Log(0);

      volume += TripleProduct(p1 - _centerOfMass, p2 - _centerOfMass, p3 - _centerOfMass);
    }

    return Mathf.Abs(volume) / 6f;
  }

  private Vector3 GetCenterOfMass(Mesh mesh) =>
    mesh.triangles.Aggregate(Vector3.zero, (current, t) => current + transform.TransformPoint(mesh.vertices[t])) /
    mesh.triangles.Length;

  private static float TripleProduct(Vector3 v1, Vector3 v2, Vector3 v3)
    => Vector3.Dot(Vector3.Cross(v1, v2), v3);

  #region Volume of Mesh by triangles   

  public float VolumeOfMeshByTriangles(List<Triangle> triangles)
  {
    var volume = 0f;

    for (var i = 0; i < triangles.Count; i++)
      volume += TripleProduct(triangles[i].v1, triangles[i].v2, triangles[i].v3);

    return Mathf.Abs(volume) / 6f;
  }

  public float VolumeOfMeshByTriangles()
  {
    var volume = 0f;
    var _meshVertices = _mesh.vertices;
    var _triangles = _mesh.triangles;

    var _newTris = new List<Triangle>();

    var vertices = new Vector3[3];
    for (var i = 0; i < _triangles.Length; i += 3)
    {
      for (var j = 0; j < 3; j++)
        vertices[j] = transform.TransformPoint(_meshVertices[_triangles[i + j]]);

      _newTris.Add(new Triangle(vertices));
    }

    for (var i = 0; i < _newTris.Count; i++)
      volume += TripleProduct(_newTris[i].v1, _newTris[i].v2, _newTris[i].v3);

    return Mathf.Abs(volume) / 6f;
  }

  #endregion
}