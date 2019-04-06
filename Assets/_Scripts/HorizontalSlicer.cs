using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

//[ExecuteInEditMode]
public class HorizontalSlicer : MonoBehaviour
{
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
    _slicerPlane = new Plane(Vector3.up, transform.position + Vector3.up * _slicerShift);
    _newTris.Clear();
    _allIntersections.Clear();


    for (var i = 0; i < _triangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      for (var j = 0; j < 3; j++)
        vertices[j] = _transform.TransformPoint(_meshVertices[_triangles[i + j]]);

      _triangleIntersections = FindIntersections(vertices);

      if (_triangleIntersections.Count == 0 && !_slicerPlane.GetSide(vertices[0]))
      {
        _newTris.Add(new Triangle(vertices));
      }
      else if (_triangleIntersections.Count == 3)
      {
        Debug.Log(3);
        var norm = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);
        if (_slicerPlane.normal == norm)
          _newTris.Add(new Triangle(vertices));
      }
      else if (_triangleIntersections.Count == 2)
      {
        Debug.Log(2);
        _newTris.AddRange(ConstructTrianglesBelowCut(pointsBelowCut: FindPointsBelowCut(vertices),
          norm: Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2])));
        _allIntersections.AddRange(_triangleIntersections);
      }
      // TODO: Resolve special cases when count == 1
      else if (_triangleIntersections.Count == 1)
      {
        Debug.Log(1);
        var norm = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);
        if (_slicerPlane.normal == norm)
          _newTris.Add(new Triangle(vertices));

        Debug.Log($"Plane normal: {_slicerPlane.normal} ---- triangle normal:{norm}");
      }
    }

    foreach (var point in _allIntersections)
      Debug.DrawLine(point, point + Vector3.up * 0.05f, Color.cyan);

    TriangulateSlicedSide();

    //Debug.Log(_volumeCalculator.VolumeOfMeshByTriangles(_newTris));
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

      // TODO: manage cases when points are on the slice Plane (cases: 1,2 or 3 points)
      if (Math.Abs(_slicerPlane.GetDistanceToPoint(origin)) < 1E-3f)
        intersections.Add(origin);
      else if (IntersectedByPlane(origin, direction, out var distOnRay))
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

  private void TriangulateSlicedSide()
  {
    var center = Vector3.zero;
    foreach (var vec in _allIntersections) // find average point TODO: for more complex shapes this doesn't work
      center += vec;
    center /= _allIntersections.Count;

    for (var i = 0; i < _allIntersections.Count; i += 2)
      _newTris.Add(new Triangle(_allIntersections[i], center, _allIntersections[i + 1], _slicerPlane.normal));
  }
}