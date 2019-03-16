using UnityEngine;

public struct Triangle
{
  public Vector3 v1, v2, v3;

  public void MatchToDirection(Vector3 dir)
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