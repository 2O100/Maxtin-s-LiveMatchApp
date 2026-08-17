using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerUIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.75f;
        canvasGroup.blocksRaycasts = false;

        // Calcul du décalage entre le curseur et le pivot de la carte
        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, cam, out Vector2 localPointerPos);
        dragOffset = localPointerPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentRectTransform == null) return;

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, eventData.position, cam, out Vector2 localPoint))
        {
            // Déplacement fluide sans saut
            rectTransform.anchoredPosition = localPoint - dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}