using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoardAreaClickCatcher : MonoBehaviour
{
    private GameController controller;

    private void Awake()
    {
        controller = FindObjectOfType<GameController>();
    }

    private void OnMouseDown()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<GameController>();
        }

        controller?.HandleBoardAreaClicked();
    }
}
