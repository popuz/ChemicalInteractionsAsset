using NUnit.Framework;
using UnityEngine;

public class VolumeCalculatorTest
{
  private const float PRECISION = 1E-6f;

  [TestCase(PrimitiveType.Quad, 0f)]
  [TestCase(PrimitiveType.Cube, 1f)]
  public void DefaultPrimitive_HasDefaultVolume(PrimitiveType primitive, float expectedVolume)
    => Assert.AreEqual(expectedVolume,
      GameObject.CreatePrimitive(primitive).AddComponent<VolumeCalculator>().GetMeshVolume(), PRECISION);

  
  [TestCase(0.3f, 0.1f, 0.5f)]
  [TestCase(3, 5, 7)]
  [TestCase(7000, 50000, 100000)]
  public void MovedDefaultCube_VolumeEqualsOne(float x, float y, float z)
  {
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.Translate(new Vector3(x, y, z));
    Assert.AreEqual(1f, cube.AddComponent<VolumeCalculator>().GetMeshVolume());
  }

  [TestCase(0.3f, 0.1f, 0.5f)]
  [TestCase(3, 5, 7)]
  [TestCase(7000, 50000, 100000)]
  public void RotatedDefaultCube_VolumeEqualsOne(float x, float y, float z)
  {
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.Rotate(x, y, z);
    Assert.AreEqual(1f, cube.AddComponent<VolumeCalculator>().GetMeshVolume());
  }

  [TestCase(0.1f)]
  [TestCase(0.2f)]
  [TestCase(0.3f)]
  [TestCase(0.5f)]
  [TestCase(0.7f)]
  [TestCase(1f)]
  [TestCase(20f)]
  [TestCase(2000f)]
  public void ScaledCubeInXAxis_HasScaledVolume(float x)
  {
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.transform.localScale = new Vector3(x, 1, 1);
    Assert.AreEqual(x, cube.AddComponent<VolumeCalculator>().GetMeshVolume(), PRECISION);
  }
}