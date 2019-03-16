using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//[ExecuteInEditMode]
public class HorizontalSlicer : MonoBehaviour
{
  [SerializeField] private float _slicerShift;
  private Plane _slicerPlane;

  private Transform _transform;
  private Mesh _mesh;
  private Vector3[] _meshVertices;
  private int[] _triangles;

  private List<Vector3> _allIntersections = new List<Vector3>();
  private List<Vector3> _triangleIntersections = new List<Vector3>();
  private List<Triangle> _newTris = new List<Triangle>(); // list that stores the triangles for first slice   

  private VolumeCalculator _volumeCalculator;

  private void OnValidate()
  {
    _transform = gameObject.transform;
    _mesh = GetComponent<MeshFilter>().sharedMesh;
    _triangles = _mesh.triangles;
    _meshVertices = _mesh.vertices;

    _volumeCalculator = GetComponent<VolumeCalculator>();
  }

  private void OnDrawGizmosSelected() =>
    Gizmos.DrawCube(transform.position + Vector3.up * _slicerShift, new Vector3(1.0f, 0.0001f, 1.0f));

  private void Update()
  {
    //if (!Input.GetMouseButtonDown(0)) return;

    _slicerPlane = new Plane(Vector3.up, transform.position + Vector3.up * _slicerShift);
    _newTris.Clear();
    _allIntersections.Clear();

    for (var i = 0; i < _triangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      for (var j = 0; j < 3; j++)
        vertices[j] = _transform.TransformPoint(_meshVertices[_triangles[i + j]]);
      var norm = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]);

      _triangleIntersections = FindIntersections(vertices);
      _allIntersections.AddRange(_triangleIntersections);

      if (_triangleIntersections.Count == 0 && !_slicerPlane.GetSide(vertices[0]))
        _newTris.Add(new Triangle(vertices));
      else if (_triangleIntersections.Count == 2)
        AddTrianglesBelowCut(FindPointsBelowTheCut(vertices), norm);
      else if (_triangleIntersections.Count != 0) // TODO: Resolve special cases when count == 1 and == 3
        Debug.LogWarning($"Un managed intersections count:{_triangleIntersections.Count} intersections");
    }

    foreach (var point in _allIntersections)
      Debug.DrawLine(point, point + Vector3.up * 0.05f, Color.cyan);
       
    //TriangulateSlicedSide();    
    _volumeCalculator.PrintRealVolume();
    Debug.Log(_volumeCalculator.VolumeOfMesh(_newTris));   
    Debug.Log($"started:{_volumeCalculator.VolumeOfMeshByTriangles()}  cut:{_volumeCalculator.VolumeOfMesh(_newTris)}");        
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

  private List<Vector3> FindPointsBelowTheCut(Vector3[] triangleVertices)
  {
    var points = new List<Vector3>();

    for (var i = 0; i < 3; i++)
      if (!_slicerPlane.GetSide(triangleVertices[i]))
        points.Add(triangleVertices[i]);

    return points;
  }

  private void AddTrianglesBelowCut(List<Vector3> pointsBelowCut, Vector3 norm)
  {
    _newTris.Add(new Triangle(_triangleIntersections[0], _triangleIntersections[1], pointsBelowCut[0], norm));
    
    if (pointsBelowCut.Count == 2)    
      _newTris.Add(new Triangle(pointsBelowCut[0], pointsBelowCut[1], _triangleIntersections[1], norm));    
  }

  private void TriangulateSlicedSide()
  {
    var center = Vector3.zero;
    foreach (var vec in _allIntersections) // find average point TODO: for more complex shapes this doesn't work
      center += vec;
    center /= _allIntersections.Count;

    for (var i = 0; i < _allIntersections.Count; i++)
      _newTris.Add(new Triangle
      (_allIntersections[i], center, i + 1 == _allIntersections.Count ? _allIntersections[i] : _allIntersections[i + 1],
        _slicerPlane.normal));
  }

  private void GenerateSlicedMesh()
  {
    var mat = gameObject.GetComponent<MeshRenderer>().material; // get the original material
    //Destroy(gameObject);

    Mesh mesh1 = new Mesh();

    List<Vector3> tris = new List<Vector3>();
    List<int> indices = new List<int>();

    int index = 0;
    foreach (var thing in _newTris) // generate first slice
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

    // create the actual slice gameobjects
    var go1 = new GameObject("Sliced Mesh");
    var mf1 = go1.AddComponent<MeshFilter>();
    mf1.mesh = mesh1;
    var mr1 = go1.AddComponent<MeshRenderer>();
    mr1.material = mat;
    //var mc1 = go1.AddComponent<MeshCollider>();
    //mc1.convex = true;
    //go1.AddComponent<Rigidbody>(); // TODO: this will fail if the mesh has more than 255 verts (I think the only solution is to simplify the collision mesh, but... that's complicated)
    //mc1.sharedMesh = mesh1;
  }
}