using System;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSlicer
{
  private const float EPS = 1E-6f;
  private readonly float _slicerShift;

  private Mesh _mesh;
  private Transform _meshTransform;

  private int[] _meshTriangles = new int[0];
  private Vector3[] _meshVertices;

  public VolumeSlicer(float slicerShift) => _slicerShift = slicerShift;

  public void Init(GameObject objectToSlice)
  {
    if (objectToSlice == null || objectToSlice.GetComponent<MeshFilter>() == null ||
        objectToSlice.GetComponent<MeshFilter>().sharedMesh == null) return;

    _mesh = objectToSlice.GetComponent<MeshFilter>().sharedMesh;
    _meshTransform = objectToSlice.transform;
    _meshTriangles = _mesh.triangles;
    _meshVertices = _mesh.vertices;
  }

  public List<Triangle> MakeSlice()
  {
    var slicedTris = new List<Triangle>();

    for (var i = 0; i < _meshTriangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      var vDistToPlane = new float[3];

      for (var j = 0; j < 3; j++)
      {
        vertices[j] = _meshTransform.TransformPoint(_meshVertices[_meshTriangles[i + j]]);
        vDistToPlane[j] = vertices[j].y - (_meshTransform.position.y + _slicerShift);
      }

      if (vDistToPlane[0] < -EPS && vDistToPlane[1] < -EPS && vDistToPlane[2] < -EPS)
      {
        slicedTris.Add(new Triangle(vertices));
      }
      else if (Math.Abs(vDistToPlane[0]) <= EPS && Math.Abs(vDistToPlane[1]) <= EPS &&
               Math.Abs(vDistToPlane[2]) <= EPS)
      {
        slicedTris.Add(new Triangle(vertices));
      }
      else if (Math.Abs(vDistToPlane[0]) <= EPS || Math.Abs(vDistToPlane[1]) <= EPS ||
               Math.Abs(vDistToPlane[2]) <= EPS)
      {
//        if (vertDistToPlane[0] * vertDistToPlane[1] < -EPS) continue;
//        if (vertDistToPlane[1] * vertDistToPlane[2] < -EPS) continue;
//        if (vertDistToPlane[2] * vertDistToPlane[0] < -EPS) continue;

        if (vDistToPlane[0] < -EPS || vDistToPlane[1] < -EPS || vDistToPlane[2] < -EPS)
          slicedTris.Add(new Triangle(vertices));
      }
      else if (vDistToPlane[0] < -EPS || vDistToPlane[1] < -EPS || vDistToPlane[2] < -EPS)
      {
//        if (vertDistToPlane[0] * vertDistToPlane[1] < -EPS || vertDistToPlane[1] * vertDistToPlane[2] < -EPS
//                                                           || vertDistToPlane[2] * vertDistToPlane[0] < -EPS)
        {
          SliceTriangleInMiddle(vDistToPlane, vertices);
          slicedTris.Add(new Triangle(vertices));
        }
      }
    }

    return slicedTris;
  }


  private void SliceTriangleInMiddle(float[] vDistToPlane, Vector3[] vertices)
  {
    var idBelow = vDistToPlane[0] < -EPS ? 0 : vDistToPlane[1] < -EPS ? 1 : 2;
    var idUnder1 = vDistToPlane[0] < -EPS ? 1 : vDistToPlane[1] < -EPS ? 2 : 0;
    var idUnder2 = vDistToPlane[0] < -EPS ? 2 : vDistToPlane[1] < -EPS ? 0 : 1;
        
    if (vDistToPlane[idUnder1] * vDistToPlane[idUnder2] > 0)
    {      
      MoveVerticesToTheCutPlane(vertices[idBelow], ref vertices[idUnder1]);
      MoveVerticesToTheCutPlane(vertices[idBelow], ref vertices[idUnder2]);
    }    
  }

  private void MoveVerticesToTheCutPlane(Vector3 belowPlaneVertex, ref Vector3 underPlaneVertex)
  {
    underPlaneVertex.x =
      (_slicerShift - belowPlaneVertex.y) * (underPlaneVertex.x - belowPlaneVertex.x) /
      (underPlaneVertex.y - belowPlaneVertex.y) +
      belowPlaneVertex.x;
    underPlaneVertex.x =
      (_slicerShift - belowPlaneVertex.y) * (underPlaneVertex.z - belowPlaneVertex.z) /
      (underPlaneVertex.y - belowPlaneVertex.y) +
      belowPlaneVertex.z;
    underPlaneVertex.y = _slicerShift;
  }
}