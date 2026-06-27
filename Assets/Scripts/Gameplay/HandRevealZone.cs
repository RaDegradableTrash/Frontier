using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HandRevealZone : MonoBehaviour
{
    private GameController controller;

    private void Awake()
    {
        controller = FindObjectOfType<GameController>();
    }

    private void OnMouseEnter()
    {
        SetRevealed(true);
    }

    private void OnMouseOver()
    {
        SetRevealed(true);
    }

    private void OnMouseDown()
    {
        SetRevealed(true);
    }

    private void OnMouseExit()
    {
        SetRevealed(false);
    }

    private void SetRevealed(bool revealed)
    {
        if (controller == null)
        {
            controller = FindObjectOfType<GameController>();
        }

        controller?.SetPlayerHandRevealRequested(revealed);
    }
}
