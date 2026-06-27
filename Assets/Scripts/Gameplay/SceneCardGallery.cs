using UnityEngine;

public class SceneCardGallery : MonoBehaviour
{
    [SerializeField] private SceneCardLibrary library;
    [SerializeField] private int columns = 3;
    [SerializeField] private float spacingX = 0.86f;
    [SerializeField] private float spacingZ = 1.12f;
    [SerializeField] private Vector3 localOrigin = new Vector3(-1.05f, 0.08f, -3.35f);
    [SerializeField] private float previewScale = 0.55f;
    [SerializeField] private bool showRuntimeGallery;

    private void Start()
    {
        if (!showRuntimeGallery)
        {
            return;
        }

        BuildGallery();
    }

    private void BuildGallery()
    {
        if (library == null)
        {
            library = GetComponent<SceneCardLibrary>();
        }

        if (library == null || library.authoredCards == null)
        {
            return;
        }

        ClearExistingCards();
        for (int i = 0; i < library.authoredCards.Count; i++)
        {
            CardData cardData = library.authoredCards[i];
            if (cardData == null)
            {
                continue;
            }

            RuntimeCard previewCard = cardData.ToRuntimeCard(PlayerSide.Player);
            GameObject cardObject = new GameObject($"Gallery_{previewCard.CardName}");
            cardObject.transform.SetParent(transform, false);
            CardGallerySlot slot = CardGalleryLayout.SlotFor(i, columns);
            cardObject.transform.localPosition = localOrigin + new Vector3(slot.Column * spacingX, 0f, slot.Row * spacingZ);

            CardView view = cardObject.AddComponent<CardView>();
            view.Initialize(previewCard, null);
            cardObject.transform.localScale = Vector3.one * previewScale;
        }
    }

    private void ClearExistingCards()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Gallery_"))
            {
                Destroy(child.gameObject);
            }
        }
    }
}
