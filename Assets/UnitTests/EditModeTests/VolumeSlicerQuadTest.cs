using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class VolumeSlicerQuadTest
{
  private const float EPS = 1E-6f;
  private VolumeSlicer slicer;

  [SetUp]
  public void SetUp() => slicer = new VolumeSlicer(0f);

  private void AssertSlicedTrianglesCount(int expected, GameObject objectToSlice)
  {
    slicer.Init(objectToSlice);
    Assert.AreEqual(expected, slicer.MakeSlice().Count);
  }
  
  private static int AmountOfVerticesBelowTheSlice(List<Triangle> tris, float slicerShift)
  {
    var vAmount = 0;
    foreach (var t in tris)
      for (var i = 0; i < 3; i++)
        if (t[i].y <= slicerShift + EPS)
          vAmount++;
    return vAmount;
  }

  #region ZERO TRIANGLES RETURNED

  [Test]
  public void SliceWithoutInit_ReturnsZeroTriangles() => Assert.AreEqual(0, slicer.MakeSlice().Count);

  [Test]
  public void SliceNull_ReturnsZeroTriangles() => AssertSlicedTrianglesCount(0, null);

  [Test]
  public void SliceEmptyGameObject_ReturnsZeroTriangles() => AssertSlicedTrianglesCount(0, new GameObject());

  [Test]
  public void SliceGameObjectWitEmptyMeshFilter_ReturnsZeroTriangles()
  {
    var go = new GameObject();
    go.AddComponent<MeshFilter>();
    AssertSlicedTrianglesCount(0, go);
  }

  [Test]
  public void SliceBelowQuad_ReturnsZeroTriangles()
  {
    slicer = new VolumeSlicer(-1f);
    AssertSlicedTrianglesCount(0, GameObject.CreatePrimitive(PrimitiveType.Quad));
  }

  [Test]
  public void OnBottomSliceOfQuad_ReturnsZeroTriangles()
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    slicer = new VolumeSlicer(-0.5f);
    
    AssertSlicedTrianglesCount(0, go);
  }

  #endregion

  #region ALL TRIANGLES RETURNED  

  [Test]
  public void SliceUnderQuad_ReturnsAllTriangles()
  {    
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
    
    slicer = new VolumeSlicer(1f);

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }

  [Test]
  public void OnTopSliceOfQuad_ReturnsAllTriangles()
  {    
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
    
    slicer = new VolumeSlicer(0.5f);

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }
  
  [TestCase(45f)]
  [TestCase(-45f)]
  public void SliceOnTop_IntersectOneVertex_ReturnsAllTriangles(float rot)
  {    
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
    go.transform.Rotate(Vector3.forward, rot);
    
    slicer = new VolumeSlicer(0.5f);

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }   

  [Test]
  public void InPlaneSliceOfQuad_ReturnsAllTriangles()
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;

    go.transform.Rotate(Vector3.right, 90);

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }

  #endregion

  #region TIRANGLES CASES 
  [TestCase(0.49f)]
  [TestCase(0.25f)]
  [TestCase(0)]
  [TestCase(-0.49f)]
  [TestCase(-0.25f)]
  public void SliceQuadOnTriangles_ReturnsTwoTriangles(float slicerShift)
  {
    slicer = new VolumeSlicer(slicerShift);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    AssertSlicedTrianglesCount(3, go);
  }
  
  [TestCase(0.25f)]
  public void SliceTwoTrianglesInRotatedQuad_AboveCenter_ReturnsFourTriangles(float slicerShift)
  {
    slicer = new VolumeSlicer(0.25f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, 45);

    AssertSlicedTrianglesCount(4, go);
  }    
  
  [Test]
  public void SliceOneTriangleInRotatedQuad_ReturnsTwoTrianglePlusRemainedBelow()
  {
    slicer = new VolumeSlicer(0.25f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, -45);

    AssertSlicedTrianglesCount(3, go);
  }
   
  [TestCase(0f)]
  [TestCase(-0.25f)]
  public void SliceTwoTrianglesInRotatedQuad_ReturnsTwoTriangles(float slicerShift)
  {
    slicer = new VolumeSlicer(slicerShift);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, 45);

    AssertSlicedTrianglesCount(2, go);
  }    
  
  [TestCase(0f)]
  [TestCase(-0.25f)]  
  public void SliceOneLastTriangleInRotatedQuad_ReturnsOneTriangle(float slicerShift)
  {
    slicer = new VolumeSlicer(slicerShift);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, -45);

    AssertSlicedTrianglesCount(1, go);
  }
  #endregion

  [TestCase(1f, 0f)]
  [TestCase(0.5f, 0f)]
  [TestCase(0f, 90f)]
  public void AllTrianglesSlice_HasAllVertices_BelowTheCut(float slicerShift, float quadRotation)
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    go.transform.Rotate(Vector3.right, quadRotation);
    slicer = new VolumeSlicer(slicerShift);
    slicer.Init(go);

    var tris = slicer.MakeSlice();

    Assert.AreEqual(tris.Count * 3, AmountOfVerticesBelowTheSlice(tris, slicerShift));
  }

  [TestCase(0.25f)]
  [TestCase(0f)]
  [TestCase(-0.25f)]
  public void SliceOnRotatedQuad_HasAllVertices_BelowTheCut(float slicerShift)
  {    
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    go.transform.Rotate(Vector3.forward, -45);
    
    slicer = new VolumeSlicer(slicerShift);
    slicer.Init(go);
    var tris = slicer.MakeSlice();

    Assert.AreEqual(tris.Count * 3, AmountOfVerticesBelowTheSlice(tris, slicerShift));
  }   
  
  //[TestCase(0.25f)]    
  [TestCase(0f)]
  [TestCase(-0.25f)]
  public void SliceTwoTrianglesInRotatedQuad_HasAllVertices_BelowTheCut(float slicerShift)
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    go.transform.Rotate(Vector3.forward, 45);
    
    slicer = new VolumeSlicer(slicerShift);
    slicer.Init(go);
    var tris = slicer.MakeSlice();

    Assert.AreEqual(tris.Count * 3, AmountOfVerticesBelowTheSlice(tris, slicerShift));
  }    
}