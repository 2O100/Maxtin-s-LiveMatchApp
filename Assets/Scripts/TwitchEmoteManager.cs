using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class TwitchEmoteManager : MonoBehaviour
{
    public static TwitchEmoteManager Instance;

    [Header("--- TMP Asset Dynamique ---")]
    public TMP_SpriteAsset runtimeSpriteAsset;

    // Cache local pour éviter de re-télécharger la même émote plusieurs fois
    private Dictionary<string, Sprite> emoteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeSpriteAsset();
    }

    /// <summary>
    /// Initialise un Sprite Asset vierge au démarrage
    /// </summary>
    private void InitializeSpriteAsset()
    {
        if (runtimeSpriteAsset == null)
        {
            runtimeSpriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            runtimeSpriteAsset.name = "TwitchEmotesRuntime";
        }
    }

    /// <summary>
    /// Télécharge une émote par son ID si elle n'est pas déjà en mémoire
    /// </summary>
    public void RequestEmote(string emoteId, System.Action onComplete = null)
    {
        if (emoteCache.ContainsKey(emoteId))
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(DownloadAndRegisterEmote(emoteId, onComplete));
    }

    private IEnumerator DownloadAndRegisterEmote(string emoteId, System.Action onComplete)
    {
        string url = $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/static/light/2.0";

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                sprite.name = emoteId;

                // Ajouter au cache local
                emoteCache[emoteId] = sprite;

                // Injecter le sprite dans le TMP_SpriteAsset de TextMeshPro
                AddSpriteToTMPAsset(sprite, emoteId);

                Debug.Log($"[EmoteManager] Émote {emoteId} téléchargée et ajoutée !");
                onComplete?.Invoke();
            }
        }
    }

    private void AddSpriteToTMPAsset(Sprite sprite, string emoteId)
    {
        if (runtimeSpriteAsset == null) return;

        // Ajouter le sprite à la liste des sprites de l'asset
        TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
        {
            index = (uint)runtimeSpriteAsset.spriteCharacterTable.Count,
            metrics = new UnityEngine.TextCore.GlyphMetrics(sprite.rect.width, sprite.rect.height, 0, sprite.rect.height * 0.8f, sprite.rect.width),
            glyphRect = new UnityEngine.TextCore.GlyphRect(0, 0, (int)sprite.rect.width, (int)sprite.rect.height)
        };

        TMP_SpriteCharacter character = new TMP_SpriteCharacter((uint)runtimeSpriteAsset.spriteCharacterTable.Count, glyph)
        {
            name = emoteId,
            scale = 1.0f
        };

        runtimeSpriteAsset.spriteGlyphTable.Add(glyph);
        runtimeSpriteAsset.spriteCharacterTable.Add(character);

        // Mise à jour de la table de recherche de TextMeshPro
        runtimeSpriteAsset.UpdateLookupTables();
    }
}