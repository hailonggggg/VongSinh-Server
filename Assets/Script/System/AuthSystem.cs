using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

public class AuthSystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        base.HandlePackage(client, messageType, payload);
        switch (messageType)
        {
            case Command.RequestLogin:
                LoginRequest request = JsonUtility.FromJson<LoginRequest>(payload);
                _ = Login(client, request);
                break;
            case Command.RegisterRequest:
                RegisterRequest registerRequest = JsonUtility.FromJson<RegisterRequest>(payload);
                _ = Register(client, registerRequest);
                break;
            case Command.LoginWithFakeAccount:
                LoginWithFakeAccount(client);
                break;
            case Command.Logout:
                Logout(client);
                break;
            default:
                break;
        }
    }

    private void Logout(Client client)
    {
        client.Player = null;
        ServerNetwork.Instance.SendToClient(client, Service.LoadLoginScene());
    }

    private void LoginWithFakeAccount(Client client)
    {
        client.Player = new Player
        {
            Name = $"FakeUser{UnityEngine.Random.Range(1000, 9999)}"
        };
        ServerNetwork.Instance.SendToClient(client, Service.SendLoginResponse(client.Player.Name, ""), Service.LoadLobbyScene());
    }


    private async Task Register(Client client, RegisterRequest registerRequest)
    {
        try
        {
            if (!IsValidEmail(registerRequest.Email))
            {
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, "Email không hợp lệ"));
                return;
            }
            RegisterApiResponse registerApiResponse = await ApiService.Register(registerRequest);
            if (registerApiResponse == null)
            {
                Debug.LogWarning("RegisterApiResponse is null");
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, "Tạo tài khoản thất bại"));
                return;
            }
            if (!registerApiResponse.Success)
            {
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, registerApiResponse.Message));
                return;
            }
            ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(true, "Tạo tài khoản thành công, vui lòng đăng nhập để chơi"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[AUTH] Unexpected register error. Error={e}");
        }
    }

    bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
    }


    private async Task Login(Client client, LoginRequest request)
    {
        try
        {
            LoginApiResponse apiResponse = await ApiService.Login(request);
            if (!apiResponse.Success)
            {
                Debug.LogWarning($"[AUTH] Login failed for '{request?.Email}'. Message={apiResponse.Message}");
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification(apiResponse.Message ?? "Đăng nhập thất bại."));
                return;
            }
            client.Token = apiResponse.token;
            UserApiUserData[] userApiResponse = await ApiService.GetUser(client, request.Email);


            if (userApiResponse == null || userApiResponse.Length == 0)
            {
                Debug.LogWarning($"[AUTH] Get user returned no data for '{request?.Email}'.");
                return;
            }

            UserApiUserData user = userApiResponse[0];
            client.Player = new Player
            {
                Username = request.Email,
                Name = user.lastName
            };

            AnnouncementResponse[] announcements = await ApiService.GetAllAnnouncement(client);

            if (announcements == null)
            {
                Debug.LogError("[SERVER] Announcements is NULL");
            }
            else
            {
                Debug.Log($"[SERVER] Got {announcements.Length} announcements");
            }

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendLoginResponse(user.lastName, apiResponse.avatarUrl),
                Service.SendAnnouncementResponse(announcements),
                Service.LoadLobbyScene());
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AUTH] Unexpected login error for '{request?.Email}'. Error={exception}");
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Đăng nhập thất bại."));
        }
    }
}
