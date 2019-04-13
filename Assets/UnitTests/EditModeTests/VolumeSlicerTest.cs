using NUnit.Framework;
using UnityEngine;

public class VolumeSlicerTest
{
  private VolumeSlicer slicer;

  [SetUp]
  public void SetUp() => slicer = new VolumeSlicer(0f);

  private void AssertSlicedTrianglesCount(int expected, GameObject objectToSlice) =>
    Assert.AreEqual(expected, slicer.MakeSlice(objectToSlice).Count);

  [Test]
  public void SliceNull_ReturnsZeroTriangles() => AssertSlicedTrianglesCount(0, null);

  [Test]
  public void SliceEmptyGameObject_ReturnsZeroTriangles() => AssertSlicedTrianglesCount(0, new GameObject());

  [Test]
  public void SliceBelowQuad_ReturnsZeroTriangles()
  {
    slicer = new VolumeSlicer(-1f);
    AssertSlicedTrianglesCount(0, GameObject.CreatePrimitive(PrimitiveType.Quad));
  }
  
  [Test]
  public void SliceUnderQuad_ReturnsAllTriangles()
  {
    slicer = new VolumeSlicer(1f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }
  
  [Test]
  public void InPlaneSliceOfQuad_ReturnsAllTriangles()
  {    
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);    
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
    
    go.transform.Rotate(Vector3.right,90);
    
    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }
  
  [Test]
  public void OnBottomSliceOfQuad_ReturnsZeroTriangles()
  {    
    slicer = new VolumeSlicer(-0.5f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);    
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;       
    
    AssertSlicedTrianglesCount(0, go);
  }
  
  [Test]
  public void OnTopSliceOfQuad_ReturnsAllTriangles()
  {    
    slicer = new VolumeSlicer(0.5f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);    
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;       
    
    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }
  
  [Test]
  public void HalfOfRotatedQuadSlicedByEdge_ReturnsOneTriangle()
  {    
    slicer = new VolumeSlicer(0.0f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);           
    
    go.transform.Rotate(Vector3.forward,-45);
    
    AssertSlicedTrianglesCount(1, go);
  }
  
  [Test]
  public void SliceRotatedQuadOnHalfs_ReturnsTwoTriangles()
  {    
    slicer = new VolumeSlicer(0.0f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);           
    
    go.transform.Rotate(Vector3.forward,45);
    
    AssertSlicedTrianglesCount(2, go);
  }

}