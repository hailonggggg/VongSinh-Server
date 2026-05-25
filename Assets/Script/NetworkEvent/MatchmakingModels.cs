using System;

[System.Serializable]
public class ConfirmMatchRequest
{
    public bool Confirmed;
}

[System.Serializable]
public class MatchFoundResponse
{
    public string MatchId;
    public string PlayerName;
}

[System.Serializable]
public class MatchmakingUpdate
{
    public float WaitTime;
    public int QueuePosition;
    public int TotalInQueue;
}

[System.Serializable]
public class MatchmakingResponse
{
    public bool Success;
    public string Message;
}
