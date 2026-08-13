using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchManager : MonoBehaviour
{
    [Header("--- Modèles de Données ---")]
    public MatchData currentMatch;

    [Header("--- Références UI Score & Infos ---")]
    [SerializeField] private TMP_Text textNameA;
    [SerializeField] private TMP_Text textNameB;
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private TMP_Text textTimer;

    [Header("--- Références Terrain & Prefabs ---")]
    [SerializeField] private RectTransform zoneTeamA;
    [SerializeField] private GameObject playerPrefab;

    [Header("--- Références Fil d'Événements ---")]
    [SerializeField] private Transform eventsContent;
    [SerializeField] private GameObject eventPrefab;

    [Header("--- Références Chat en Direct ---")]
    [SerializeField] private Transform chatContent;
    [SerializeField] private GameObject chatItemPrefab;
    [SerializeField] private ScrollRect chatScrollRect;

    private void Start()
    {
        // Au lancement, l'interface attend les premières données envoyées par l'APIManager
    }

    /// <summary>
    /// Reçoit les données réelles de l'APIManager et met à jour toute l'interface en direct
    /// </summary>
    public void ApplyRealMatchData(MatchData realData)
    {
        if (realData == null) return;

        currentMatch = realData;

        UpdateHeaderUI();
        SpawnPlayers();
        LoadEvents();
    }

    /// <summary>
    /// Met à jour les noms d'équipes, le score et la minute de jeu
    /// </summary>
    public void UpdateHeaderUI()
    {
        if (currentMatch == null) return;

        if (textNameA && currentMatch.teamA != null) textNameA.text = currentMatch.teamA.name;
        if (textNameB && currentMatch.teamB != null) textNameB.text = currentMatch.teamB.name;
        if (textScore) textScore.text = $"{currentMatch.scoreA} - {currentMatch.scoreB}";
        if (textTimer) textTimer.text = $"{currentMatch.currentMinute}'";
    }

    /// <summary>
    /// Génère la composition d'équipe sur le terrain
    /// </summary>
    public void SpawnPlayers()
    {
        if (currentMatch?.teamA?.players == null) return;

        // Nettoyage des anciens joueurs sur le terrain
        foreach (Transform child in zoneTeamA)
        {
            Destroy(child.gameObject);
        }

        Vector2 zoneSize = zoneTeamA.rect.size;

        foreach (var player in currentMatch.teamA.players)
        {
            GameObject pItem = Instantiate(playerPrefab, zoneTeamA);
            Vector2 localPos = new Vector2(player.pitchPosition.x * zoneSize.x, player.pitchPosition.y * zoneSize.y);
            pItem.GetComponent<RectTransform>().anchoredPosition = localPos;

            TMP_Text nameTxt = pItem.transform.Find("Text_PlayerName")?.GetComponent<TMP_Text>();
            TMP_Text numTxt = pItem.transform.Find("Image_Shirt/Text_Number")?.GetComponent<TMP_Text>();

            if (nameTxt) nameTxt.text = player.name;
            if (numTxt) numTxt.text = player.number.ToString();
        }
    }

    /// <summary>
    /// Charge le fil des événements (buts, cartons, changements)
    /// </summary>
    public void LoadEvents()
    {
        if (currentMatch?.events == null) return;

        // Nettoyage de l'ancien fil d'événements
        foreach (Transform child in eventsContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var evt in currentMatch.events)
        {
            GameObject evtItem = Instantiate(eventPrefab, eventsContent);
            TMP_Text timeTxt = evtItem.transform.Find("Text_Time")?.GetComponent<TMP_Text>();
            TMP_Text detailTxt = evtItem.transform.Find("Text_Detail")?.GetComponent<TMP_Text>();

            if (timeTxt) timeTxt.text = $"{evt.minute}'";
            if (detailTxt) detailTxt.text = evt.description;
        }
    }

    /// <summary>
    /// Ajoute un message reçu du chat dans la liste
    /// </summary>
    private void AddChatMessage(ChatMessageData msg)
    {
        if (!chatContent || !chatItemPrefab) return;

        GameObject item = Instantiate(chatItemPrefab, chatContent);
        TMP_Text txt = item.GetComponent<TMP_Text>();

        if (txt)
        {
            txt.text = $"<color={msg.colorHex}><b>[{msg.author}]</b></color> {msg.message}";
        }

        // Forcer le scroll automatique vers le bas
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Méthode publique appelée par le TwitchChatReceiver
    /// </summary>
    public void ReceiveExternalChatMessage(string author, string message, string colorHex)
    {
        AddChatMessage(new ChatMessageData
        {
            author = author,
            message = message,
            colorHex = colorHex
        });
    }
}