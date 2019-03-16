using UnityEngine;
using System.Collections.Generic;

public class Slice : MonoBehaviour
{
  private struct Triangle
  {
    public Vector3 v1, v2, v3;

    public void MatchToDirection(Vector3 dir)
    {
      if (!IsNormalCodirectionalWith(dir))
        SwapVertices(v1, v3);
    }

    private bool IsNormalCodirectionalWith(Vector3 dir) => Vector3.Dot(GetNormal(), dir) > 0;

    private Vector3 GetNormal() => Vector3.Cross(v1 - v2, v1 - v3).normalized;

    private static void SwapVertices(Vector3 vertex1, Vector3 vertex2)
    {
      var tmp = vertex1;
      vertex1 = vertex2;
      vertex2 = tmp;
    }
  }

  // Keep in mind this is absolutely horrible as it is now
  // you'd like to clean stuff up and not recreate the lists every frame
  // also this code doesn't need to run every frame, but only when you actually slice something
  // and there is a bug somewhere, the slices sometimes have holes in them
  void Update()
  {
    // find the points the define the slicing plane
    Transform cTrans = Camera.main.transform;
    Vector3 o1 = cTrans.position;
    Vector3 o2 = cTrans.position + cTrans.forward * 2f - cTrans.right;
    Vector3 o3 = cTrans.position + cTrans.forward * 2f + cTrans.right;

    // draw a triangle tha defines thes plane
    Debug.DrawLine(o1, o2, Color.red);
    Debug.DrawLine(o2, o3, Color.red);
    Debug.DrawLine(o3, o1, Color.red);

    RaycastHit hit;
    if (
      Physics.Linecast(o1, o2,
        out hit) // there is definitely better ways for this (checks if the triangle edges intersect with anything)
      || Physics.Linecast(o2, o3, out hit)
      || Physics.Linecast(o3, o1, out hit))
    {
      Plane pl = new Plane(o1, o2, o3);
      var tr = hit.collider.gameObject.transform;
      var m = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
      var triangles = m.triangles;
      var verts = m.vertices;

      List<Vector3> intersections = new List<Vector3>(); // list that stores all the found edge intersections
      List<Triangle> newTris1 = new List<Triangle>(); // list that stores the triangles for first slice
      List<Triangle> newTris2 = new List<Triangle>(); // second slice

      for (int i = 0; i < triangles.Length; i += 3) // loop all triangles in the mesh
      {
        List<Vector3> points = new List<Vector3>();

        int v1 = triangles[i];
        int v2 = triangles[i + 1];
        int v3 = triangles[i + 2];
        var p1 = tr.TransformPoint(verts[v1]); // vertex 1 in world coordinates
        var p2 = tr.TransformPoint(verts[v2]);
        var p3 = tr.TransformPoint(verts[v3]);
        var norm = Vector3.Cross(p1 - p2, p1 - p3); // normal of the triangle
        Debug.DrawLine(p1, p1 + norm, Color.cyan);

        var dir = p2 - p1;
        float ent;
        Debug.DrawRay(p1, dir);
        if (pl.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude) // check for intersections in the first edge
        {
          var intersection = p1 + ent * dir.normalized;
          intersections.Add(intersection);
          points.Add(intersection);
        }

        dir = p3 - p2;
        Debug.DrawRay(p2, dir);
        if (pl.Raycast(new Ray(p2, dir), out ent) && ent <= dir.magnitude) // 2nd edge
        {
          var intersection = p2 + ent * dir.normalized;
          intersections.Add(intersection);
          points.Add(intersection);
        }

        dir = p3 - p1;
        Debug.DrawRay(p1, dir);
        if (pl.Raycast(new Ray(p1, dir), out ent) && ent <= dir.magnitude) // 3rd edge
        {
          var intersection = p1 + ent * dir.normalized;
          intersections.Add(intersection);
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
          if (pl.GetSide(p1)) points1.Add(p1); // check on which side each of the original vertices was
          else points2.Add(p1);
          if (pl.GetSide(p2)) points1.Add(p2);
          else points2.Add(p2);
          if (pl.GetSide(p3)) points1.Add(p3);
          else points2.Add(p3);

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

          // 2nd slice
          if (points2.Count == 3)
          {
            var tri = new Triangle() {v1 = points2[1], v2 = points2[0], v3 = points2[2]};
            tri.MatchToDirection(norm);
            newTris2.Add(tri);
          }
          else
          {
            Debug.Assert(points2.Count == 4);
            if (Vector3.Dot((points2[0] - points2[1]), points2[2] - points2[3]) >= 0)
            {
              var tri = new Triangle() {v1 = points2[0], v2 = points2[2], v3 = points2[3]};
              tri.MatchToDirection(norm);
              newTris2.Add(tri);
              tri = new Triangle() {v1 = points2[0], v2 = points2[3], v3 = points2[1]};
              tri.MatchToDirection(norm);
              newTris2.Add(tri);
            }
            else
            {
              var tri = new Triangle() {v1 = points2[0], v2 = points2[3], v3 = points2[2]};
              tri.MatchToDirection(norm);
              newTris2.Add(tri);
              tri = new Triangle() {v1 = points2[0], v2 = points2[2], v3 = points2[1]};
              tri.MatchToDirection(norm);
              newTris2.Add(tri);
            }
          }
        }
        else // no intersection found, add the original triangle
        {
          if (pl.GetSide(p1)) // check which side of the plane it is on
          {
            newTris1.Add(new Triangle() {v1 = p1, v2 = p2, v3 = p3});
          }
          else
          {
            newTris2.Add(new Triangle() {v1 = p1, v2 = p2, v3 = p3});
          }
        }
      }

      if (intersections.Count > 1) // generate new geometry
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

      if (intersections.Count > 0 && Input.GetMouseButtonDown(0)
      ) // intersects and player pressed button, in a realistic use case, you'd like to nest all the above code in this block
      {
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

      foreach (var thing in intersections)
      {
        Debug.DrawLine(thing, thing + Vector3.up * 0.05f, Color.blue);
      }
    }
  }
}