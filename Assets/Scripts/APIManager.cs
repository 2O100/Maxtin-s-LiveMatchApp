using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    [Header("--- Configuration Football-Data ---")]
    [Tooltip("Clé API (Token) reçue par mail depuis football-data.org")]
    public string apiKey = "";

    [Tooltip("ID du match à suivre (ex: 435987)")]
    public string matchId = "435987";

    [Tooltip("Intervalle de rafraîchissement en secondes (10s = 6 req/min, respecte la limite de 10 req/min)")]
    public float pollInterval = 10f;

    [Header("--- Références ---")]
    [SerializeField] private MatchManager matchManager;

    private Coroutine pollingCoroutine;

    /// <summary>
    /// Démarre le cycle de requêtes automatique vers l'API.
    /// Appelé par le LauncherManager une fois les champs validés.
    /// </summary>
    public void StartPolling()
    {
        if (!matchManager) matchManager = GetComponent<MatchManager>();

        // Arrêter une ancienne boucle si elle tournait déjà
        if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);

        pollingCoroutine = StartCoroutine(FetchMatchDataRoutine());
    }

    private IEnumerator FetchMatchDataRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(GetMatchData());
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator GetMatchData()
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(matchId))
        {
            Debug.LogWarning("[API] Clé API ou Match ID manquant !");
            yield break;
        }

        string url = $"https://api.football-data.org/v4/matches/{matchId}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // En-tête obligatoire pour Football-Data.org
            webRequest.SetRequestHeader("X-Auth-Token", apiKey);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"[API] Données reçues pour le match #{matchId}");

                // Décodage du JSON
                FootballDataResponse apiData = JsonUtility.FromJson<FootballDataResponse>(jsonResponse);

                if (apiData != null && matchManager != null)
                {
                    ConvertAndApplyData(apiData);
                }
            }
            else
            {
                Debug.LogWarning($"[API] Erreur HTTP ({webRequest.responseCode}) : {webRequest.error}");
            }
        }
    }

    private void ConvertAndApplyData(FootballDataResponse apiData)
    {
        if (matchManager.currentMatch == null)
        {
            matchManager.currentMatch = new MatchData();
        }

        // 1. Initialisation des équipes si nécessaire
        if (matchManager.currentMatch.teamA == null) matchManager.currentMatch.teamA = new TeamData();
        if (matchManager.currentMatch.teamB == null) matchManager.currentMatch.teamB = new TeamData();

        // 2. Noms d'équipes (Utilisation du nom court ou nom officiel)
        if (apiData.homeTeam != null)
        {
            matchManager.currentMatch.teamA.name = !string.IsNullOrEmpty(apiData.homeTeam.shortName)
                ? apiData.homeTeam.shortName.ToUpper()
                : apiData.homeTeam.name.ToUpper();
        }

        if (apiData.awayTeam != null)
        {
            matchManager.currentMatch.teamB.name = !string.IsNullOrEmpty(apiData.awayTeam.shortName)
                ? apiData.awayTeam.shortName.ToUpper()
                : apiData.awayTeam.name.ToUpper();
        }

        // 3. Score
        if (apiData.score != null && apiData.score.fullTime != null)
        {
            matchManager.currentMatch.scoreA = apiData.score.fullTime.home;
            matchManager.currentMatch.scoreB = apiData.score.fullTime.away;
        }

        // 4. Minute & Statut du Match
        matchManager.currentMatch.currentMinute = apiData.minute;
        matchManager.currentMatch.matchStatus = apiData.status;

        // 5. Rafraîchissement de l'interface graphique
        matchManager.UpdateHeaderUI();
        matchManager.SpawnPlayers();
        matchManager.LoadEvents();
    }
}

// ============================================================================
// --- Structure DTO pour mapper le JSON natif de Football-Data.org (v4) ---
// ============================================================================

[Serializable]
public class FootballDataResponse
{
    public int id;
    public string status; // IN_PLAY, PAUSED, FINISHED, TIMED...
    public int minute;
    public FDTeam homeTeam;
    public FDTeam awayTeam;
    public FDScore score;
}

[Serializable]
public class FDTeam
{
    public int id;
    public string name;
    public string shortName;
    public string tla;
}

[Serializable]
public class FDScore
{
    public string winner;
    public FDFullTime fullTime;
}

[Serializable]
public class FDFullTime
{
    public int home;
    public int away;
}