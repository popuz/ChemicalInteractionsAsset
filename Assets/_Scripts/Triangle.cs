using UnityEngine;

public struct Triangle
{
  public Vector3 v1, v2, v3;

  public Triangle(Vector3[] vertices)
  {
    v1 = vertices[0];
    v2 = vertices[1];
    v3 = vertices[2];
  }
  
  public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
  {
    this.v1 = v1;
    this.v2 = v2;
    this.v3 = v3;
  }
  
  public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 norm)
  {
    this.v1 = v1;
    this.v2 = v2;
    this.v3 = v3;
    AlignToDirection(norm);
  }
  public void AlignToDirection(Vector3 dir)
  {
    if (!IsNormalCodirectional(dir))
      SwapVertices(v1, v3);
  }

  private bool IsNormalCodirectional(Vector3 dir) => Vector3.Dot(GetNormal(), dir) > 0;

  private Vector3 GetNormal() => Vector3.Cross(v1 - v2, v1 - v3).normalized;

  private static void SwapVertices(Vector3 vertex1, Vector3 vertex2)
  {
    var tmp = vertex1;
    vertex1 = vertex2;
    vertex2 = tmp;
  }
}