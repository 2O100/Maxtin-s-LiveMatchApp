using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class MatchManager : MonoBehaviour
{
    [Header("--- Références Panneau Tactique ---")]
    [SerializeField] private TMP_Text textTacticNameA;
    [SerializeField] private TMP_Text textTacticNameB;
    [SerializeField] private TMP_Text textCoachA;
    [SerializeField] private TMP_Text textCoachB;

    [Header("--- Modèles de Données ---")]
    public MatchData currentMatch;

    [Header("--- Couleurs Maillots (Inspector) ---")]
    [SerializeField] public Color colorTeamA = Color.red;
    [SerializeField] public Color colorTeamB = Color.blue;

    [Header("--- Références UI Score & Infos ---")]
    [SerializeField] private TMP_Text textNameA;
    [SerializeField] private TMP_Text textNameB;
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private TMP_Text textTimer;
    [SerializeField] private Image logoTeamA;
    [SerializeField] private Image logoTeamB;

    [Header("--- Références Terrain & Prefabs ---")]
    [SerializeField] private RectTransform zoneTeamA;
    [SerializeField] private RectTransform zoneTeamB;
    [SerializeField] private GameObject playerPrefab;

    [Header("--- Références Fil d'Événements ---")]
    [SerializeField] private Transform eventsContent;
    [SerializeField] private GameObject eventPrefab;

    [Header("--- Références Chat en Direct ---")]
    [SerializeField] private Transform chatContent;
    [SerializeField] private GameObject chatItemPrefab;
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("--- Limites UI & Performance ---")]
    [SerializeField] private int maxChatMessages = 50;

    public void ApplyRealMatchData(MatchData realData)
    {
        if (realData == null) return;
        currentMatch = realData;
        UpdateHeaderUI();
        SpawnPlayers();
        LoadEvents();
    }

    public void UpdateHeaderUI()
    {
        if (currentMatch == null) return;

        if (textNameA && currentMatch.teamA != null) textNameA.text = currentMatch.teamA.name;
        if (textNameB && currentMatch.teamB != null) textNameB.text = currentMatch.teamB.name;
        if (textScore) textScore.text = $"{currentMatch.scoreA} - {currentMatch.scoreB}";

        if (textTimer)
        {
            if (currentMatch.matchStatus == "HT") textTimer.text = "Mi-temps";
            else if (currentMatch.matchStatus == "FT") textTimer.text = "Terminé";
            else textTimer.text = $"{currentMatch.currentMinute}'";
        }

        if (logoTeamA && currentMatch.teamA != null && !string.IsNullOrEmpty(currentMatch.teamA.logoUrl))
        {
            StartCoroutine(DownloadTeamLogo(currentMatch.teamA.logoUrl, logoTeamA));
        }

        if (logoTeamB && currentMatch.teamB != null && !string.IsNullOrEmpty(currentMatch.teamB.logoUrl))
        {
            StartCoroutine(DownloadTeamLogo(currentMatch.teamB.logoUrl, logoTeamB));
        }

        if (textTacticNameA && currentMatch.teamA != null) textTacticNameA.text = currentMatch.teamA.name;
        if (textTacticNameB && currentMatch.teamB != null) textTacticNameB.text = currentMatch.teamB.name;

        if (textCoachA && currentMatch.teamA != null)
            textCoachA.text = string.IsNullOrEmpty(currentMatch.teamA.coachName) ? "Coach: N/A" : $"Coach: {currentMatch.teamA.coachName}";

        if (textCoachB && currentMatch.teamB != null)
            textCoachB.text = string.IsNullOrEmpty(currentMatch.teamB.coachName) ? "Coach: N/A" : $"Coach: {currentMatch.teamB.coachName}";
    }

    private IEnumerator DownloadTeamLogo(string url, Image targetImage)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                Sprite logoSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                targetImage.sprite = logoSprite;
                targetImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[UI] Impossible de télécharger le logo : {webRequest.error}");
            }
        }
    }

    public void SpawnPlayers()
    {
        if (currentMatch == null) return;

        if (currentMatch.teamA != null && currentMatch.teamA.players != null && zoneTeamA != null)
        {
            UpdateZonePlayers(currentMatch.teamA.players, zoneTeamA, colorTeamA);
        }

        if (currentMatch.teamB != null && currentMatch.teamB.players != null && zoneTeamB != null)
        {
            UpdateZonePlayers(currentMatch.teamB.players, zoneTeamB, colorTeamB);
        }
    }

    private void UpdateZonePlayers(List<PlayerData> players, RectTransform zone, Color shirtColor)
    {
        Vector2 zoneSize = zone.rect.size;

        if (zone.childCount == 0)
        {
            foreach (var player in players)
            {
                GameObject pItem = Instantiate(playerPrefab, zone);
                float posX = (player.pitchPosition.x - 0.5f) * zoneSize.x;
                float posY = (player.pitchPosition.y - 0.5f) * zoneSize.y;

                pItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(posX, posY);
                UpdatePlayerCardUI(pItem, player.name, player.number, shirtColor);
            }
        }
        else
        {
            int count = Mathf.Min(zone.childCount, players.Count);
            for (int i = 0; i < count; i++)
            {
                Transform child = zone.GetChild(i);
                UpdatePlayerCardUI(child.gameObject, players[i].name, players[i].number, shirtColor);
            }
        }
    }

    private void UpdatePlayerCardUI(GameObject playerObj, string name, int number, Color shirtColor)
    {
        TMP_Text nameTxt = playerObj.transform.Find("Text_PlayerName")?.GetComponent<TMP_Text>();
        TMP_Text numTxt = playerObj.transform.Find("Image_Shirt/Text_Number")?.GetComponent<TMP_Text>();
        Image shirtImg = playerObj.transform.Find("Image_Shirt")?.GetComponent<Image>();

        if (!nameTxt) nameTxt = playerObj.GetComponentInChildren<TMP_Text>();

        if (nameTxt) nameTxt.text = name;
        if (numTxt) numTxt.text = number.ToString();
        if (shirtImg) shirtImg.color = shirtColor;
    }

    public void LoadEvents()
    {
        if (currentMatch?.events == null || eventsContent == null) return;

        for (int i = eventsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(eventsContent.GetChild(i).gameObject);
        }

        foreach (var evt in currentMatch.events)
        {
            GameObject evtItem = Instantiate(eventPrefab, eventsContent);
            TMP_Text[] textComponents = evtItem.GetComponentsInChildren<TMP_Text>();

            if (textComponents.Length >= 2)
            {
                textComponents[0].text = $"{evt.minute}'";
                textComponents[1].text = evt.description;
            }
            else if (textComponents.Length == 1)
            {
                textComponents[0].text = $"{evt.minute}' - {evt.description}";
            }
        }
    }

    private void AddChatMessage(ChatMessageData msg)
    {
        if (!chatContent || !chatItemPrefab) return;

        GameObject item = Instantiate(chatItemPrefab, chatContent);
        TMP_Text txt = item.GetComponent<TMP_Text>();
        if (txt) txt.text = $"<color={msg.colorHex}><b>[{msg.author}]</b></color> {msg.message}";

        if (chatContent.childCount > maxChatMessages) Destroy(chatContent.GetChild(0).gameObject);
        StartCoroutine(ForceScrollToBottom());
    }

    private IEnumerator ForceScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    public void ReceiveExternalChatMessage(string author, string message, string colorHex)
    {
        AddChatMessage(new ChatMessageData { author = author, message = message, colorHex = colorHex });
    }
}