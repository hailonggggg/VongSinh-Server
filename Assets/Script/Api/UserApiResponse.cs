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
    public int UserId;
    public string Email;
    public string FirstName;
    public string LastName;
    public bool Banned;
    public string UserName;
    public bool IsOnline;
    public string AvatarUrl;
    public DateTime LastOnline;
    public TimeSpan? BannedUntil;
}
