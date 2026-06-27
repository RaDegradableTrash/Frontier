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
        camera.clearFlags = CameraClearFlags.Skybox;

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
