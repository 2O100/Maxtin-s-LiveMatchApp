using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private RectTransform zoneTeamB;
    [SerializeField] private GameObject playerPrefab;

    [Header("--- Références Fil d'Événements ---")]
    [SerializeField] private Transform eventsContent;
    [SerializeField] private GameObject eventPrefab;

    private void Start()
    {
        // 1. Initialiser des données de test fictives
        InitMockData();

        // 2. Mettre à jour le tableau d'affichage
        UpdateHeaderUI();

        // 3. Spawner les joueurs sur le terrain
        SpawnPlayers();

        // 4. Charger les événements initiaux
        LoadEvents();
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

        // Exemple de joueurs pour l'équipe A (Positions relatives sur leur zone X: -0.4 à 0.4, Y: -0.4 à 0.4)
        currentMatch.teamA.players.Add(new PlayerData { name = "RAMSDALE", number = 1, pitchPosition = new Vector2(0f, 0.35f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "WHITE", number = 4, pitchPosition = new Vector2(-0.35f, 0.15f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "SALIBA", number = 2, pitchPosition = new Vector2(-0.12f, 0.18f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "GABRIEL", number = 6, pitchPosition = new Vector2(0.12f, 0.18f) });
        currentMatch.teamA.players.Add(new PlayerData { name = "ZINCHENKO", number = 35, pitchPosition = new Vector2(0.35f, 0.15f) });

        // Événement fictif
        currentMatch.events.Add(new EventData { minute = 5, type = "YELLOW_CARD", description = "Carton jaune pour Xhaka" });
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
        // Nettoyage préalable si besoin
        foreach (Transform child in zoneTeamA) Destroy(child.gameObject);

        // Récupérer la taille de la zone terrain
        Vector2 zoneSize = zoneTeamA.rect.size;

        foreach (var player in currentMatch.teamA.players)
        {
            GameObject pItem = Instantiate(playerPrefab, zoneTeamA);
            
            // Calcul de la position locale sur le terrain
            Vector2 localPos = new Vector2(
                player.pitchPosition.x * zoneSize.x,
                player.pitchPosition.y * zoneSize.y
            );
            
            pItem.GetComponent<RectTransform>().anchoredPosition = localPos;

            // Mise à jour des textes du Prefab Joueur
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
}