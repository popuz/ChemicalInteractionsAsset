using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Serialization;

//[ExecuteInEditMode]
public class HorizontalSlicer : MonoBehaviour
{
  Coroutine abs;
  private const float EPS = 1E-6f;
  [SerializeField] private float _slicerShift;
  [SerializeField] private float _slicerPlaneDebugSize = 1f;
  private Plane _slicerPlane;

  private Transform _transform;
  private Mesh _mesh;
  private Vector3[] _meshVertices;
  private int[] _triangles;

  private List<Vector3> _allIntersections = new List<Vector3>();
  private List<Vector3> _triangleIntersections = new List<Vector3>();
  private List<Triangle> _newTris = new List<Triangle>(); // list that stores the triangles for first slice   

  private VolumeCalculator _volumeCalculator;

  public float SlicerShift
  {
    set => _slicerShift = value;
  }

  public List<Triangle> SlicedTris => _newTris;

  private void OnValidate()
  {
    _transform = gameObject.transform;
    _mesh = GetComponent<MeshFilter>().sharedMesh;
    _triangles = _mesh.triangles;
    _meshVertices = _mesh.vertices;

    _volumeCalculator = GetComponent<VolumeCalculator>();
  }

  private void OnDrawGizmosSelected() =>
    Gizmos.DrawCube(transform.position + Vector3.up * _slicerShift,
      new Vector3(_slicerPlaneDebugSize, 0.0001f, _slicerPlaneDebugSize));

  private void Update()
  {
    MakeSlice();

    foreach (var point in _allIntersections)
      Debug.DrawLine(point, point + Vector3.up * 0.05f, Color.cyan);

    TriangulateSlicedSide();
    //Debug.Log(_volumeCalculator.VolumeOfMeshByTriangles(_newTris));
  }

