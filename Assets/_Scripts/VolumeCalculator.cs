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
    //Debug.Log($"{_mesh.name.Split(' ')[0]} volume: by mesh = {VolumeOfMesh(_mesh)}");
    Debug.Log($"Unscaled = {VolumeOfMesh(_mesh)}    Scaled = {VolumeOfMeshScaled(_mesh)}");

  private float VolumeOfMesh(Mesh mesh)
  {
    var volume = 0f;
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;

    for (var i = 0; i < mesh.triangles.Length; i += 3)
      volume += TripleProduct(vertices[triangles[i + 0]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);

    var lossyScale = transform.lossyScale;
    return Mathf.Abs(volume) / 6f * lossyScale.x * lossyScale.y * lossyScale.z;
  }

  private void ShowTriangleInConsole(Vector3 verticeX, Vector3 verticeY, Vector3 verticeZ)
  {
    Debug.Log($"{verticeX} + {verticeY}+ {verticeZ}");
    if (verticeX == verticeY || verticeX == verticeZ || verticeY == verticeZ)
      Debug.LogError("repeated vertices in triangle");
  }

  /// TODO: fix - Gives incorrect volume for Plane, if it translated in Y-axis (green) form origin
  private float VolumeOfMeshScaled(Mesh mesh)
  {
    float volume = 0;
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;

//    var _centerOfMass = Vector3.zero;    
//    for (var i = 0; i < mesh.triangles.Length; i++) 
//      _centerOfMass += vertices[triangles[i]];
//    _centerOfMass = _centerOfMass / mesh.triangles.Length;

    for (var i = 0; i < mesh.triangles.Length; i += 3)
    {
      var p1 = transform.TransformPoint(vertices[triangles[i + 0]]);
      var p2 = transform.TransformPoint(vertices[triangles[i + 1]]);
      var p3 = transform.TransformPoint(vertices[triangles[i + 2]]);

      //volume += TripleProduct(p1 - _centerOfMass, p2 - _centerOfMass, p3 - _centerOfMass);
      volume += TripleProduct(p1, p2, p3);
    }

    return Mathf.Abs(volume) / 6f;
  }

  #region Volume of Mesh by triangles   

  public float VolumeOfMeshByTriangles(List<Triangle> triangles)
  {
    var volume = 0f;

    for (var i = 0; i < triangles.Count; i++)
    {
      volume += TripleProduct(triangles[i].v1, triangles[i].v2, triangles[i].v3);
      Debug.Log(
        $"{transform.TransformPoint(triangles[i].v1)} + {transform.TransformPoint(triangles[i].v2)}+ {transform.TransformPoint(triangles[i].v3)}");
      if (triangles[i].v1 == triangles[i].v2 ||
          triangles[i].v1 == triangles[i].v3 ||
          triangles[i].v2 == triangles[i].v3)
        Debug.LogError("repeated vertices in triangle");
    }

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

  private static float TripleProduct(Vector3 v1, Vector3 v2, Vector3 v3)
    => Vector3.Dot(Vector3.Cross(v1, v2), v3);
}