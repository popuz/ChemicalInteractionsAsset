using System.Collections.Generic;
using UnityEngine;

public class HorizontalSlicer : MonoBehaviour
{
  private Transform slicer;
  private void Start() => slicer = transform;
  private void Update()
  {
    // find the points the define the slicing plane    
    Vector3 o1 = slicer.position;
    Vector3 o2 = slicer.position + slicer.forward * 2f - slicer.right;
    Vector3 o3 = slicer.position + slicer.forward * 2f + slicer.right;

    // draw a triangle tha defines thes plane\        
    Debug.DrawLine(o1, o2, Color.red);
    Debug.DrawLine(o2, o3, Color.red);
    Debug.DrawLine(o3, o1, Color.red);

    // there is definitely better ways for this (checks if the triangle edges intersect with anything)
    if (Physics.Linecast(o1, o2, out var hit) || Physics.Linecast(o2, o3, out hit) || Physics.Linecast(o3, o1, out hit))
    {
      var cutPlane = new Plane(o1, o2, o3);
      var hitTransform = hit.collider.gameObject.transform;
      var hitMesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
      var hitMeshTriangles = hitMesh.triangles;
      var hitMeshVertices = hitMesh.vertices;

      var edgeIntersections = new List<Vector3>(); 
      var firstSliceTriangles = new List<Triangle>(); 
      var secondSliceTriangles = new List<Triangle>();

      for (var i = 0; i < hitMeshTriangles.Length; i += 3) 
      {
        var points = new List<Vector3>();

        var v1 = hitMeshTriangles[i];
        var v2 = hitMeshTriangles[i + 1];
        var v3 = hitMeshTriangles[i + 2];
        var p1 = hitTransform.TransformPoint(hitMeshVertices[v1]); // vertex 1 in world coordinates
        var p2 = hitTransform.TransformPoint(hitMeshVertices[v2]);
        var p3 = hitTransform.TransformPoint(hitMeshVertices[v3]);
        var norm = Vector3.Cross(p1 - p2, p1 - p3); // normal of the triangle
        Debug.DrawLine(p1, p1 + norm, Color.cyan);

        var dir = p2 - p1;
        float ent;
        Debug.DrawRay(p1, dir);
        if (cutPlane.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude) // check for intersections in the first edge
        {
          var intersection = p1 + ent * dir.normalized;
          edgeIntersections.Add(intersection);
          points.Add(intersection);
        }

        dir = p3 - p2;
        Debug.DrawRay(p2, dir);
        if (cutPlane.Raycast(new Ray(p2, dir), out ent) && ent <= dir.magnitude) // 2nd edge
        {
          var intersection = p2 + ent * dir.normalized;
          edgeIntersections.Add(intersection);
          points.Add(intersection);
        }

        dir = p3 - p1;
        Debug.DrawRay(p1, dir);
        if (cutPlane.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude) // 3rd edge
        {
          var intersection = p1 + ent * dir.normalized;
          edgeIntersections.Add(intersection);
          points.Add(intersection);
        }

        if (points.Count > 0) // an intersection was found, subdivide and add 3 triangles
        {
          if (points.Count != 2) // this should not normally happen, if it does, we pretend it didn't :)
            return; // (this is a special case, ideally you would want to handle this)
          Debug.Assert(points.Count == 2);
          List<Vector3> points1 = new List<Vector3>(); // vertices for first slice
          points1.AddRange(points); // add the intersection vertices (they are shared for both slices)
          List<Vector3> points2 = new List<Vector3>();
          points2.AddRange(points);
          if (cutPlane.GetSide(p1)) points1.Add(p1); // check on which side each of the original vertices was
          else points2.Add(p1);
          if (cutPlane.GetSide(p2)) points1.Add(p2);
          else points2.Add(p2);
          if (cutPlane.GetSide(p3)) points1.Add(p3);
          else points2.Add(p3);

          // this is just dumb, could be cleaned up a lot
          // if we slice a triangle, it will end up one half with 3 vertices and second half with 4 vertices
          // 3 means we just create the triangle and flip it the right way if needed
          // 4 means we need to divide it into 2 triangles
          if (points1.Count == 3) // first slice
          {
            var tri = new Triangle() {v1 = points1[1], v2 = points1[0], v3 = points1[2]};
            tri.MatchToDirection(norm);
            firstSliceTriangles.Add(tri);
          }
          else
          {
            Debug.Assert(points1.Count == 4); // TODO: handle special case
            if (Vector3.Dot((points1[0] - points1[1]), points1[2] - points1[3]) >= 0)
            {
              var tri = new Triangle() {v1 = points1[0], v2 = points1[2], v3 = points1[3]};
              tri.MatchToDirection(
                norm); // flip the triangle if it happens to be the wrong way compared to the original normal
              firstSliceTriangles.Add(tri);
              tri = new Triangle() {v1 = points1[0], v2 = points1[3], v3 = points1[1]};
              tri.MatchToDirection(norm);
              firstSliceTriangles.Add(tri);
            }
            else
            {
              var tri = new Triangle() {v1 = points1[0], v2 = points1[3], v3 = points1[2]};
              tri.MatchToDirection(norm);
              firstSliceTriangles.Add(tri);
              tri = new Triangle() {v1 = points1[0], v2 = points1[2], v3 = points1[1]};
              tri.MatchToDirection(norm);
              firstSliceTriangles.Add(tri);
            }
          }

          // 2nd slice
          if (points2.Count == 3)
          {
            var tri = new Triangle() {v1 = points2[1], v2 = points2[0], v3 = points2[2]};
            tri.MatchToDirection(norm);
            secondSliceTriangles.Add(tri);
          }
          else
          {
            Debug.Assert(points2.Count == 4);
            if (Vector3.Dot((points2[0] - points2[1]), points2[2] - points2[3]) >= 0)
            {
              var tri = new Triangle() {v1 = points2[0], v2 = points2[2], v3 = points2[3]};
              tri.MatchToDirection(norm);
              secondSliceTriangles.Add(tri);
              tri = new Triangle() {v1 = points2[0], v2 = points2[3], v3 = points2[1]};
              tri.MatchToDirection(norm);
              secondSliceTriangles.Add(tri);
            }
            else
            {
              var tri = new Triangle() {v1 = points2[0], v2 = points2[3], v3 = points2[2]};
              tri.MatchToDirection(norm);
              secondSliceTriangles.Add(tri);
              tri = new Triangle() {v1 = points2[0], v2 = points2[2], v3 = points2[1]};
              tri.MatchToDirection(norm);
              secondSliceTriangles.Add(tri);
            }
          }
        }
        else // no intersection found, add the original triangle
        {
          if (cutPlane.GetSide(p1)) // check which side of the plane it is on
          {
            firstSliceTriangles.Add(new Triangle() {v1 = p1, v2 = p2, v3 = p3});
          }
          else
          {
            secondSliceTriangles.Add(new Triangle() {v1 = p1, v2 = p2, v3 = p3});
          }
        }
      }

      if (edgeIntersections.Count > 1) // generate new geometry
      {
        var center = Vector3.zero;
        foreach (var vec in edgeIntersections) // find average point TODO: for more complex shapes this doesn't work
          center += vec;
        center /= edgeIntersections.Count;
        for (int i = 0; i < edgeIntersections.Count; i++)
        {
          var tri = new Triangle()
          {
            v1 = edgeIntersections[i], v2 = center,
            v3 = i + 1 == edgeIntersections.Count ? edgeIntersections[i] : edgeIntersections[i + 1]
          };
          tri.MatchToDirection(-cutPlane.normal);
          firstSliceTriangles.Add(tri);
        }

        for (int i = 0; i < edgeIntersections.Count; i++)
        {
          var tri = new Triangle()
          {
            v1 = edgeIntersections[i], v2 = center,
            v3 = i + 1 == edgeIntersections.Count ? edgeIntersections[i] : edgeIntersections[i + 1]
          };
          tri.MatchToDirection(cutPlane.normal);
          secondSliceTriangles.Add(tri);
        }
      }
    }
  }
}