using System;

[Serializable]
public class UserApiResponse
{
    // public bool Success = true;
    // public string Message;
    public UserApiUserData[] Data;
}

[Serializable]
public class UserApiUserData
{
    public int userId;
    public string email;
    public string firstName;
    public string lastName;
    public bool banned;
    public TimeSpan? bannedUntil;
}
