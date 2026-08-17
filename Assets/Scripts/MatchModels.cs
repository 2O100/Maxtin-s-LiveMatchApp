using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public int id;
    public string name;
    public int number;
    public string position;
    public Vector2 pitchPosition;
}

[Serializable]
public class EventData
{
    public int minute;
    public string type;
    public string teamName;
    public string description;
}

[Serializable]
public class ChatMessageData
{
    public string author;
    public string message;
    public string colorHex;
}

[Serializable]
public class TeamData
{
    public string name;
    public Color teamColor = Color.red;
    public string coachName;
    public List<PlayerData> players = new List<PlayerData>();
    public string logoUrl;
}

[Serializable]
public class MatchData
{
    public TeamData teamA;
    public TeamData teamB;
    public int scoreA;
    public int scoreB;
    public int currentMinute;
    public string matchStatus;
    public List<EventData> events = new List<EventData>();
    public List<ChatMessageData> chatMessages = new List<ChatMessageData>();
}