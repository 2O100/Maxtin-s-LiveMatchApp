using UnityEngine;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;

    private void Start()
    {
        // 1. Récupère automatiquement le composant texte si non assigné
        if (!textComponent) textComponent = GetComponentInChildren<TMP_Text>();

        // 2. Assigne le Sprite Asset dynamique généré par le TwitchEmoteManager
        if (textComponent != null && TwitchEmoteManager.Instance != null)
        {
            textComponent.spriteAsset = TwitchEmoteManager.Instance.runtimeSpriteAsset;
        }
    }
}