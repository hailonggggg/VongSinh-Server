using System;

[Serializable]
public class LoginApiResponse
{
    public bool Success = true;
    public string Message;
    public int Id;
    public string token;
    public string Email;
    public string Role;
    public string avatarUrl;
}
