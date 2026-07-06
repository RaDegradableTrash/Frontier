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
        if (EditorApplication.isCompiling
            || EditorApplication.isUpdating
            || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject tabletop = GameObject.Find(DesktopQuadName);
        if (tabletop == null)
        {
            return;
        }

        DeskContourTerrainGenerator generator = tabletop.GetComponent<DeskContourTerrainGenerator>();
        if (generator == null)
        {
            Debug.LogWarning("[DeskContourBootstrap] DesktopQuad has no DeskContourTerrainGenerator. Use Frontier/Desk Contour menu actions to add or rebuild it.");
            return;
        }

        if (!generator.IsTablePresetApplied)
        {
            Debug.LogWarning("[DeskContourBootstrap] DesktopQuad contour preset is not marked as applied. Use Frontier/Desk Contour menu actions to apply it explicitly.");
        }
    }
}
