using System;

[Serializable]
public class LoginResponse
{
    public bool Success;
    public string Message;
    public string FirstName;
    public string LastName; 
    public int RankPoint;
    public string PlayerAvatarUrl;
}
