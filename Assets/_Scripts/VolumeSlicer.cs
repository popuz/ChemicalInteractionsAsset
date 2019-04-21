using System;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSlicer
{
  private const float EPS = 1E-6f;
  private float _slicerShift;

  private Mesh _mesh;
  private Transform _meshTransform;

  private int[] _meshTriangles = new int[0];
  private Vector3[] _meshVertices;

  public float SlicerShift
  {
    set => _slicerShift = value;
  }

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

      if (AllVerticesBelowTheSlice(vDistToPlane) || AllVerticesAreOnTheSlice(vDistToPlane))
      {
        slicedTris.Add(new Triangle(vertices));
      }
      else if (AtLeastOneVertexAreOnTheSlice(vDistToPlane) && AtLeastOneVertexBelowTheSlice(vDistToPlane))
      {
        var idBelowSlice = vDistToPlane[1] < -EPS ? 1 : vDistToPlane[2] < -EPS ? 2 : 0;
        var idAboveSlice = vDistToPlane[2] > EPS ? 2 : vDistToPlane[0] > EPS ? 0 : 1;

        if (vDistToPlane[idBelowSlice] * vDistToPlane[idAboveSlice] < 0)
          MoveVertexToTheCutPlane(vertices[idBelowSlice], ref vertices[idAboveSlice]);

        slicedTris.Add(new Triangle(vertices));
      }
      else if (AtLeastOneVertexBelowTheSlice(vDistToPlane))
      {
        if (OnlyOneVertexBelowTheSlice(vDistToPlane))
        {
          SliceTriangleInMiddleWithOnePointBelow(vDistToPlane, vertices);
          slicedTris.Add(new Triangle(vertices));
        }
        else
        {
          SliceTriangleInMiddleWithTwoPointsBelow(vDistToPlane, vertices);
          slicedTris.Add(new Triangle(vertices));
          slicedTris.Add(GenerateNewTriangleWithOneVertexOnSlice(vDistToPlane, vertices));
        }
      }
    }

    return slicedTris;
  }

  private static bool AllVerticesBelowTheSlice(float[] vDistToPlane) =>
    vDistToPlane[0] < -EPS && vDistToPlane[1] < -EPS && vDistToPlane[2] < -EPS;

  private static bool AtLeastOneVertexBelowTheSlice(float[] vDistToPlane) =>
    vDistToPlane[0] < -EPS || vDistToPlane[1] < -EPS || vDistToPlane[2] < -EPS;

  private static bool OnlyOneVertexBelowTheSlice(float[] vDistToPlane) =>
    vDistToPlane[0] < -EPS ^ vDistToPlane[1] < -EPS ^ vDistToPlane[2] < -EPS;

  private static bool AllVerticesAreOnTheSlice(float[] vDistToPlane) =>
    Math.Abs(vDistToPlane[0]) <= EPS && Math.Abs(vDistToPlane[1]) <= EPS && Math.Abs(vDistToPlane[2]) <= EPS;

  private static bool AtLeastOneVertexAreOnTheSlice(float[] vDistToPlane) =>
    Math.Abs(vDistToPlane[0]) <= EPS || Math.Abs(vDistToPlane[1]) <= EPS || Math.Abs(vDistToPlane[2]) <= EPS;

  private void MoveVertexToTheCutPlane(Vector3 belowPlaneVertex, ref Vector3 underPlaneVertex)
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

  private void SliceTriangleInMiddleWithOnePointBelow(float[] vDistToPlane, Vector3[] vertices)
  {
    var idBelow = vDistToPlane[0] < -EPS ? 0 : vDistToPlane[1] < -EPS ? 1 : 2;
    var idUnder1 = vDistToPlane[0] < -EPS ? 1 : vDistToPlane[1] < -EPS ? 2 : 0;
    var idUnder2 = 3 ^ (idBelow ^ idUnder1);

    MoveVertexToTheCutPlane(vertices[idBelow], ref vertices[idUnder1]);
    MoveVertexToTheCutPlane(vertices[idBelow], ref vertices[idUnder2]);
  }

  private void SliceTriangleInMiddleWithTwoPointsBelow(float[] vDistToPlane, Vector3[] vertices)
  {
    var idBelow1 = vDistToPlane[0] < -EPS ? 0 : vDistToPlane[1] < -EPS ? 1 : 2;
    var tempId = vDistToPlane[0] < -EPS ? 1 : vDistToPlane[1] < -EPS ? 2 : 0;
    var idBelow2 = vDistToPlane[idBelow1] * vDistToPlane[tempId] > 0 ? tempId : 3 ^ (idBelow1 ^ tempId);
    var idUnder = 3 ^ (idBelow1 ^ idBelow2);

    MoveVertexToTheCutPlane(vertices[idBelow1], ref vertices[idUnder]);
    MoveVertexToTheCutPlane(vertices[idBelow2], ref vertices[idUnder]);
  }


  private Triangle GenerateNewTriangleWithOneVertexOnSlice(float[] vDistToPlane, Vector3[] vertices)
  {
    var idOnSlice = Math.Abs(vDistToPlane[0]) <= EPS ? 0 : Math.Abs(vDistToPlane[1]) <= EPS ? 1 : 2;
    var id2 = idOnSlice == 0 ? 1 : idOnSlice == 1 ? 2 : 0;
    var id3 = 3 ^ (idOnSlice ^ id2);

    return new Triangle(vertices[idOnSlice], vertices[id2], vertices[id3]);
  }
}