  public void MakeSlice()
  {
    // TODO: Garbage collected! Improve! Use Translate func of the Plane class or just simple Y position
    // TODO: Even more - write own simplified Plane class with always normal == Vector3.up 
    _slicerPlane = new Plane(Vector3.down, transform.position + Vector3.up * _slicerShift);

    _newTris.Clear();
    _allIntersections.Clear();

    for (var i = 0; i < _triangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      var vertDistToPlane = new float[3];
      for (var j = 0; j < 3; j++)
      {
        vertices[j] = _transform.TransformPoint(_meshVertices[_triangles[i + j]]);
        vertDistToPlane[j] = vertices[j].y - _slicerPlane.distance;
      }

      var norm = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);
      // 2 or 3 points are on the Plane or all of them are below the Plane
      if (vertDistToPlane[0] <= EPS && vertDistToPlane[1] <= EPS && vertDistToPlane[2] <= EPS)
      {
        if (!(Math.Abs(vertDistToPlane[0]) <= EPS && Math.Abs(vertDistToPlane[1]) <= EPS &&
              Math.Abs(vertDistToPlane[2]) <= EPS))
          _newTris.Add(new Triangle(vertices));                

        if (Math.Abs(vertDistToPlane[0]) <= EPS && Math.Abs(vertDistToPlane[1]) <= EPS &&
            Math.Abs(vertDistToPlane[2]) > EPS)
        {
          _allIntersections.Add(vertices[0]);
          _allIntersections.Add(vertices[1]);
        }
        else if (Math.Abs(vertDistToPlane[1]) <= EPS && Math.Abs(vertDistToPlane[2]) <= EPS &&
                 Math.Abs(vertDistToPlane[0]) > EPS)
        {
          _allIntersections.Add(vertices[1]);
          _allIntersections.Add(vertices[2]);
        }
        else if (Math.Abs(vertDistToPlane[2]) <= EPS && Math.Abs(vertDistToPlane[0]) <= EPS &&
                 Math.Abs(vertDistToPlane[1]) > EPS)
        {
          _allIntersections.Add(vertices[2]);
          _allIntersections.Add(vertices[0]);
        }
      }
      // 1 point is one the Plane and other are in different sides of the Plane
      else if (Math.Abs(vertDistToPlane[0] * vertDistToPlane[1] * vertDistToPlane[2]) <= EPS)
      {
        // Make sure that vertices is on opposite sides and not on the Plane
        if (Math.Abs(vertDistToPlane[0]) <= EPS && vertDistToPlane[1] * vertDistToPlane[2] > -EPS) continue;
        if (Math.Abs(vertDistToPlane[1]) <= EPS && vertDistToPlane[0] * vertDistToPlane[2] > -EPS) continue;
        if (Math.Abs(vertDistToPlane[2]) <= EPS && vertDistToPlane[1] * vertDistToPlane[0] > -EPS) continue;

        int i0 = 0, i1 = 1, i2 = 2;

        if (Math.Abs(vertDistToPlane[2]) <= EPS)
        {
          i0 = 2;
          i1 = vertDistToPlane[0] > 0 ? 0 : 1;
          i2 = vertDistToPlane[0] > 0 ? 1 : 0;
        }
        else if (Math.Abs(vertDistToPlane[1]) <= EPS)
        {
          i0 = 1;
          i1 = vertDistToPlane[0] > 0 ? 0 : 2;
          i2 = vertDistToPlane[0] > 0 ? 2 : 0;
        }
        else if (Math.Abs(vertDistToPlane[0]) <= EPS)
        {
          i1 = vertDistToPlane[1] > 0 ? 1 : 2;
          i2 = vertDistToPlane[1] > 0 ? 2 : 1;
        }

        _slicerPlane.Raycast(new Ray(vertices[i1], vertices[i2] - vertices[i1]), out var distOnRay);
        var cutVertex = (vertices[i1] + distOnRay * (vertices[i2] - vertices[i1]).normalized);
        _newTris.Add(new Triangle(vertices[i0], cutVertex, vertices[i2], norm));

        _allIntersections.Add(vertices[i0]);
        _allIntersections.Add(cutVertex);
      }
//      else if (vertDistToPlane[0] * vertDistToPlane[1] < 0)
//      {
//        _slicerPlane.Raycast(new Ray(vertices[0], vertices[1] - vertices[0]), out var distOnRay);
//        var cutVertex = (vertices[0] + distOnRay * (vertices[1] - vertices[0]).normalized);
//        _newTris.Add(new Triangle(vertices[0], cutVertex, vertices[2], norm));
//        _allIntersections.Add(cutVertex);
//      }
//      else if (vertDistToPlane[1] * vertDistToPlane[2] < 0)
//      {
//        _slicerPlane.Raycast(new Ray(vertices[1], vertices[2] - vertices[1]), out var distOnRay);
//        var cutVertex = (vertices[1] + distOnRay * (vertices[2] - vertices[1]).normalized);
//        _newTris.Add(new Triangle(vertices[0], cutVertex, vertices[1], norm));
//        _allIntersections.Add(cutVertex);
//      }
//      else if (vertDistToPlane[2] * vertDistToPlane[0] < 0)
//      {
//        _slicerPlane.Raycast(new Ray(vertices[2], vertices[0] - vertices[2]), out var distOnRay);
//        var cutVertex = (vertices[2] + distOnRay * (vertices[0] - vertices[2]).normalized);
//        _newTris.Add(new Triangle(vertices[1], cutVertex, vertices[2], norm));
//        _allIntersections.Add(cutVertex);
//      }


//      else if (_triangleIntersections.Count == 2)
//      {
//        Debug.Log($"Triangle Intersections = {_triangleIntersections.Count}");
//        _newTris.AddRange(ConstructTrianglesBelowCut(pointsBelowCut: FindPointsBelowCut(vertices),
//          norm: Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2])));
//        _allIntersections.AddRange(_triangleIntersections);
//      }
    }
  }

  private List<Vector3> FindIntersections(Vector3[] points)
  {
    var intersections = new List<Vector3>();

    for (var i = 0; i < 3; i++)
    {
      var j = (i + 1) < 3 ? (i + 1) : 0;
      var origin = points[i];
      var direction = points[j] - points[i];

      Debug.DrawRay(origin, direction);

      if (IntersectedByPlane(origin, direction, out var distOnRay))
        intersections.Add(origin + distOnRay * direction.normalized);
    }

    return intersections;
  }

  private bool IntersectedByPlane(Vector3 origin, Vector3 direction, out float distOnRay)
    => _slicerPlane.Raycast(new Ray(origin, direction), out distOnRay) && distOnRay <= direction.magnitude;

  private List<Vector3> FindPointsBelowCut(Vector3[] triangleVertices)
  {
    var points = new List<Vector3>();

    for (var i = 0; i < 3; i++)
      if (!_slicerPlane.GetSide(triangleVertices[i]))
        points.Add(triangleVertices[i]);

    return points;
  }

  private List<Triangle> ConstructTrianglesBelowCut(List<Vector3> pointsBelowCut, Vector3 norm)
  {
    return pointsBelowCut.Count == 2
      ? new List<Triangle>
      {
        new Triangle(_triangleIntersections[0], _triangleIntersections[1], pointsBelowCut[0], norm),
        new Triangle(pointsBelowCut[0], pointsBelowCut[1], _triangleIntersections[1], norm)
      }
      : new List<Triangle>
        {new Triangle(_triangleIntersections[0], _triangleIntersections[1], pointsBelowCut[0], norm)};
  }

  public void TriangulateSlicedSide()
  {
    var center = Vector3.zero;
    foreach (var vec in _allIntersections) // find average point TODO: for more complex shapes this doesn't work
      center += vec;
    center /= _allIntersections.Count;

    for (var i = 0; i < _allIntersections.Count; i += 2)
      _newTris.Add(new Triangle(_allIntersections[i], center, _allIntersections[i + 1], -_slicerPlane.normal));
  }
}