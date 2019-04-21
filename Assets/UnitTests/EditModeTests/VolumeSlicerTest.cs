using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class VolumeSlicerTest
{
  private const float EPS = 1E-6f;
  private VolumeSlicer _slicer;

  [SetUp]
  public void CreateSlicer() => _slicer = new VolumeSlicer(0f);

  private void AssertSlicedTrianglesCount(int expected, GameObject objectToSlice)
  {
    _slicer.Init(objectToSlice);
    Assert.AreEqual(expected, _slicer.MakeSlice().Count);
  }

  [Test]
  public void SliceWithoutInit_ReturnsZeroTriangles() => Assert.AreEqual(0, _slicer.MakeSlice().Count);

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

  [TestFixture]
  public class QuadSlicerTrianglesTest : VolumeSlicerTest
  {
    private GameObject _quadObject;
    private Mesh _mesh;

    [SetUp]
    public void CreateQuad()
    {
      _quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
      _mesh = _quadObject.GetComponent<MeshFilter>().sharedMesh;
    }

    [TestCase(-1f, TestName = "Slice below the Quad")]
    [TestCase(-0.5f, TestName = "Slice Quad on bottom edge")]
    public void SliceQuad_ZeroTrianglesReturned(float slicerShift)
    {
      _slicer.SlicerShift = slicerShift;
      AssertSlicedTrianglesCount(0, _quadObject);
    }

    [TestCase(1f, TestName = "Slice under the Quad")]
    [TestCase(0.5f, TestName = "Slice Quad on top edge")]
    [TestCase(0.0f, 90f, 'x', TestName = "Slice rotated Quad on its only face (In Plane)")]
    [TestCase(0.7071068f, 45f, 'z', TestName = "Slice rotated Quad on top intersect vertex on angle")]
    [TestCase(0.7071068f, -45f, 'z', TestName = "Slice rotated Quad on top intersect vertex on angle")]
    public void SliceQuad_AllTrianglesReturned(float slicerShift, float quadRotation = 0f, char axisName = 'x')
    {
      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if (quadRotation != 0f)
      {
        var rotAxis = axisName == 'x' ? Vector3.right : axisName == 'z' ? Vector3.forward : Vector3.up;
        _quadObject.transform.Rotate(rotAxis, quadRotation);
      }

      _slicer.SlicerShift = slicerShift;
      AssertSlicedTrianglesCount(_mesh.triangles.Length / 3, _quadObject);
    }

    [TestCase(0.49f)]
    [TestCase(0.25f)]
    [TestCase(0)]
    [TestCase(-0.49f)]
    [TestCase(-0.25f)]
    public void SliceQuad_OnTheMiddle_3TrianglesReturned(float slicerShift)
    {
      _slicer.SlicerShift = slicerShift;
      AssertSlicedTrianglesCount(3, _quadObject);
    }

    [TestCase(0.25f, 45f, 4, TestName = "Slice 2 Triangles In Rotated Quad Above Middle")]
    [TestCase(0.25f, -45f, 3, TestName = "Slice 1 Triangle In Rotated Quad Above Middle")]
    [TestCase(0.0f, 45f, 2, TestName = "Slice 2 Triangles In Rotated Quad In the Middle")]
    [TestCase(-0.25f, 45f, 2, TestName = "Slice 2 Triangles In Rotated Quad below the Middle")]
    [TestCase(0.0f, -45f, 1, TestName = "Slice 1 Last Triangle In Rotated Quad In the Middle")]
    [TestCase(-0.25f, -45f, 1, TestName = "Slice 1 Last Triangle In Rotated Quad below the Middle")]
    public void SliceRotatedQuad_SomeTrianglesReturned(float slicerShift, float quadRotation, int expectedTrisAmount)
    {
      _slicer.SlicerShift = slicerShift;
      _quadObject.transform.Rotate(Vector3.forward, quadRotation);
      AssertSlicedTrianglesCount(expectedTrisAmount, _quadObject);
    }

    [TestFixture]
    public class QuadSlicerVerticesTest : QuadSlicerTrianglesTest
    {
      private static int AmountOfVerticesBelowTheSlice(List<Triangle> tris, float slicerShift)
      {
        var vAmount = 0;
        foreach (var t in tris)
          for (var i = 0; i < 3; i++)
            if (t[i].y <= slicerShift + EPS)
              vAmount++;
        return vAmount;
      }

      [TestCase(1f)]
      [TestCase(0.5f)]
      [TestCase(0.49f)]
      [TestCase(0.25f)]
      [TestCase(0)]
      [TestCase(-0.25f)]
      [TestCase(-0.49f)]
      [TestCase(-0.5f)]
      [TestCase(-1f)]
      [TestCase(0f, 90f, 'x')]
      [TestCase(0.7071068f, 45f, 'z')]
      [TestCase(0.7071068f, -45f, 'z')]
      [TestCase(0.25f, -45f, 'z')]
      [TestCase(0f, -45f, 'z')]
      [TestCase(-0.25f, -45f, 'z')]
      [TestCase(0.25f, 45f, 'z')]
      [TestCase(0f, 45f, 'z')]
      [TestCase(-0.25f, 45f, 'z')]
      public void SliceQuad_HasAllVertices_BelowTheCut(float slicerShift, float quadRotation = 0f, char axisName = 'x')
      {
        var rotAxis = axisName == 'x' ? Vector3.right : axisName == 'z' ? Vector3.forward : Vector3.up;
        _quadObject.transform.Rotate(rotAxis, quadRotation);
        _slicer.SlicerShift = slicerShift;
        _slicer.Init(_quadObject);

        var tris = _slicer.MakeSlice();

        Assert.AreEqual(tris.Count * 3, AmountOfVerticesBelowTheSlice(tris, slicerShift));
      }

      [TestCase(0.49f)]
      [TestCase(0.25f)]
      [TestCase(0)]
      [TestCase(-0.49f)]
      [TestCase(-0.25f)]
      public void SliceQuad_PerpendicularToTriangles_ReturnedTrianglesAreNotCoincides(float slicerShift)
      {
        _slicer.SlicerShift = slicerShift;
        _slicer.Init(_quadObject);

        var tris = _slicer.MakeSlice();

        Assert.IsFalse(tris[0].v1 == tris[1].v1 && tris[0].v2 == tris[1].v2 && tris[0].v3 == tris[1].v3);
      }
      
      [TestCase(0.25f)]
      [TestCase(0f)]
      [TestCase(-0.25f)]
      public void SliceRotatedQuad_PerpendicularToTriangles_ReturnedTrianglesAreNotCoincides(float slicerShift)
      {
        _quadObject.transform.Rotate(Vector3.forward, 45);
        _slicer.SlicerShift = slicerShift;
        _slicer.Init(_quadObject);

        var tris = _slicer.MakeSlice();

        Assert.IsFalse(tris[0].v1 == tris[1].v1 && tris[0].v2 == tris[1].v2 && tris[0].v3 == tris[1].v3);
      }          
      
    }
  }
}