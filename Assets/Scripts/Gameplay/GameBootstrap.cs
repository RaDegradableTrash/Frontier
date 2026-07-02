using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePlayableScene()
    {
        EnsureCamera();
        EnsureLight();
        EnsureBoard();
        EnsureFeedbackManager();

        if (FindObjectOfType<GameController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("GameController");
        controllerObject.AddComponent<GameController>();
    }

    private static void EnsureCamera()
    {
        Camera camera = Camera.main ?? FindObjectOfType<Camera>();
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.position = new Vector3(0f, 6.8f, -6.2f);
        camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        camera.fieldOfView = 42f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.024f, 0.028f, 0.046f, 1f);
        if (RenderSettings.skybox == null)
        {
            Material darkSkybox = new Material(Shader.Find("Skybox/6 Sided"));
            if (darkSkybox.HasProperty("_Tint"))
            {
                darkSkybox.SetColor("_Tint", new Color(0.018f, 0.022f, 0.036f, 1f));
            }

            if (darkSkybox.HasProperty("_Exposure"))
            {
                darkSkybox.SetFloat("_Exposure", 0.45f);
            }

            RenderSettings.skybox = darkSkybox;
        }

        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Tint"))
        {
            RenderSettings.skybox.SetColor("_Tint", new Color(0.02f, 0.025f, 0.035f, 1f));
        }

        RenderSettings.ambientSkyColor = new Color(0.018f, 0.022f, 0.03f, 1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.086f, 0.11f, 1f);

        if (camera.GetComponent<CameraInteraction>() == null)
        {
            camera.gameObject.AddComponent<CameraInteraction>();
        }
    }

    private static void EnsureLight()
    {
        if (FindObjectOfType<Light>() != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void EnsureBoard()
    {
        if (FindObjectOfType<BoardManager>() != null)
        {
            return;
        }

        GameObject boardObject = new GameObject("Board");
        boardObject.AddComponent<BoardManager>();
    }

    private static void EnsureFeedbackManager()
    {
        if (FindObjectOfType<FeedbackManager>() != null)
        {
            return;
        }

        GameObject feedbackObject = new GameObject("FeedbackManager");
        feedbackObject.AddComponent<FeedbackManager>();
    }
}
