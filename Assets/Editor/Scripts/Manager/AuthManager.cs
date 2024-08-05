using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class AuthManager
{
    private static readonly string baseUrl = "https://api.eden-world.net/";
    private static string authToken;
    private static string refreshToken;
    private static long tokenExpires;

    public static IEnumerator Login(string email, string password, System.Action<bool> callback)
    {
        string loginUrl = baseUrl + "v2/auth/email/login";
        var loginData = new
        {
            email = email,
            password = password
        };

        string jsonData = JsonUtility.ToJson(loginData);

        using (UnityWebRequest request = new UnityWebRequest(loginUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                authToken = response.token;
                refreshToken = response.refreshToken;
                tokenExpires = response.tokenExpires;

                Debug.Log("Login Successful");
                callback(true);
            }
            else
            {
                Debug.LogError($"Login Failed: {request.error}");
                callback(false);
            }
        }
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
        public string refreshToken;
        public long tokenExpires;
        public User user;
    }

    [System.Serializable]
    private class User
    {
        // Define user properties here
    }
}