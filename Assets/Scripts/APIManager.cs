using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    [Header("--- Configuration API-Football ---")]
    [Tooltip("Clé d'API récupérée sur dashboard.api-football.com")]
    public string apiKey = "";

    [Tooltip("ID du match à suivre (ex: 1035088)")]
    public string matchId = "";

    [Header("--- Références ---")]
    [SerializeField] private MatchManager matchManager;

    // Positions par défaut (4-3-3) pour placer les cartes proprement au premier chargement
    private readonly Vector2[] defaultPositions = {
        new Vector2(0.50f, 0.05f), new Vector2(0.85f, 0.20f), new Vector2(0.60f, 0.18f),
        new Vector2(0.40f, 0.18f), new Vector2(0.15f, 0.20f), new Vector2(0.50f, 0.35f),
        new Vector2(0.75f, 0.45f), new Vector2(0.25f, 0.45f), new Vector2(0.85f, 0.70f),
        new Vector2(0.50f, 0.80f), new Vector2(0.15f, 0.70f)
    };

    /// <summary>
    /// À LIER AU BOUTON "ACTUALISER" DU CLIENT SUR L'INTERFACE
    /// </summary>
    public void RefreshMatchDataManually()
    {
        if (!matchManager) matchManager = GetComponent<MatchManager>();

        Debug.Log("[API] Rafraîchissement manuel demandé...");
        StartCoroutine(GetMatchData());
    }

    private IEnumerator GetMatchData()
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(matchId))
        {
            Debug.LogWarning("[API] Clé API ou Match ID manquant !");
            yield break;
        }

        // Endpoint API-Football (v3)
        string url = $"https://v3.football.api-sports.io/fixtures?id={matchId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Header obligatoire
            webRequest.SetRequestHeader("x-apisports-key", apiKey);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"[API] Données reçues pour le match #{matchId}");

                ApiFootballResponse apiData = JsonUtility.FromJson<ApiFootballResponse>(jsonResponse);

                if (apiData != null && apiData.response != null && apiData.response.Length > 0)
                {
                    ConvertAndApplyData(apiData.response[0]);
                }
                else
                {
                    Debug.LogWarning("[API] Match introuvable ou quota API dépassé.");
                }
            }
            else
            {
                Debug.LogError($"[API] Erreur HTTP ({webRequest.responseCode}) : {webRequest.error}");
            }
        }
    }

    private void ConvertAndApplyData(MatchDataApi apiMatch)
    {
        if (matchManager.currentMatch == null)
            matchManager.currentMatch = new MatchData();

        if (matchManager.currentMatch.teamA == null) matchManager.currentMatch.teamA = new TeamData();
        if (matchManager.currentMatch.teamB == null) matchManager.currentMatch.teamB = new TeamData();

        // 1. Noms, Scores & Logos
        matchManager.currentMatch.teamA.name = apiMatch.teams.home.name.ToUpper();
        matchManager.currentMatch.teamB.name = apiMatch.teams.away.name.ToUpper();

        matchManager.currentMatch.teamA.logoUrl = apiMatch.teams.home.logo;
        matchManager.currentMatch.teamB.logoUrl = apiMatch.teams.away.logo;

        matchManager.currentMatch.scoreA = apiMatch.goals.home;
        matchManager.currentMatch.scoreB = apiMatch.goals.away;

        matchManager.currentMatch.currentMinute = apiMatch.fixture.status.elapsed;
        matchManager.currentMatch.matchStatus = apiMatch.fixture.status.@short;

        // 2. Compositions (Lineups) & Couleurs Maillots
        if (apiMatch.lineups != null && apiMatch.lineups.Length >= 2)
        {
            matchManager.currentMatch.teamA.coachName = apiMatch.lineups[0].coach?.name;
            matchManager.currentMatch.teamB.coachName = apiMatch.lineups[1].coach?.name;

            // Extraction couleur officielle Équipe A
            if (apiMatch.lineups[0].team?.colors?.player != null)
            {
                string hexA = apiMatch.lineups[0].team.colors.player.primary;
                if (!string.IsNullOrEmpty(hexA))
                {
                    if (!hexA.StartsWith("#")) hexA = "#" + hexA;
                    if (ColorUtility.TryParseHtmlString(hexA, out Color colorA))
                    {
                        matchManager.colorTeamA = colorA;
                    }
                }
            }

            // Extraction couleur officielle Équipe B
            if (apiMatch.lineups[1].team?.colors?.player != null)
            {
                string hexB = apiMatch.lineups[1].team.colors.player.primary;
                if (!string.IsNullOrEmpty(hexB))
                {
                    if (!hexB.StartsWith("#")) hexB = "#" + hexB;
                    if (ColorUtility.TryParseHtmlString(hexB, out Color colorB))
                    {
                        matchManager.colorTeamB = colorB;
                    }
                }
            }

            UpdateTeamPlayers(matchManager.currentMatch.teamA, apiMatch.lineups[0].startXI);
            UpdateTeamPlayers(matchManager.currentMatch.teamB, apiMatch.lineups[1].startXI);
        }

        // 3. Événements du match (Events)
        matchManager.currentMatch.events.Clear();
        if (apiMatch.events != null)
        {
            foreach (var ev in apiMatch.events)
            {
                string pName = ev.player != null ? ev.player.name : "";
                string details = string.IsNullOrEmpty(pName) ? ev.detail : $"{pName} ({ev.detail})";

                matchManager.currentMatch.events.Add(new EventData
                {
                    minute = ev.time.elapsed,
                    teamName = ev.team.name,
                    type = ev.type,
                    description = $"{ev.type} : {details}"
                });
            }

            // Événement le plus récent en haut
            matchManager.currentMatch.events.Reverse();
        }

        // 4. Mise à jour de l'UI
        matchManager.UpdateHeaderUI();
        matchManager.SpawnPlayers();
        matchManager.LoadEvents();
    }

    private void UpdateTeamPlayers(TeamData teamData, PlayerWrapper[] startXI)
    {
        if (startXI == null) return;

        bool isFirstLoad = teamData.players.Count == 0;

        for (int i = 0; i < startXI.Length && i < 11; i++)
        {
            string pName = startXI[i].player.name;
            int pNum = startXI[i].player.number;

            if (isFirstLoad)
            {
                teamData.players.Add(new PlayerData
                {
                    name = pName,
                    number = pNum,
                    position = startXI[i].player.pos,
                    pitchPosition = defaultPositions[i]
                });
            }
            else if (i < teamData.players.Count)
            {
                // Maintient la position drag & drop tout en actualisant le joueur
                teamData.players[i].name = pName;
                teamData.players[i].number = pNum;
            }
        }
    }
}

