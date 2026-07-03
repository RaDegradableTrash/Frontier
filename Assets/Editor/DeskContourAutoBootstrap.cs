using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DeskContourAutoBootstrap
{
    private const string DesktopQuadName = "DesktopQuad";

    static DeskContourAutoBootstrap()
    {
        EditorApplication.delayCall += EnsureDesktopQuadBackdrop;
        EditorApplication.projectChanged += () =>
        {
            EditorApplication.delayCall -= EnsureDesktopQuadBackdrop;
            EditorApplication.delayCall += EnsureDesktopQuadBackdrop;
        };
    }

    private static void EnsureDesktopQuadBackdrop()
    {
        GameObject tabletop = GameObject.Find(DesktopQuadName);
        if (tabletop == null)
        {
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            generator = Undo.AddComponent<DeskContourTerrainGenerator>(tabletop);
        }

        generator.ApplyDarkBottomWhiteLineTabletopPreset();
        Debug.Log("[DeskContourBootstrap] Auto-applied dark-bottom white-line backdrop to DesktopQuad.");
        EditorUtility.SetDirty(tabletop);
    }
}
