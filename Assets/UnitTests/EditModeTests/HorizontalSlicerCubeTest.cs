using NUnit.Framework;
using UnityEngine;

public class HorizontalSlicerCubeTest : MonoBehaviour
{
  private HorizontalSlicer _slicer;
  private GameObject _cube;

  [SetUp]
  public void Init()
  {
    _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    _slicer = _cube.AddComponent<HorizontalSlicer>();
  }

  [Test]
  public void DefaultCube_SliceBelow_HasNoTriangles()
  {
    _slicer.SlicerShift = -1f;
    _slicer.MakeSlice();
    _slicer.TriangulateSlicedSide();
    Assert.AreEqual(0, _slicer.SlicedTris.Count);
  }

  [Test]
  public void DefaultCube_SliceOnFaceBottom_HasTwoTriangles()
  {
    _slicer.SlicerShift = -0.5f;
    _slicer.MakeSlice();
    _slicer.TriangulateSlicedSide();    
    Assert.AreEqual(2, _slicer.SlicedTris.Count);
  }

  [Test]
  public void DefaultCube_SliceAbove_HasAllTriangles()
  {
    _slicer.SlicerShift = 1f;
    _slicer.MakeSlice();
    _slicer.TriangulateSlicedSide();
    Assert.AreEqual(_cube.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, _slicer.SlicedTris.Count);
  }

  [Test]
  public void DefaultCube_SliceOnTopFace_HasAllTriangles()
  {
    _slicer.SlicerShift = 0.5f;
    _slicer.MakeSlice();
    _slicer.TriangulateSlicedSide();
    Assert.AreEqual(_cube.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3, _slicer.SlicedTris.Count);
  }
}