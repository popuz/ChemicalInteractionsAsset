using UnityEngine;

public class VolumeCalculator : MonoBehaviour
{
  private Mesh _mesh;

  private void Awake() => _mesh = GetComponent<MeshFilter>().mesh;

  private void Start() =>
    Debug.Log($"{_mesh.name}: Unscaled = {VolumeOfMesh(_mesh)}    Scaled = {VolumeOfMeshScaled(_mesh)}");

  private float VolumeOfMesh(Mesh mesh)
  {
    var volume = 0f;
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;

    for (var i = 0; i < mesh.triangles.Length; i += 3)          
      volume += TripleProduct(vertices[triangles[i + 0]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
    
    var lossyScale = transform.lossyScale;    
    return Mathf.Abs(volume) / 6f * lossyScale.x * lossyScale.y * lossyScale.z;
  }
  
  private float VolumeOfMeshScaled(Mesh mesh)
  {
    float volume = 0;
    var vertices = mesh.vertices;
    var triangles = mesh.triangles;

    for (var i = 0; i < mesh.triangles.Length; i += 3)
    {
      var p1 = transform.TransformPoint(vertices[triangles[i + 0]]);
      var p2 = transform.TransformPoint(vertices[triangles[i + 1]]);
      var p3 = transform.TransformPoint(vertices[triangles[i + 2]]);

      volume += TripleProduct(p1, p2, p3);
    }

    return Mathf.Abs(volume) / 6f;
  } 
  
  private static float TripleProduct(Vector3 v1, Vector3 v2, Vector3 v3)
    => Vector3.Dot(Vector3.Cross(v1, v2), v3);
    

}