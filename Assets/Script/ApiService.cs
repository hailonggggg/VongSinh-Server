using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;


public static class ApiService
{
    private const string LoginUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Auth/login-user";
    private const string SearchUserUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/User?Search={0}&SortBy={1}&IsDescending={2}";
    private const string RegisterUserUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Auth/register";
    private const string AnnouncementUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Announcement?Search={0}&SortBy={1}&IsDescending={2}";
    private const string GemBundleUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Shop/gem-bundles";
    private const string OrderUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Order";
    private const string InventoryUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Player/inventory";
    //private const string OrderUrl = "https://localhost:7270/api/Order";

    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
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
                return null;
            }
            LoginApiResponse loginResponse = JsonUtility.FromJson<LoginApiResponse>(responseJson);
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

    [Serializable]
    private class UserApiArrayWrapper
    {
        public UserApiUserData[] Data;
    }

    public static async Task<UserApiUserData[]> GetUser(Client client, string email)
    {
        try
        {
            string requestUrl = string.Format(
                SearchUserUrl,
                Uri.EscapeDataString(email),
                string.Empty,
                "false");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
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

            string wrappedJson = WrapArrayResponse(responseJson, "Data");
            UserApiArrayWrapper userResponse = JsonUtility.FromJson<UserApiArrayWrapper>(wrappedJson);

            if (userResponse?.Data == null)
            {
                Debug.LogWarning($"[AUTH API] Could not parse user response. Body={responseJson}");
                return null;
            }

            return userResponse.Data;
        }
        catch (TaskCanceledException exception)
        {
            Debug.LogError($"[AUTH API] Get user request timed out. Error={exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            Debug.LogError($"[AUTH API] Could not reach user API. Error={exception.Message}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AUTH API] Unexpected error while calling user API. Error={exception}");
        }
        return null;
    }

    private static string WrapArrayResponse(string responseJson, string fieldName)
    {
        string trimmedJson = responseJson.TrimStart();
        if (!trimmedJson.StartsWith("["))
        {
            return responseJson;
        }

        return $"{{\"{fieldName}\":{trimmedJson}}}";
    }

    private static string BuildErrorMessage(HttpStatusCode statusCode, string responseJson)
    {
        if (!string.IsNullOrWhiteSpace(responseJson))
        {
            return responseJson;
        }

        return $"API Error ({(int)statusCode})";
    }

    public static async Task<RegisterApiResponse> Register(RegisterRequest registerRequest)
    {

        try
        {
            string json = JsonUtility.ToJson(registerRequest, prettyPrint: true);
            using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, RegisterUserUrl);
            httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);
            string responseJson = await response.Content.ReadAsStringAsync();
            if (!CheckSuccessStatusAndLogError(response, responseJson))
            {
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return new RegisterApiResponse
                    {
                        Success = false,
                        Message = "Email đã tồn tại, vui lòng sử dụng email khác"
                    };
                }
                return null;
            }
            RegisterApiResponse registerApiResponse = JsonUtility.FromJson<RegisterApiResponse>(responseJson);
            if (registerApiResponse == null)
            {
                Debug.LogWarning($"[AUTH API] Could not parse register response. Body={responseJson}");
                return null;
            }
            return registerApiResponse;

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

    public static async Task<OrderResponse> CreateOrder(Client client, GemBundleResponse bundle)
    {
        try
        {
            OrderRequest request = new OrderRequest
            {
                userId = client.Player.Id,
                totalAmount = bundle.bundlePrice,
                playerEmail = client.Player.Username,
                playerUserName = client.Player.Name,
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
}
