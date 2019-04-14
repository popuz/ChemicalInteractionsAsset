using NUnit.Framework;
using UnityEngine;

public class VolumeSlicerQuadTest
{
  private VolumeSlicer slicer;

  [SetUp]
  public void SetUp() => slicer = new VolumeSlicer(0f);

  private void AssertSlicedTrianglesCount(int expected, GameObject objectToSlice)
  {
    slicer.Init(objectToSlice);
    Assert.AreEqual(expected, slicer.MakeSlice().Count);
  }

  #region ASSERT TRIANGLES COUNT

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
    slicer = new VolumeSlicer(-0.5f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;

    AssertSlicedTrianglesCount(0, go);
  }

  #endregion

  #region ALL TRIANGLES RETURNED  

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

    go.transform.Rotate(Vector3.right, 90);

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }

  [Test]
  public void OnTopSliceOfQuad_ReturnsAllTriangles()
  {
    slicer = new VolumeSlicer(0.5f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
    var mesh = go.GetComponent<MeshFilter>().sharedMesh;

    AssertSlicedTrianglesCount(mesh.triangles.Length / 3, go);
  }

  #endregion

  [TestCase(0.49f)]
  [TestCase(0.25f)]
  [TestCase(0)]
  [TestCase(-0.49f)]
  [TestCase(-0.25f)]
  public void SliceQuadOnTriangles_ReturnsTwoTriangles(float slicerShift)
  {
    slicer = new VolumeSlicer(slicerShift);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    AssertSlicedTrianglesCount(2, go);
  }

  [Test]
  public void HalfOfRotatedQuadSlicedByEdge_ReturnsOneTriangle()
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, -45);

    AssertSlicedTrianglesCount(1, go);
  }

  [Test]
  public void SliceOneLastTriangleInRotatedQuad_ReturnsOneTriangle()
  {
    slicer = new VolumeSlicer(-0.25f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, -45);

    AssertSlicedTrianglesCount(1, go);
  }

  [Test]
  public void SliceOneTriangleInRotatedQuad_ReturnsOneTrianglePlusRemainedBelow()
  {
    slicer = new VolumeSlicer(0.25f);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, -45);

    AssertSlicedTrianglesCount(2, go);
  }

  [Test]
  public void SliceRotatedQuadOnHalf_ReturnsTwoTriangles()
  {
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, 45);

    AssertSlicedTrianglesCount(2, go);
  }
  
  [TestCase(0.25f)]
  [TestCase(-0.25f)]
  public void SliceTwoTrianglesInRotatedQuad_ReturnsTwoTriangles(float slicerShift)
  {
    slicer = new VolumeSlicer(slicerShift);
    var go = GameObject.CreatePrimitive(PrimitiveType.Quad);

    go.transform.Rotate(Vector3.forward, 45);

    AssertSlicedTrianglesCount(2, go);
  }

  #endregion
}