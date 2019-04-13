using System;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSlicer
{
  private const float EPS = 1E-6f;
  private readonly float _slicerShift;

  public VolumeSlicer(float slicerShift) => _slicerShift = slicerShift;

  public List<Triangle> MakeSlice(GameObject objectToSlice)
  {
    if (objectToSlice == null || objectToSlice.GetComponent<MeshFilter>() == null)
      return new List<Triangle>();

    var mesh = objectToSlice.GetComponent<MeshFilter>().sharedMesh;
    var slicedTris = new List<Triangle>();

    var meshTransform = objectToSlice.transform;
    var meshTriangles = mesh.triangles;
    var meshVertices = mesh.vertices;

    for (var i = 0; i < meshTriangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      var vertDistToPlane = new float[3];

      for (var j = 0; j < 3; j++)
      {
        vertices[j] = meshTransform.TransformPoint(meshVertices[meshTriangles[i + j]]);
        vertDistToPlane[j] = vertices[j].y - (meshTransform.position.y + _slicerShift);
      }

      if (vertDistToPlane[0] < -EPS && vertDistToPlane[1] < -EPS && vertDistToPlane[2] < -EPS)
        slicedTris.Add(new Triangle(vertices));

      if (Math.Abs(vertDistToPlane[0]) <= EPS && Math.Abs(vertDistToPlane[1]) <= EPS &&
          Math.Abs(vertDistToPlane[2]) <= EPS)
      {
        slicedTris.Add(new Triangle(vertices));
        continue;
      }

      if (Math.Abs(vertDistToPlane[0]) <= EPS || Math.Abs(vertDistToPlane[1]) <= EPS ||
          Math.Abs(vertDistToPlane[2]) <= EPS)
      {
        if (vertDistToPlane[0] * vertDistToPlane[1] < -EPS) continue;
        if (vertDistToPlane[1] * vertDistToPlane[2] < -EPS) continue;
        if (vertDistToPlane[2] * vertDistToPlane[0] < -EPS) continue;

        if (vertDistToPlane[0] < -EPS || vertDistToPlane[1] < -EPS || vertDistToPlane[2] < -EPS)
          slicedTris.Add(new Triangle(vertices));
      }
    }

    return slicedTris;
  }
}