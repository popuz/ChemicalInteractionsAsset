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

  private void AlignToDirection(Vector3 dir)
  {
    if (!IsNormalCodirectional(dir))
      SwapVertices();
  }

  private bool IsNormalCodirectional(Vector3 dir) => Vector3.Dot(GetNormalDirection(), dir) > 0;

  private Vector3 GetNormalDirection() => Vector3.Cross(v1 - v2, v1 - v3);
  
  private void SwapVertices()
  {
    var tmp = v2;
    v2 = v3;
    v3 = tmp;
  }
}