// ============================================================================
// --- Structure DTO (JSON Mapping v3.football.api-sports.io) ---
// ============================================================================
[Serializable] public class ApiFootballResponse { public MatchDataApi[] response; }
[Serializable] public class MatchDataApi { public FixtureInfo fixture; public TeamsInfo teams; public GoalsInfo goals; public LineupInfo[] lineups; public EventInfo[] events; }
[Serializable] public class FixtureInfo { public StatusInfo status; }
[Serializable] public class StatusInfo { public string @short; public int elapsed; }
[Serializable] public class TeamsInfo { public TeamDetail home; public TeamDetail away; }
[Serializable] public class TeamDetail { public string name; public string logo; }
[Serializable] public class GoalsInfo { public int home; public int away; }

[Serializable]
public class LineupInfo
{
    public LineupTeamDetail team;
    public CoachInfo coach;
    public PlayerWrapper[] startXI;
}

[Serializable]
public class LineupTeamDetail
{
    public string name;
    public TeamColors colors;
}

[Serializable]
public class TeamColors
{
    public PlayerColors player;
    public PlayerColors goalkeeper;
}

[Serializable]
public class PlayerColors
{
    public string primary;
    public string number;
    public string border;
}

[Serializable] public class CoachInfo { public string name; }
[Serializable] public class PlayerWrapper { public PlayerDetail player; }
[Serializable] public class PlayerDetail { public string name; public int number; public string pos; }
[Serializable] public class EventInfo { public TimeInfo time; public TeamDetail team; public PlayerDetail player; public string type; public string detail; }
[Serializable] public class TimeInfo { public int elapsed; }