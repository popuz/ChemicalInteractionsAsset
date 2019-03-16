using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
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
  private List<Triangle> newTris = new List<Triangle>(); // list that stores the triangles for first slice   

  private void OnValidate()
  {
    _transform = gameObject.transform;
    _mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
    _triangles = _mesh.triangles;
    _meshVertices = _mesh.vertices;
  }

  private void OnDrawGizmosSelected() =>
    Gizmos.DrawCube(transform.position + Vector3.up * _slicerShift, new Vector3(1.0f, 0.0001f, 1.0f));

  private void Update()
  {
    _slicerPlane = new Plane(Vector3.up, transform.position + Vector3.up * _slicerShift);
    _allIntersections.Clear();
    
    for (var i = 0; i < _triangles.Length; i += 3)
    {
      var vertices = new Vector3[3];
      for (var j = 0; j < 3; j++)
        vertices[j] = _transform.TransformPoint(_meshVertices[_triangles[i + j]]);

      _triangleIntersections = FindIntersections(vertices);
      _allIntersections.AddRange(_triangleIntersections);

      if (_triangleIntersections.Count == 0 && !_slicerPlane.GetSide(vertices[0]))
        newTris.Add(new Triangle(vertices));
      else if (_triangleIntersections.Count == 2)
        AddTrianglesBelowCut(FindPointsBelowTheCut(vertices),
          norm: Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]));
      else
        Debug.Log(_triangleIntersections.Count);
    }

    foreach (var point in _allIntersections)
      Debug.DrawLine(point, point + Vector3.up * 0.05f, Color.cyan);
//      if (intersections.Count > 1)
//        GenerateNewGeometry(intersections, pl, newTris1, newTris2);
//
//      if (intersections.Count > 0 && Input.GetMouseButtonDown(0))
//        SliceMesh(hit, newTris1, newTris2);
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
    newTris.Add(new Triangle(pointsBelowCut[0], _triangleIntersections[0], _triangleIntersections[1], norm));
    if (pointsBelowCut.Count == 2)
      newTris.Add(new Triangle(pointsBelowCut[0], pointsBelowCut[1], _triangleIntersections[1], norm));
  }

  private void GenerateNewGeometry(List<Vector3> intersections, Plane pl, List<Triangle> newTris1,
    List<Triangle> newTris2)
  {
    var center = Vector3.zero;
    foreach (var vec in intersections) // find average point TODO: for more complex shapes this doesn't work
      center += vec;
    center /= intersections.Count;
    for (int i = 0; i < intersections.Count; i++)
    {
      var tri = new Triangle()
      {
        v1 = intersections[i], v2 = center,
        v3 = i + 1 == intersections.Count ? intersections[i] : intersections[i + 1]
      };
      tri.AlignToDirection(-pl.normal);
      newTris1.Add(tri);
    }

    for (int i = 0; i < intersections.Count; i++)
    {
      var tri = new Triangle()
      {
        v1 = intersections[i], v2 = center,
        v3 = i + 1 == intersections.Count ? intersections[i] : intersections[i + 1]
      };
      tri.AlignToDirection(pl.normal);
      newTris2.Add(tri);
    }
  }

  private void SliceMesh(RaycastHit hit, List<Triangle> newTris1, List<Triangle> newTris2)
  {
    // intersects and player pressed button, in a realistic use case, you'd like to nest all the above code in this block
    var mat = hit.collider.gameObject.GetComponent<MeshRenderer>().material; // get the original material
    Destroy(hit.collider.gameObject);

    Mesh mesh1 = new Mesh();
    Mesh mesh2 = new Mesh();

    List<Vector3> tris = new List<Vector3>();
    List<int> indices = new List<int>();

    int index = 0;
    foreach (var thing in newTris1) // generate first slice
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

    index = 0;
    tris.Clear();
    indices.Clear();
    foreach (var thing in newTris2) // and second
    {
      tris.Add(thing.v1);
      tris.Add(thing.v2);
      tris.Add(thing.v3);
      indices.Add(index++);
      indices.Add(index++);
      indices.Add(index++);
    }

    mesh2.vertices = tris.ToArray();
    mesh2.triangles = indices.ToArray();

    mesh1.RecalculateNormals(); // TODO: most likely you'd want to rebase the pivot, I just didn't care
    mesh1.RecalculateBounds();
    mesh2.RecalculateNormals();
    mesh2.RecalculateBounds();

    // create the actual slice gameobjects

    var go1 = new GameObject();
    var go2 = new GameObject();

    var mf1 = go1.AddComponent<MeshFilter>();
    mf1.mesh = mesh1;
    var mr1 = go1.AddComponent<MeshRenderer>();
    mr1.material = mat;
    var mc1 = go1.AddComponent<MeshCollider>();
    mc1.convex = true;
    go1.AddComponent<Rigidbody>(); // TODO: this will fail if the mesh has more than 255 verts (I think the only solution is to simplify the collision mesh, but... that's complicated)
    mc1.sharedMesh = mesh1;

    var mf2 = go2.AddComponent<MeshFilter>();
    mf2.mesh = mesh2;
    var mr2 = go2.AddComponent<MeshRenderer>();
    mr2.material = mat;
    var mc2 = go2.AddComponent<MeshCollider>();
    mc2.convex = true;
    go2.AddComponent<Rigidbody>();
    mc2.sharedMesh = mesh2;
  }
}