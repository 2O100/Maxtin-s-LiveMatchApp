using UnityEngine;
using TMPro;

public class LauchManager : MonoBehaviour
{
    [Header("--- Input Fields ---")]
    [SerializeField] private TMP_InputField inputTwitch;
    [SerializeField] private TMP_InputField inputApiKey;
    [SerializeField] private TMP_InputField inputMatchId;

    [Header("--- UI References ---")]
    [SerializeField] private GameObject panelSetup;
    [SerializeField] private GameObject buttonConfig;
    [SerializeField] private TMP_Text textStatus;

    [Header("--- Managers ---")]
    [SerializeField] private APIManager apiManager;
    [SerializeField] private TwitchChatReceiver twitchManager;

    private void Start()
    {
        // Charger automatiquement les informations sauvegardées lors de la dernière session
        if (inputTwitch) inputTwitch.text = PlayerPrefs.GetString("Saved_TwitchChannel", "");
        if (inputApiKey) inputApiKey.text = PlayerPrefs.GetString("Saved_ApiKey", "");
        if (inputMatchId) inputMatchId.text = PlayerPrefs.GetString("Saved_MatchId", "");

        if (textStatus) textStatus.text = "";
    }

    public void OnStartClicked()
    {
        string twitchChannel = inputTwitch ? inputTwitch.text.Trim() : "";
        string apiKey = inputApiKey ? inputApiKey.text.Trim() : "";
        string matchId = inputMatchId ? inputMatchId.text.Trim() : "";

        // 1. Contrôle des saisies (Feedback visuel)
        if (string.IsNullOrEmpty(apiKey))
        {
            ShowStatus("Erreur : La clé API est requise.", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(matchId))
        {
            ShowStatus("Erreur : Veuillez saisir un ID de match.", Color.red);
            return;
        }

        // 2. Sauvegarde automatique dans le registre (PlayerPrefs)
        PlayerPrefs.SetString("Saved_TwitchChannel", twitchChannel);
        PlayerPrefs.SetString("Saved_ApiKey", apiKey);
        PlayerPrefs.SetString("Saved_MatchId", matchId);
        PlayerPrefs.Save();

        ShowStatus("Connexion en cours...", Color.yellow);

        // 3. Transmission de la clé + Match ID et lancement de l'API
        if (apiManager != null)
        {
            apiManager.apiKey = apiKey;
            apiManager.matchId = matchId;
            apiManager.RefreshMatchDataManually();
        }

            // 4. Déclenchement du chat Twitch
            if (twitchManager != null && !string.IsNullOrEmpty(twitchChannel))
        {
            twitchManager.ConnectToChannel(twitchChannel);
        }

        // 5. Masquer le panneau de setup et afficher le bouton Config
        if (panelSetup) panelSetup.SetActive(false);
        if (buttonConfig) buttonConfig.SetActive(true);
    }

    /// <summary>
    /// Affiche un message d'information ou d'erreur sur l'UI
    /// </summary>
    public void ShowStatus(string message, Color color)
    {
        if (textStatus != null)
        {
            textStatus.color = color;
            textStatus.text = message;
        }
    }

    /// <summary>
    /// Bascule entre le mode Plein Écran et le mode Fenêtré
    /// </summary>
    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    /// <summary>
    /// Ferme l'application (fonctionne sur le .exe et dans l'éditeur Unity)
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("Fermeture de l'application...");

        Application.Quit();

        // Permet de tester la fermeture directement dans l'éditeur Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}