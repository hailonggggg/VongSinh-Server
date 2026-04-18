using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ApiService
{
    private const string LoginUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Auth/login-user";
    private const string SearchUserUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/User?Search={0}&SortBy={1}&IsDescending={2}";
    private const string RegisterUserUrl = "https://unaliveapi-a3hbhfb4dba5gwgs.japanwest-01.azurewebsites.net/api/Auth/register";
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

}
