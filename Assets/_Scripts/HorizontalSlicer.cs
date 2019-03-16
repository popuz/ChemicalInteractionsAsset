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

  private List<Vector3> _intersections = new List<Vector3>();
  private List<Vector3> _points = new List<Vector3>();

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
    _intersections.Clear();

    var newTris1 = new List<Triangle>(); // list that stores the triangles for first slice
    var newTris2 = new List<Triangle>(); // second slice

    for (var i = 0; i < _triangles.Length; i += 3) // loop all triangles in the mesh
    {
      _points.Clear();

      var v1 = _triangles[i];
      var v2 = _triangles[i + 1];
      var v3 = _triangles[i + 2];
      var p1 = _transform.TransformPoint(_meshVertices[v1]); // vertex 1 in world coordinates
      var p2 = _transform.TransformPoint(_meshVertices[v2]);
      var p3 = _transform.TransformPoint(_meshVertices[v3]);

      var norm = Vector3.Cross(p1 - p2, p1 - p3); // normal of the triangle
      Debug.DrawLine(p1, p1 + norm, Color.green); // draw normals

      Debug.DrawRay(p1, p2 - p1);
      Debug.DrawRay(p2, p3 - p2);
      Debug.DrawRay(p3, p1 - p3);

      CheckIntersectionWitPlane(p1, p2 - p1);
      CheckIntersectionWitPlane(p2, p3 - p2);
      CheckIntersectionWitPlane(p3, p1 - p3);

      if (_points.Count == 2)
        SubdivideAndAdd3Triangles(p1, p2, p3, norm, newTris1);
      else if (_points.Count <= 0 && !_slicerPlane.GetSide(p1))
        newTris1.Add(new Triangle() {v1 = p1, v2 = p2, v3 = p3});
    }

    foreach (var point in _intersections)
      Debug.DrawLine(point, point + Vector3.up * 0.05f, Color.cyan);

//
//      if (intersections.Count > 1)
//        GenerateNewGeometry(intersections, pl, newTris1, newTris2);
//
//      if (intersections.Count > 0 && Input.GetMouseButtonDown(0))
//        SliceMesh(hit, newTris1, newTris2);
//
  }

  private void CheckIntersectionWitPlane(Vector3 origin, Vector3 direction)
  {
    if (_slicerPlane.Raycast(new Ray(origin, direction), out var ent) && ent <= direction.magnitude)
    {
      var intersection = origin + ent * direction.normalized;
      _intersections.Add(intersection);
      _points.Add(intersection);
    }
  }

  private void SubdivideAndAdd3Triangles(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 norm, List<Triangle> newTris1)
  {
    Debug.Assert(_points.Count == 2);

    var points1 = new List<Vector3>(); // vertices for first slice
    points1.AddRange(_points); // add the intersection vertices (they are shared for both slices)    

    if (!_slicerPlane.GetSide(p1))
      points1.Add(p1);
    if (!_slicerPlane.GetSide(p2))
      points1.Add(p2);
    if (!_slicerPlane.GetSide(p3))
      points1.Add(p3);

    // this is just dumb, could be cleaned up a lot
    // if we slice a triangle, it will end up one half with 3 vertices and second half with 4 vertices
    // 3 means we just create the triangle and flip it the right way if needed
    // 4 means we need to divide it into 2 triangles
    if (points1.Count == 3) // first slice
    {
      var tri = new Triangle() {v1 = points1[1], v2 = points1[0], v3 = points1[2]};
      tri.MatchToDirection(norm);
      newTris1.Add(tri);
    }
    else
    {
      Debug.Assert(points1.Count == 4); // TODO: handle special case
      if (Vector3.Dot((points1[0] - points1[1]), points1[2] - points1[3]) >= 0)
      {
        var tri = new Triangle() {v1 = points1[0], v2 = points1[2], v3 = points1[3]};
        tri.MatchToDirection(
          norm); // flip the triangle if it happens to be the wrong way compared to the original normal
        newTris1.Add(tri);
        tri = new Triangle() {v1 = points1[0], v2 = points1[3], v3 = points1[1]};
        tri.MatchToDirection(norm);
        newTris1.Add(tri);
      }
      else
      {
        var tri = new Triangle() {v1 = points1[0], v2 = points1[3], v3 = points1[2]};
        tri.MatchToDirection(norm);
        newTris1.Add(tri);
        tri = new Triangle() {v1 = points1[0], v2 = points1[2], v3 = points1[1]};
        tri.MatchToDirection(norm);
        newTris1.Add(tri);
      }
    }
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
      tri.MatchToDirection(-pl.normal);
      newTris1.Add(tri);
    }

    for (int i = 0; i < intersections.Count; i++)
    {
      var tri = new Triangle()
      {
        v1 = intersections[i], v2 = center,
        v3 = i + 1 == intersections.Count ? intersections[i] : intersections[i + 1]
      };
      tri.MatchToDirection(pl.normal);
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