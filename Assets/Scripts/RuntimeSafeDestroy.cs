using UnityEngine;

public static class RuntimeSafeDestroy
{
    public static void Destroy(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            };
#else
            UnityEngine.Object.Destroy(target);
#endif
        }
    }
}
