using System;
using System.IO;

public static class SceneSerializedLayoutTests
{
    public static int Main()
    {
        string scene = File.ReadAllText("Assets/Scenes/SampleScene.unity");
        AssertTrue(scene.Contains("m_LocalScale: {x: 15.8, y: 9.8, z: 1}"), "DesktopQuad should serialize the wide tabletop scale used in Play mode.");
        AssertTrue(!scene.Contains("m_LocalPosition: {x: 3.72, y: 0.16"), "Scene command buttons should not serialize the old headquarters-crowding column.");
        AssertTrue(scene.Contains("m_LocalPosition: {x: 5.34, y: 0.16, z: -0.10}"), "End Turn should serialize into the right-side rail.");
        AssertTrue(scene.Contains("playerHandAnchor: {x: 0, y: 0.18, z: -4.18}"), "Scene hand layout should serialize the tucked hand anchor.");
        AssertTrue(scene.Contains("playerHandRevealedAnchor: {x: 0, y: 0.18, z: -3.56}"), "Scene hand layout should serialize the non-jarring revealed hand anchor.");
        return 0;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
