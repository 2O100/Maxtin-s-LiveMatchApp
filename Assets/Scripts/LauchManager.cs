using UnityEngine;
using TMPro; // À remplacer par UnityEngine.UI si tu n'utilises pas TextMeshPro

public class LauncherManager : MonoBehaviour
{
    [Header("--- Champs de Saisie UI ---")]
    [SerializeField] private TMP_InputField inputTwitchChannel;
    [SerializeField] private TMP_InputField inputApiKey;
    [SerializeField] private TMP_InputField inputMatchId;

    [Header("--- Interface & Gestionnaires ---")]
    [SerializeField] private GameObject setupPanel;
    [SerializeField] private TwitchChatReceiver twitchReceiver;
    [SerializeField] private APIManager apiManager;

    private void Start()
    {
        // Charger automatiquement les dernières valeurs saisies
        if (inputTwitchChannel != null && PlayerPrefs.HasKey("PREF_TWITCH_CHANNEL"))
            inputTwitchChannel.text = PlayerPrefs.GetString("PREF_TWITCH_CHANNEL");

        if (inputApiKey != null && PlayerPrefs.HasKey("PREF_API_KEY"))
            inputApiKey.text = PlayerPrefs.GetString("PREF_API_KEY");

        if (inputMatchId != null && PlayerPrefs.HasKey("PREF_MATCH_ID"))
            inputMatchId.text = PlayerPrefs.GetString("PREF_MATCH_ID");
    }

    /// <summary>
    /// Méthode reliée au bouton "Démarrer" de ton UI.
    /// </summary>
    public void OnStartClicked()
    {
        // 1. Déclaration et récupération des chaînes de caractères depuis les champs UI
        string channel = inputTwitchChannel != null ? inputTwitchChannel.text.Trim() : "";
        string key = inputApiKey != null ? inputApiKey.text.Trim() : "";
        string mId = inputMatchId != null ? inputMatchId.text.Trim() : "";

        // 2. Vérification que rien n'est vide
        if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(mId))
        {
            Debug.LogWarning("[Launcher] Merci de remplir tous les champs !");
            return;
        }

        // 3. Sauvegarde locale dans PlayerPrefs
        PlayerPrefs.SetString("PREF_TWITCH_CHANNEL", channel);
        PlayerPrefs.SetString("PREF_API_KEY", key);
        PlayerPrefs.SetString("PREF_MATCH_ID", mId);
        PlayerPrefs.Save();

        // 4. Transmission à TwitchReceiver
        if (twitchReceiver != null)
        {
            twitchReceiver.channelName = channel;
            twitchReceiver.StartConnection();
        }

        // 5. Transmission à APIManager et lancement des requêtes
        if (apiManager != null)
        {
            apiManager.apiKey = key;     // Transmet la clé API
            apiManager.matchId = mId;    // Transmet l'ID du match
            apiManager.StartPolling();
        }

        // 6. Fermeture du panneau de configuration
        if (setupPanel != null)
        {
            setupPanel.SetActive(false);
        }
    }
}