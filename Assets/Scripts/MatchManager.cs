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

    // Banque de messages fictifs pour la simulation
    private List<ChatMessageData> mockChatPool = new List<ChatMessageData>();

    private void Start()
    {
        InitMockData();
        UpdateHeaderUI();
        SpawnPlayers();
        LoadEvents();

        // Lancement du Chat
        StartCoroutine(SimulateLiveChat());
    }

    private void InitMockData()
    {
        currentMatch = new MatchData
        {
            scoreA = 0,
            scoreB = 0,
            currentMinute = 12,
            matchStatus = "IN_PROGRESS",
            teamA = new TeamData { name = "ARSENAL", coachName = "M. Arteta" },
            teamB = new TeamData { name = "MANCHESTER UTD", coachName = "M. Carrick" }
        };

        currentMatch.teamA.players.Add(new PlayerData { name = "RAMSDALE", number = 1, pitchPosition = new Vector2(0f, 0.35f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "WHITE", number = 4, pitchPosition = new Vector2(-0.35f, 0.15f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "SALIBA", number = 2, pitchPosition = new Vector2(-0.12f, 0.18f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "GABRIEL", number = 6, pitchPosition = new Vector2(0.12f, 0.18f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "ZINCHENKO", number = 35, pitchPosition = new Vector2(0.35f, 0.15f) });

        currentMatch.events.Add(new EventData { minute = 5, type = "YELLOW_CARD", description = "Carton jaune pour Xhaka" });

        // Messages simulés
        mockChatPool.Add(new ChatMessageData { author = "Gunner99", message = "COYG !! 🔴⚪", colorHex = "#FF4136" });
        mockChatPool.Add(new ChatMessageData { author = "RedDevil_Alex", message = "On a du mal en ce début de match...", colorHex = "#0074D9" });
        mockChatPool.Add(new ChatMessageData { author = "TacticFan", message = "La défense à 4 d'Arsenal est très haute.", colorHex = "#2ECC40" });
        mockChatPool.Add(new ChatMessageData { author = "SakaMagic", message = "Saka va débloquer la situation !", colorHex = "#FF851B" });
        mockChatPool.Add(new ChatMessageData { author = "UnitedWay", message = "Carrick doit ajuster le milieu.", colorHex = "#B10DC9" });
    }

    private void UpdateHeaderUI()
    {
        if (textNameA) textNameA.text = currentMatch.teamA.name;
        if (textNameB) textNameB.text = currentMatch.teamB.name;
        if (textScore) textScore.text = $"{currentMatch.scoreA} - {currentMatch.scoreB}";
        if (textTimer) textTimer.text = $"{currentMatch.currentMinute}'";
    }

    private void SpawnPlayers()
    {
        foreach (Transform child in zoneTeamA) Destroy(child.gameObject);
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

    private void LoadEvents()
    {
        foreach (Transform child in eventsContent) Destroy(child.gameObject);

        foreach (var evt in currentMatch.events)
        {
            GameObject evtItem = Instantiate(eventPrefab, eventsContent);
            TMP_Text timeTxt = evtItem.transform.Find("Text_Time")?.GetComponent<TMP_Text>();
            TMP_Text detailTxt = evtItem.transform.Find("Text_Detail")?.GetComponent<TMP_Text>();

            if (timeTxt) timeTxt.text = $"{evt.minute}'";
            if (detailTxt) detailTxt.text = evt.description;
        }
    }

    private IEnumerator SimulateLiveChat()
    {
        int index = 0;

        while (true)
        {
            // Attendre entre 2 et 4 secondes
            yield return new WaitForSeconds(Random.Range(2f, 4f));

            if (mockChatPool.Count > 0)
            {
                ChatMessageData msg = mockChatPool[index % mockChatPool.Count];
                AddChatMessage(msg);
                index++;
            }
        }
    }

    private void AddChatMessage(ChatMessageData msg)
    {
        if (!chatContent || !chatItemPrefab) return;

        GameObject item = Instantiate(chatItemPrefab, chatContent);
        TMP_Text txt = item.GetComponent<TMP_Text>();

        if (txt)
        {
            // Utilisation des Rich Text Tags pour colorer uniquement le pseudo
            txt.text = $"<color={msg.colorHex}><b>[{msg.author}]</b></color> {msg.message}";
        }

        // Forcer le défilement vers le bas
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect) chatScrollRect.verticalNormalizedPosition = 0f;
    }
}