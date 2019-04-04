using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For testing purposes ONLY
/// </summary>
public class MeshGenerator : MonoBehaviour
{
  private void Update()
  {
    if (Input.GetMouseButtonDown(0))
      GenerateSlicedMesh(FindObjectOfType<HorizontalSlicer>().SlicedTris);
  }

  public void GenerateSlicedMesh(List<Triangle> _newTris)
  {
    var mesh1 = new Mesh();

    var tris = new List<Vector3>();
    var indices = new List<int>();

    var index = 0;
    foreach (var thing in _newTris) // generate slice
    {
      tris.Add(thing.v1);
      tris.Add(thing.v2);
      tris.Add(thing.v3);    
      indices.Add(index++);
      indices.Add(index++);
      indices.Add(index++);
    }

    mesh1.vertices = tris.ToArray();
    mesh1.triangles = indices.ToArray();

    mesh1.RecalculateNormals(); // TODO: most likely you'd want to rebase the pivot, I just didn't care
    mesh1.RecalculateBounds();

    // create the actual slice gameObject
    var go1 = new GameObject("Sliced Mesh");
    var mf1 = go1.AddComponent<MeshFilter>();
    mf1.mesh = mesh1;
    var mr1 = go1.AddComponent<MeshRenderer>();
    mr1.material = new Material(FindObjectOfType<MeshRenderer>().GetComponent<MeshRenderer>().material);
    go1.AddComponent<VolumeCalculator>();
  }
}