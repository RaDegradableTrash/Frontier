using UnityEngine;

public static class SceneHierarchyOrganizer
{
    private const string RuntimeRootName = "Runtime Scene";
    private const string TableRootName = "Table Presentation";
    private const string UiRootName = "Scene UI";
    private const string CardsRootName = "Runtime Cards";
    private const string FxRootName = "Runtime FX";
    private static bool organizeScheduled;
    private static bool isOrganizing;
    private static int scheduleFrame;

    public static void Organize()
    {
#if UNITY_EDITOR
        if (isOrganizing || !CanOrganizeNow())
        {
            return;
        }

        ScheduleDeferredOrganize();
        return;
#endif
#if !UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }

        OrganizeNow();
#endif
    }

#if UNITY_EDITOR
    private static void ScheduleDeferredOrganize()
    {
        if (organizeScheduled)
        {
            return;
        }

        organizeScheduled = true;
        scheduleFrame = Time.frameCount;
        UnityEditor.EditorApplication.update -= FlushOrganize;
        UnityEditor.EditorApplication.update += FlushOrganize;
    }

    private static void FlushOrganize()
    {
        if (!Application.isPlaying)
        {
            organizeScheduled = false;
            UnityEditor.EditorApplication.update -= FlushOrganize;
            return;
        }

        if (Time.frameCount <= scheduleFrame)
        {
            return;
        }

        if (isOrganizing || !CanOrganizeNow() || EditorConsistencyCallbackInProgress())
        {
            if (Application.isPlaying)
            {
                organizeScheduled = true;
                scheduleFrame = Time.frameCount;
            }
            else
            {
                organizeScheduled = false;
            }

            return;
        }

        organizeScheduled = false;
        isOrganizing = true;
        try
        {
            OrganizeNow();
        }
        finally
        {
            isOrganizing = false;
            UnityEditor.EditorApplication.update -= FlushOrganize;
        }
    }

    private static bool EditorConsistencyCallbackInProgress()
    {
        var stack = new System.Diagnostics.StackTrace();
        for (int i = 1; i < stack.FrameCount; i++)
        {
            var method = stack.GetFrame(i)?.GetMethod();
            var name = method?.Name ?? string.Empty;
            if (
                name.Contains("OnValidate") ||
                name.Contains("CheckConsistency") ||
                name.Contains("OnBeforeTransformParentChanged") ||
                name.Contains("OnTransformParentChanged") ||
                name.Contains("OnTransformChildrenChanged") ||
                name.Contains("Awake") ||
                name.Contains("OnEnable") ||
                name.Contains("OnDisable"))
            {
                return true;
            }
        }

        return false;
    }

#endif

    private static bool CanOrganizeNow()
    {
#if UNITY_EDITOR
        return !UnityEditor.EditorApplication.isCompiling
            && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
            && !UnityEditor.EditorApplication.isUpdating
            && Time.frameCount >= 2
            && !EditorConsistencyCallbackInProgress()
            && Application.isPlaying;
#else
        return true;
#endif
    }

    private static void OrganizeNow()
    {
        Transform runtimeRoot = EnsureRoot(RuntimeRootName);
        Transform tableRoot = EnsureChild(runtimeRoot, TableRootName);
        Transform uiRoot = EnsureChild(runtimeRoot, UiRootName);
        Transform cardsRoot = EnsureChild(runtimeRoot, CardsRootName);
        Transform fxRoot = EnsureChild(runtimeRoot, FxRootName);

        ReparentIfExists("Battlefield Surface", tableRoot);
        ReparentIfExists("Dark Table Border", tableRoot);
        ReparentIfExists("Player Hand Rail", tableRoot);
        ReparentStartsWith("Surface Groove", tableRoot);

        ReparentIfExists("Action Prompt", uiRoot);
        ReparentIfExists("Action Prompt Backing", uiRoot);
        ReparentStartsWith("Command ", uiRoot);
        ReparentStartsWith("Player Kredit Display", uiRoot);
        ReparentStartsWith("Enemy Kredit Display", uiRoot);
        ReparentContains(" Pile", uiRoot);
        ReparentContains("Deck Summary", uiRoot);
        ReparentContains("Inspector Display", uiRoot);
        ReparentContains("Status Display", uiRoot);

        ReparentStartsWith("Card_", cardsRoot);
        ReparentStartsWith("Drag Target Arrow", fxRoot);
        ReparentStartsWith("FloatingText_", fxRoot);
        ReparentStartsWith("AttackTracer", fxRoot);
        ReparentStartsWith("PlayedOrder_", cardsRoot);
    }

    private static Transform EnsureRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing == null)
        {
            existing = new GameObject(name);
        }

        return existing.transform;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }

        return child;
    }

    private static void ReparentIfExists(string objectName, Transform parent)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null && target.transform.parent != parent)
        {
            target.transform.SetParent(parent, true);
        }
    }

    private static void ReparentStartsWith(string prefix, Transform parent)
    {
        GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject target in objects)
        {
            if (target.name.StartsWith(prefix) && target.transform.parent != parent)
            {
                target.transform.SetParent(parent, true);
            }
        }
    }

    private static void ReparentContains(string fragment, Transform parent)
    {
        GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject target in objects)
        {
            if (target.name.Contains(fragment) && target.transform.parent != parent)
            {
                target.transform.SetParent(parent, true);
            }
        }
    }
}
