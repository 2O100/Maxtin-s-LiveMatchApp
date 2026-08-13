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

    public void StartConnection()
    {
        if (!matchManager) matchManager = GetComponent<MatchManager>();
        ConnectToTwitch();
    }

    private void Update()
    {
        // Consommation des messages reçus sur le thread principal d'Unity
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

            // Identifiant anonyme autorisé par Twitch (justinfan + chiffres)
            string anonUser = "justinfan" + UnityEngine.Random.Range(10000, 99999);

            writer.WriteLine("PASS oauth:none");
            writer.WriteLine("NICK " + anonUser);
            writer.WriteLine("USER " + anonUser + " 8 * :" + anonUser);
            writer.WriteLine("JOIN #" + channelName.ToLower());
            writer.Flush();

            isConnected = true;

            // Lecture sur un Thread séparé pour ne jamais faire ramer le jeu
            readThread = new Thread(ReadTwitchChat) { IsBackground = true };
            readThread.Start();

            Debug.Log($"[Twitch] Connecté au chat de #{channelName.ToLower()}");
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
        // Répondre au Ping automatique de Twitch pour maintenir la connexion
        if (rawLine.StartsWith("PING"))
        {
            writer.WriteLine("PONG :tmi.twitch.tv");
            writer.Flush();
            return;
        }

        // Format IRC standard : :pseudo!pseudo@pseudo.tmi.twitch.tv PRIVMSG #chaine :message
        if (rawLine.Contains("PRIVMSG"))
        {
            try
            {
                int authorEnd = rawLine.IndexOf("!");
                string author = rawLine.Substring(1, authorEnd - 1);

                int msgStart = rawLine.IndexOf(" :", authorEnd);
                string message = rawLine.Substring(msgStart + 2);

                // Couleur attribuée au pseudo
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

    private void OnDestroy()
    {
        isConnected = false;
        if (twitchClient != null) twitchClient.Close();
    }
}