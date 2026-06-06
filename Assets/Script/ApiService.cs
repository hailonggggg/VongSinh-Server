using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;


public static class ApiService
{
    private const string LoginUrl = "https://be-adminmanagementsystem.onrender.com/api/Auth/login-user";
    private const string SearchUserUrl = "https://be-adminmanagementsystem.onrender.com/api/User?Search={0}&SortBy={1}&IsDescending={2}";
    private const string RegisterUserUrl = "https://be-adminmanagementsystem.onrender.com/api/Auth/register";
    private const string AnnouncementUrl = "https://be-adminmanagementsystem.onrender.com/api/Announcement?Search={0}&SortBy={1}&IsDescending={2}";
    private const string GemBundleUrl = "https://be-adminmanagementsystem.onrender.com/api/Shop/gem-bundles";
    private const string OrderUrl = "https://be-adminmanagementsystem.onrender.com/api/Order";
    private const string InventoryUrl = "https://be-adminmanagementsystem.onrender.com/api/Player/inventory";
    private const string GetUserById = "https://be-adminmanagementsystem.onrender.com/api/User/{0}";
    private const string GetPlayerProfile = "https://be-adminmanagementsystem.onrender.com/api/Player/Profile";
    private const string SkinBundleUrl = "https://be-adminmanagementsystem.onrender.com/api/Shop/skin-and-character-bundles";
    private const string PurchaseOrderUrl = "https://be-adminmanagementsystem.onrender.com/api/PurchaseOrder";
    private const string ItemUrl = "https://be-adminmanagementsystem.onrender.com/api/Item/{0}";
    private const string GiftUrl = "https://be-adminmanagementsystem.onrender.com/api/Gift";
    private const string SearchCharacterStatUrl = "https://be-adminmanagementsystem.onrender.com/api/Character/stats?Search={0}";
    private const string SearchCharacterSkillUrl = "https://be-adminmanagementsystem.onrender.com/api/Character/skills?Search={0}";
    private const string SearchCharacterAttackUrl = "https://be-adminmanagementsystem.onrender.com/api/Character/attacks?Search={0}";
    private const string SearchCharacterPassiveUrl = "https://be-adminmanagementsystem.onrender.com/api/Character/passives?Search={0}";
    private const string RankPointUrl = "https://be-adminmanagementsystem.onrender.com/api/Player/rank-point";
    private const string UploadImageUrl = "https://be-adminmanagementsystem.onrender.com/api/Player/avatar";
    private const string ForgetPasswordUrl = "https://be-adminmanagementsystem.onrender.com/api/Auth/forgot-password-user";
    private const string UpdatePlayerProfileUrl = "https://be-adminmanagementsystem.onrender.com/api/Player/profile";
    private const string RankingLeaderBoardUrl = "https://be-adminmanagementsystem.onrender.com/api/Player/rankingLeaderBoard?limit={0}&direction={1}&cursorRank={2}";
    private const string CharacterPvPUrl = "https://be-adminmanagementsystem.onrender.com/api/Character/pvp";

    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static async Task<LoginApiResponse> Login(LoginRequest request)
    {
        if (request == null)
        {
            Debug.LogWarning("Request is null");
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            Debug.LogWarning("Email or Password is Empty");
            return null;
        }

        string json = JsonUtility.ToJson(request);

        try
        {
            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, LoginUrl);
            httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!CheckSuccessStatusAndLogError(response, responseJson))
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    if (responseJson.Contains("Email is not verified."))
                    {
                        return new LoginApiResponse
                        {
                            Success = false,
                            Message = "Vui lòng xác nhận email"
                        };
                    }
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new LoginApiResponse
                    {
                        Success = false,
                        Message = "Tài khoản hoặc mật khẩu sai"
                    };
                }
                if(response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new LoginApiResponse
                    {
                        Success = false,
                        Message = "Tài khoản đã bị khóa"
                    };
                }
                return null;
            }
            LoginApiResponse loginResponse = JsonConvert.DeserializeObject<LoginApiResponse>(responseJson);
            if (loginResponse == null)
            {
                Debug.LogWarning($"[AUTH API] Could not parse login response. Body={responseJson}");
                return null;
            }

            return loginResponse;
        }
        catch (TaskCanceledException exception)
        {
            Debug.LogError($"[AUTH API] Login request timed out. Error={exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            Debug.LogError($"[AUTH API] Could not reach auth API. Error={exception.Message}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AUTH API] Unexpected error while calling login API. Error={exception}");
        }
        return null;
    }

    public static async Task<UserApiUserData> GetUser(Client client, int userId)
    {
        try
        {
            // string requestUrl = string.Format(GetUserById, userId);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GetPlayerProfile);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[AUTH API] Get user failed. Status={(int)response.StatusCode} Body={responseJson}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                Debug.LogWarning("[AUTH API] Get user response body was empty.");
                return null;
            }

            UserApiUserData userApiUserData = JsonConvert.DeserializeObject<UserApiUserData>(responseJson);

            if (userApiUserData == null)
            {
                Debug.LogWarning($"[AUTH API] Could not parse user response. Body={responseJson}");
                return null;
            }

            return userApiUserData;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AUTH API] Unexpected error while calling user API. Error={exception}");
        }
        return null;
    }

    public static async Task<(bool, string, RegisterApiResponse)> Register(RegisterRequest registerRequest)
    {

        try
        {
            string json = JsonConvert.SerializeObject(registerRequest);
            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, RegisterUserUrl);
            httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
            string responseJson = await response.Content.ReadAsStringAsync();
            if (!CheckSuccessStatusAndLogError(response, responseJson))
            {
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return (false, "Email đã tồn tại, vui lòng sử dụng email khác", null);
                }
                return (false, "Có lỗi xảy ra", null);
            }
            RegisterApiResponse registerApiResponse = JsonConvert.DeserializeObject<RegisterApiResponse>(responseJson);
            if (registerApiResponse == null)
            {
                Debug.LogWarning($"[AUTH API] Could not parse register response. Body={responseJson}");
                return (false, "Có lỗi xảy ra", null);
            }
            return (true, "Đăng ký thành công", registerApiResponse);

        }
        catch (TaskCanceledException exception)
        {
            Debug.LogError($"[AUTH API] Login request timed out. Error={exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            Debug.LogError($"[AUTH API] Could not reach auth API. Error={exception.Message}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AUTH API] Unexpected error while calling login API. Error={exception}");
        }
        return (false, "Có lỗi xảy ra", null);
    }

    public static bool CheckSuccessStatusAndLogError(HttpResponseMessage response, string responseJson)
    {
        if (!response.IsSuccessStatusCode)
        {
            Debug.LogWarning($"[AUTH API] Get user failed. Status={(int)response.StatusCode} Body={responseJson}");
            return false;
        }
        return true;
    }



    [Serializable]
    private class AnnouncementArrayWrapper
    {
        public AnnouncementResponse[] Data;
    }

    public static async Task<AnnouncementResponse[]> GetAllAnnouncement(
    Client client,
    string search = "",
    string sortBy = "",
    bool isDescending = false)
    {
        try
        {
            string requestUrl = string.Format(
                AnnouncementUrl,
                Uri.EscapeDataString(search ?? string.Empty),
                Uri.EscapeDataString(sortBy ?? string.Empty),
                isDescending.ToString().ToLower()
            );

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            Debug.Log($"[ANNOUNCEMENT RAW] {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[API] Get announcements failed. Status={(int)response.StatusCode} Body={responseJson}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                Debug.LogWarning("[API] Get announcements response body was empty.");
                return null;
            }

            AnnouncementResponse[] result =
                JsonConvert.DeserializeObject<AnnouncementResponse[]>(responseJson);

            if (result == null || result.Length == 0)
            {
                Debug.LogWarning("[API] Announcement list empty or parse failed.");
                return null;
            }

            Debug.Log($"[API] Parsed {result.Length} announcements successfully.");

            return result;
        }
        catch (JsonException ex)
        {
            Debug.LogError($"[API] JSON parse error: {ex}");
        }
        catch (TaskCanceledException ex)
        {
            Debug.LogError($"[API] Request timeout: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"[API] Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[API] Unexpected error: {ex}");
        }

        return null;
    }

    public static async Task<GemBundleResponse[]> GetAllGemBundle(Client client)
    {
        try
        {
            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, GemBundleUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            Debug.Log($"[BUNDLE RAW] {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[API] Get bundles failed {(int)response.StatusCode}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                Debug.LogWarning("[API] Empty bundle response");
                return null;
            }

            GemBundleResponse[] result =
                JsonConvert.DeserializeObject<GemBundleResponse[]>(responseJson);

            if (result == null || result.Length == 0)
            {
                Debug.LogWarning("[API] Bundle parse failed or empty");
                return null;
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[API] Bundle error: {e}");
        }

        return null;
    }

    public static async Task<SkinAndCharacterBundleResponse[]> GetAllSkinAndCharacterBundle(Client client)
    {
        try
        {
            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, SkinBundleUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string responseJson =
                await response.Content.ReadAsStringAsync();

            Debug.Log($"[SKIN BUNDLE RAW] {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning(
                    $"[API] Get skin bundles failed {(int)response.StatusCode}"
                );

                return null;
            }

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                Debug.LogWarning("[API] Empty skin bundle response");
                return null;
            }

            SkinAndCharacterBundleResponse[] result =
                JsonConvert.DeserializeObject<SkinAndCharacterBundleResponse[]>(
                    responseJson
                );

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[API] Skin bundle error: {e}");
        }

        return null;
    }

    public static async Task<OrderResponse> CreateOrder(Client client, GemBundleResponse bundle)
    {
        try
        {
            OrderRequest request = new OrderRequest
            {
                userId = client.User.UserId,
                totalAmount = bundle.bundlePrice,
                playerEmail = client.User.Email,
                playerUserName = client.User.Email,
                returnUrl = "https://return",
                cancelUrl = "https://cancel",
                expiredAt = DateTime.UtcNow.ToString("o"),
                items = new[]
                {
                    new OrderItem
                    {
                        bundleId = bundle.gemBundleId,
                        bundleBuyQuantity = 1,
                    }
                },
                isSuccess = false
            };

            string json = JsonConvert.SerializeObject(request);

            Debug.Log($"[API ORDER] Request JSON: {json}");

            using HttpRequestMessage httpRequest =
                new HttpRequestMessage(HttpMethod.Post, OrderUrl);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            httpRequest.Content =
                new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response =
                await httpClient.SendAsync(httpRequest);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[API ERROR] Status: {response.StatusCode}");
                Debug.LogError($"[API ERROR] Body: {responseJson}");
                return null;
            }

            return JsonConvert.DeserializeObject<OrderResponse>(responseJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API ORDER] {e}");
        }

        return null;
    }
    public static async Task<OrderResponse> GetOrderById(Client client, int orderId)
    {
        try
        {
            string url = $"{OrderUrl}/{orderId}";

            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[API ORDER GET] {json}");
                return null;
            }

            return JsonConvert.DeserializeObject<OrderResponse>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API ORDER GET] {e}");
            return null;
        }
    }

    public static async Task<PurchaseOrderResponse> PurchaseSkinBundle(Client client, int bundleId)
    {
        try
        {
            var body = new
            {
                userId = client.User.UserId,
                skinAndCharacterBundleId = bundleId
            };

            string json = JsonConvert.SerializeObject(body);


            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Post, PurchaseOrderUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            request.Content =
                new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            Debug.Log($"[PURCHASE ORDER] Status: {response.StatusCode}, Body: {responseJson}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.Log($"[PURCHASE ORDER] Request JSON: {json}");
                Debug.Log($"[PURCHASE ORDER] URL: {PurchaseOrderUrl}");
                Debug.Log($"[PURCHASE ORDER] Token: {client.Token?.Substring(0, 20)}...");
                string errorMsg = "Mua thất bại.";
                try
                {
                    var err = JsonConvert.DeserializeAnonymousType(responseJson, new { message = "" });
                    if (!string.IsNullOrWhiteSpace(err?.message)) errorMsg = err.message;
                }
                catch { }

                return new PurchaseOrderResponse { success = false, message = errorMsg };
            }

            return new PurchaseOrderResponse { success = true, message = "OK" };
        }
        catch (Exception e)
        {
            Debug.LogError($"[PURCHASE ORDER] {e}");
            return new PurchaseOrderResponse { success = false, message = "Lỗi server." };
        }
    }
    public static async Task<UserItem[]> GetInventory(Client client)
    {
        try
        {
            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, InventoryUrl);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();

            Debug.Log($"[INVENTORY RAW] {json}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[API] Inventory failed {(int)response.StatusCode}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[API] Inventory empty");
                return null;
            }

            return JsonConvert.DeserializeObject<UserItem[]>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API INVENTORY] {e}");
            return null;
        }
    }

    public static async Task<ItemDetail> GetItemById(Client client, int itemId)
    {
        try
        {
            string url = string.Format(ItemUrl, itemId);

            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[API ITEM] Get item {itemId} failed: {json}");
                return null;
            }

            return JsonConvert.DeserializeObject<ItemDetail>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API ITEM] {e}");
            return null;
        }
    }

    public static async Task AddUserItem(Client client, int id, int itemId = 1, int quantity = 1)
    {
        try
        {
            var body = new UserItem
            {
                userId = id,
                itemId = itemId,
                quantity = quantity,
            };

            string json = JsonConvert.SerializeObject(body);

            using HttpRequestMessage request = new(HttpMethod.Post, GiftUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[API ADD ITEM] Status: {response.StatusCode}");
                Debug.LogError($"[API ADD ITEM] Body: {responseJson}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[API ADD ITEM] {e}");
        }
    }

    public static async Task<UserItem[]> GetUserItems(Client client, int userId)
    {
        try
        {
            using HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, $"{InventoryUrl}/{userId}");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response =
                await httpClient.SendAsync(request);

            string json = await response.Content.ReadAsStringAsync();

            Debug.Log($"[USER ITEMS RAW] {json}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogWarning($"[API] Get user items failed {(int)response.StatusCode}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[API] User items empty");
                return null;
            }

            return JsonConvert.DeserializeObject<UserItem[]>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API USER ITEMS] {e}");
            return null;
        }
    }

    public static async Task<CharacterStats> FindCharacterStatById(Client client, int unitId)
    {
        try
        {
            string url = string.Format(SearchCharacterStatUrl, unitId);
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<CharacterStats>(responseJson);
        }
        catch (Exception)
        {

        }
        return null;
    }

    public static async Task<Dictionary<int, CharacterStats>> FetchAllCharacterStats()
    {
        try
        {
            string url = string.Format(SearchCharacterStatUrl, "");
            using HttpRequestMessage request = new(HttpMethod.Get, url);

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            List<CharacterStats> list = JsonConvert.DeserializeObject<List<CharacterStats>>(responseJson);
            return list.ToDictionary(x => x.CharacterId);
        }
        catch (Exception)
        {

        }
        return new Dictionary<int, CharacterStats>();
    }

    public static async Task<Dictionary<int, CharacterSkill>> FetchAllCharacterSkill()
    {
        try
        {
            string url = string.Format(SearchCharacterSkillUrl, "");
            using HttpRequestMessage request = new(HttpMethod.Get, url);

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            List<CharacterSkill> list = JsonConvert.DeserializeObject<List<CharacterSkill>>(responseJson);
            return list.ToDictionary(x => x.CharacterSkillId);
        }
        catch (Exception)
        {

        }
        return new Dictionary<int, CharacterSkill>();
    }

    public static async Task<Dictionary<int, CharacterBasicAttack>> FetchAllCharacterBasicAttacks()
    {
        try
        {
            string url = string.Format(SearchCharacterAttackUrl, "");
            using HttpRequestMessage request = new(HttpMethod.Get, url);

            using HttpResponseMessage response = await httpClient.SendAsync(request);

            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            List<CharacterBasicAttack> list = JsonConvert.DeserializeObject<List<CharacterBasicAttack>>(responseJson);
            return list.ToDictionary(x => x.CharacterAttackId);
        }
        catch (Exception)
        {

        }
        return new Dictionary<int, CharacterBasicAttack>();
    }

    public static async Task<int> SetRankPoint(Client winner, int rankPointPlus)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, RankPointUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", winner.Token);
            var body = new
            {
                points = rankPointPlus,
                isAddition = true
            };

            string json = JsonConvert.SerializeObject(body);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError("Set new point rank failed");
            }
            string responseJson = await response.Content.ReadAsStringAsync();
            return int.Parse(responseJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error at SetRankPoint: {e.Message}");
        }
        return 0;
    }

    public static async Task<string> UpLoadImage(Client client, byte[] imageByteArr, string fileExtension)
    {
        if (imageByteArr == null || imageByteArr.Length == 0)
        {
            Debug.LogError("[UPLOAD IMAGE] Image data is null or empty");
            return null;
        }

        try
        {
            string ext = fileExtension.Replace(".", "").ToLower();

            string contentType = ext == "png" ? "image/png" : "image/jpeg";
            string fileName = $"upload_image.{ext}";

            var imageContent = new ByteArrayContent(imageByteArr);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var content = new MultipartFormDataContent
            {
                { imageContent, "file", fileName }
            };

            using HttpRequestMessage request = new(HttpMethod.Post, UploadImageUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);
            request.Content = content;

            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[UPLOAD IMAGE] Failed: {(int)response.StatusCode} - {responseJson}");
                return null;
            }
            ImageUrl imageUrl = JsonConvert.DeserializeObject<ImageUrl>(responseJson);

            return imageUrl.AvatarUrl;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UPLOAD IMAGE] Exception: {e.Message} {e.StackTrace}");
            return null;
        }
    }
    [Serializable]
    class ImageUrl
    {
        public string AvatarUrl;
    }


    public static async Task<bool> SendForgetPassword(Client client, ForgetPasswordRequest request)
    {
        try
        {
            var obj = new
            {
                email = request.Email
            };
            var json = JsonConvert.SerializeObject(obj);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ForgetPasswordUrl);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var httpResponse = await httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Lấy lại mật khẩu thất bại"));
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SendForgetPassword] Error: {e.Message}");
        }
        return false;
    }

    public static async Task UpdateProfile(Client client, string firstName, string lastName, string newPassword)
    {
        try
        {
            var obj = new
            {
                email = "",
                password = newPassword,
                firstName,
                lastName,
                userName = "",
                avatarUrl = "",
                banned = -1,
                bannedUntil = DateTimeOffset.UtcNow,
                lastOnline = DateTimeOffset.UtcNow,
                isOnline = -1,
                rankPoint = -1,
            };
            var json = JsonConvert.SerializeObject(obj);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, UpdatePlayerProfileUrl);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            using var httpResponse = await httpClient.SendAsync(httpRequest);
            if (!httpResponse.IsSuccessStatusCode)
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Thay đổi thông tin thất bại"));
                return;
            }
            client.User.FirstName = firstName;
            client.User.LastName = lastName;
            client.Password = newPassword;
            ServerNetwork.Instance.SendToClient(
                client,
                Service.SendLoginResponse(
                    firstName,
                    lastName,
                    client.User.AvatarUrl,
                    client.User.RankPoint
                )
            );
            ServerNetwork.Instance.SendToClient(
                client,
                Service.ShowNotification("Thay đổi thông tin thành công")
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[SendForgetPassword] Error: {e.Message}");
        }
    }

    public static async Task<LeaderboardResponse> GetRankingLeaderBoard(Client client, RankingLeaderBoardRequest request)
    {
        try
        {
            string url = string.Format(RankingLeaderBoardUrl, 10, request.Direction, request.CursorRank);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", client.Token);

            using var httpResponse = await httpClient.SendAsync(httpRequest);
            string jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            if (!httpResponse.IsSuccessStatusCode)
            {
                ServerNetwork.Instance.SendToClient(client, Service.ShowNotification("Lấy dữ liệu bảng xếp hạng thất bại"));
                return null;
            }
            return JsonConvert.DeserializeObject<LeaderboardResponse>(jsonResponse);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GetRankingLeaderBoard] Error: {e.Message}");
        }
        return null;
    }

    public static async Task<Dictionary<int,CharacterPvP>> GetAllCharacterPvP()
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, CharacterPvPUrl);
            using HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[API] Get Character PvP failed: {(int)response.StatusCode} - {responseJson}");
                return null;
            }

            List<CharacterPvP> characterPvPs = JsonConvert.DeserializeObject<List<CharacterPvP>>(responseJson);
            return characterPvPs.ToDictionary(x => x.CharacterTacticId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[API] Get Character PvP error: {e.Message}");
        }
        return new Dictionary<int, CharacterPvP>();
    }
}
