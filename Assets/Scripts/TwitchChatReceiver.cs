using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TwitchChatReceiver : MonoBehaviour
{
    [Header("--- Configuration Twitch ---")]
    [Tooltip("Nom exact de la chaîne Twitch (en minuscules) ex: gotaga, kameto, otplol")]
    public string channelName = "gotaga";

    [Header("--- Références ---")]
    [SerializeField] private MatchManager matchManager;

    private TcpClient twitchClient;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread readThread;
    private bool isConnected = false;

    // Palette de couleurs pour varier l'affichage des pseudos
    private readonly string[] userColors = { "#FF4136", "#0074D9", "#2ECC40", "#FF851B", "#B10DC9", "#FFDC00", "#7FDBFF" };

    // File d'attente thread-safe pour transmettre les messages au Main Thread de Unity
    private readonly Queue<ChatMessageData> incomingQueue = new Queue<ChatMessageData>();

    public void ConnectToChannel(string targetChannel)
    {
        if (string.IsNullOrEmpty(targetChannel)) return;

        Disconnect();

        channelName = targetChannel.Trim().Replace("#", "").ToLower();

        if (!matchManager) matchManager = GetComponent<MatchManager>();

        ConnectToTwitch();
    }

    public void StartConnection()
    {
        ConnectToChannel(channelName);
    }

    private void Update()
    {
        lock (incomingQueue)
        {
            while (incomingQueue.Count > 0)
            {
                ChatMessageData msg = incomingQueue.Dequeue();
                if (matchManager != null)
                {
                    matchManager.ReceiveExternalChatMessage(msg.author, msg.message, msg.colorHex);
                }
            }
        }
    }

    private void ConnectToTwitch()
    {
        try
        {
            twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
            reader = new StreamReader(twitchClient.GetStream());
            writer = new StreamWriter(twitchClient.GetStream());

            string anonUser = "justinfan" + UnityEngine.Random.Range(10000, 99999);

            writer.WriteLine("PASS oauth:none");
            writer.WriteLine("NICK " + anonUser);
            writer.WriteLine("USER " + anonUser + " 8 * :" + anonUser);

            // 🟢 Demande à Twitch de nous envoyer les tags d'émotes
            writer.WriteLine("CAP REQ :twitch.tv/tags");

            writer.WriteLine("JOIN #" + channelName);
            writer.Flush();

            isConnected = true;

            readThread = new Thread(ReadTwitchChat) { IsBackground = true };
            readThread.Start();

            Debug.Log($"[Twitch] Connecté avec succès au chat de #{channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Twitch] Erreur de connexion : {e.Message}");
        }
    }

    private void ReadTwitchChat()
    {
        while (isConnected && twitchClient != null && twitchClient.Connected)
        {
            try
            {
                string rawLine = reader.ReadLine();
                if (rawLine != null)
                {
                    ParseTwitchLine(rawLine);
                }
            }
            catch
            {
                break;
            }
        }
    }

    private void ParseTwitchLine(string rawLine)
    {
        if (rawLine.StartsWith("PING"))
        {
            writer.WriteLine("PONG :tmi.twitch.tv");
            writer.Flush();
            return;
        }

        if (rawLine.Contains("PRIVMSG"))
        {
            try
            {
                string tags = "";
                string lineWithoutTags = rawLine;

                // Si la ligne contient des tags Twitch (commence par @)
                if (rawLine.StartsWith("@"))
                {
                    int spaceIdx = rawLine.IndexOf(" ");
                    tags = rawLine.Substring(1, spaceIdx - 1);
                    lineWithoutTags = rawLine.Substring(spaceIdx + 1);
                }

                int authorEnd = lineWithoutTags.IndexOf("!");
                string author = lineWithoutTags.Substring(1, authorEnd - 1);

                int msgStart = lineWithoutTags.IndexOf(" :", authorEnd);
                string message = lineWithoutTags.Substring(msgStart + 2);

                // Nettoyage \u0001 /me
                if (message.StartsWith("\u0001ACTION ") && message.EndsWith("\u0001"))
                {
                    message = "*" + message.Substring(8, message.Length - 9) + "*";
                }
                else
                {
                    message = message.Replace("\u0001", "");
                }

                // Traitement et conversion des émotes
                message = ParseEmotesFromTags(tags, message);

                string color = userColors[Mathf.Abs(author.GetHashCode()) % userColors.Length];

                lock (incomingQueue)
                {
                    incomingQueue.Enqueue(new ChatMessageData
                    {
                        author = author,
                        message = message,
                        colorHex = color
                    });
                }
            }
            catch
            {
                // Ignorer les lignes mal formées
            }
        }
    }

    private string ParseEmotesFromTags(string tags, string message)
    {
        if (string.IsNullOrEmpty(tags)) return message;

        string[] tagList = tags.Split(';');
        foreach (string tag in tagList)
        {
            if (tag.StartsWith("emotes="))
            {
                string emoteData = tag.Substring(7);
                if (string.IsNullOrEmpty(emoteData)) break;

                // Format: emoteId:start-end,start-end/emoteId2:start-end
                string[] emoteEntries = emoteData.Split('/');
                foreach (string entry in emoteEntries)
                {
                    string[] parts = entry.Split(':');
                    if (parts.Length < 2) continue;

                    string emoteId = parts[0];
                    string[] positions = parts[1].Split(',');

                    if (positions.Length > 0)
                    {
                        string[] range = positions[0].Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                        {
                            if (start < message.Length && end < message.Length && end >= start)
                            {
                                string emoteWord = message.Substring(start, end - start + 1);

                                // Demander le téléchargement de l'émote
                                if (TwitchEmoteManager.Instance != null)
                                {
                                    TwitchEmoteManager.Instance.RequestEmote(emoteId);
                                }

                                // Remplacer le texte par la balise sprite TMP
                                message = message.Replace(emoteWord, $"<sprite name=\"{emoteId}\">");
                            }
                        }
                    }
                }
                break;
            }
        }

        return message;
    }

    private void Disconnect()
    {
        isConnected = false;
        try
        {
            if (reader != null) reader.Close();
            if (writer != null) writer.Close();
            if (twitchClient != null) twitchClient.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Twitch] Fermeture de la connexion : {e.Message}");
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}