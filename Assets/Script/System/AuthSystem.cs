using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fusion;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

public class AuthSystem : BaseSystem
{
    public override void HandlePackage(Client client, Command messageType, string payload)
    {
        base.HandlePackage(client, messageType, payload);
        switch (messageType)
        {
            case Command.RequestLogin:
                LoginRequest request = JsonConvert.DeserializeObject<LoginRequest>(payload);
                _ = Login(client, request);
                break;
            case Command.RegisterRequest:
                RegisterRequest registerRequest = JsonConvert.DeserializeObject<RegisterRequest>(payload);
                _ = Register(client, registerRequest);
                break;
            case Command.LoginWithFakeAccount:
                LoginWithFakeAccount(client);
                break;
            case Command.Logout:
                Logout(client);
                break;
            case Command.ForgetPasswordRequest:
                _ = HandleForgetPasswordRequest(client, payload);
                break;
            case Command.RequestUpdateProfile:
                _ = HandleUpdateProfile(client, payload);
                break;
            default:
                break;
        }
    }

    private async Task HandleUpdateProfile(Client client, string payload)
    {
        UpdateProfileRequest request = JsonConvert.DeserializeObject<UpdateProfileRequest>(payload);
        if (string.IsNullOrEmpty(request.FirstName)
        || string.IsNullOrEmpty(request.LastName)
        || string.IsNullOrEmpty(request.NewPassword)
        || string.IsNullOrEmpty(request.CurrentPassword)
        || string.IsNullOrEmpty(request.ConfirmNewPassword))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Vui lòng điền đủ thông tin!"));
            return;
        }
        if (!request.ConfirmNewPassword.Equals(request.NewPassword))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Mật khẩu xác nhận không khớp với mật khẩu mới!"));
            return;
        }
        if (!request.CurrentPassword.Equals(client.Password))
        {
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Mật khẩu hiện tại không khớp!"));
            return;
        }
        await ApiService.UpdateProfile(client, request.FirstName, request.LastName, request.NewPassword);
    }

    private async Task HandleForgetPasswordRequest(Client client, string payload)
    {
        var request = JsonConvert.DeserializeObject<ForgetPasswordRequest>(payload);
        if (!IsValidEmail(request.Email))
        {
            ServerNetwork.Instance.SendToClient(
                client,
                Service.ShowNotification("Định dạng email sai. Vui lòng nhập đúng!")
            );
            return;
        }
        bool success = await ApiService.SendForgetPassword(client, request);
        if (!success)
            return;
        ServerNetwork.Instance.SendToClient(
            client,
            Service.ForgetPasswordResponse(true, "Lấy lại mật khẩu thành công, bạn vui lòng vào email để tiến hành thay đổi mật khẩu mới.")
        );
    }

    private void Logout(Client client)
    {
        client.User = null;
        ServerNetwork.Instance.SendToClient(client, Service.LoadLoginScene());
    }

    private void LoginWithFakeAccount(Client client)
    {
        client.User = new UserApiUserData
        {
            LastName = $"FakeUser{UnityEngine.Random.Range(1000, 9999)}"
        };
        ServerNetwork.Instance.SendToClient(client, Service.LoadLobbyScene());
    }


    private async Task Register(Client client, RegisterRequest registerRequest)
    {
        try
        {
            if (!IsValidEmail(registerRequest.Email))
            {
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, "Định dạng email không hợp lệ"));
                return;
            }
            (bool, string, RegisterApiResponse) result = await ApiService.Register(registerRequest);
            bool success = result.Item1;
            string message = result.Item2;
            RegisterApiResponse registerApiResponse = result.Item3;
            if (registerApiResponse == null)
            {
                Debug.LogWarning("RegisterApiResponse is null");
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, "Tạo tài khoản thất bại"));
                return;
            }
            if (!success)
            {
                ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(false, message));
                return;
            }
            client.Token = registerApiResponse.Token;
            await UserGift(client, registerApiResponse.Id);
            ServerNetwork.Instance.SendToClient(client, Service.SendRegisterResponse(true, "Tạo tài khoản thành công, vui lòng xác nhận email trước khi đăng nhập"));
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

            UserApiUserData userApiResponse = await ApiService.GetUser(client, apiResponse.Id);
            if (userApiResponse == null)
            {
                Debug.LogWarning($"[AUTH] Get user returned no data for '{request?.Email}'.");
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification(apiResponse.Message ?? "Không tìm thấy thông tin tài khoản"));
                return;
            }

            if (userApiResponse.IsOnline)
            {
                var onLoginClient = ClientManager.TryGetClient(userApiResponse.UserId);
                if (onLoginClient != null)
                {
                    ServerNetwork.Instance.SendToClients(
                        Service.ShowNotification(apiResponse.Message ?? "Tài khoản đang được đăng nhập ở nơi khác"),
                        client,
                        onLoginClient);
                    ServerNetwork.Instance.Disconnect(onLoginClient.PlayerRef);
                    return;
                }
            }

            client.User = userApiResponse;
            client.Password = request.Password;

            AnnouncementResponse[] announcements = await ApiService.GetAllAnnouncement(client);
            UserItem[] userItems = await ApiService.GetInventory(client);

            if (announcements == null)
            {
                Debug.LogError("[SERVER] Announcements is NULL");
            }
            else
            {
                Debug.Log($"[SERVER] Got {announcements.Length} announcements");
            }

            if (userItems != null)
            {
                client.UserItems = userItems.ToList();
                client.OwnedCharacterIds = userItems
                    .Where(x => x.itemId is (int)ItemType.BinhTan or (int)ItemType.HuuNghia)
                    .Select(x => x.itemId - 1)
                    .ToHashSet();
            }

            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendLoginResponse(
                    userApiResponse.FirstName,
                    userApiResponse.LastName,
                    userApiResponse.AvatarUrl,
                    userApiResponse.RankPoint
                ),
                Service.SendBattleConfig(Master.Instance.Config),
                Service.SendAnnouncementResponse(announcements),
                Service.LoadLobbyScene());
        }
        catch (Exception e)
        {
            Debug.LogError($"[AUTH] Unexpected login error for '{request?.Email}'. Error={e}. Message={e.Message}");
            ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Đăng nhập thất bại."));
        }
    }

    private async Task UserGift(Client client, int userId)
    {
        await ApiService.AddUserItem(client, userId, (int)ItemType.BinhTan, 1);
    }
}
public enum ItemType
{
    None,
    Gem,
    BinhTan,
    HuuNghia,
    PhaLe
}