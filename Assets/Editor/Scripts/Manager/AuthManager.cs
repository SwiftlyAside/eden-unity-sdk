using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor.Scripts.Manager
{
    public static class AuthManager
    {
        private const string baseUrl = "https://dev-api.eden-world.net/";
        private const string loginEndpoint = "v2/auth/email/login";

        private static string token;
        private static string refreshToken;
        private static double tokenExpires;

        public static bool IsAuthenticated { get; private set; }

        public static void Initialize()
        {
            token = PlayerPrefs.GetString("token");
            refreshToken = PlayerPrefs.GetString("refreshToken");
            tokenExpires = PlayerPrefs.GetString("tokenExpires", "0") == "0" ? 0 : double.Parse(PlayerPrefs.GetString("tokenExpires"));
            IsAuthenticated = !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken) && tokenExpires > Time.time;
        }

        public static IEnumerator Login(string email, string password, System.Action<bool> callback)
        {
            var loginUrl = baseUrl + loginEndpoint;
            using var request = UnityWebRequest.PostWwwForm(loginUrl, "");
            var jsonBody = JsonUtility.ToJson(new LoginRequest { email = email, password = password });
            byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Payload: {jsonBody}");
                Debug.LogError($"Login Error: {request.error}");
                Debug.LogError($"Login Response: {request.downloadHandler.text}");
                callback(false);
            }
            else
            {
                var jsonResponse = request.downloadHandler.text;
                var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                token = response.token;
                refreshToken = response.refreshToken;
                tokenExpires = response.tokenExpires;
                IsAuthenticated = true;
                    
                PlayerPrefs.SetString("token", token);
                PlayerPrefs.SetString("refreshToken", refreshToken);
                PlayerPrefs.SetString("tokenExpires", tokenExpires.ToString());
                PlayerPrefs.Save();
                callback(true);
            }
        }
        
        public static void Logout()
        {
            token = null;
            refreshToken = null;
            tokenExpires = 0;
            IsAuthenticated = false;
            PlayerPrefs.DeleteKey("token");
            PlayerPrefs.DeleteKey("refreshToken");
            PlayerPrefs.DeleteKey("tokenExpires");
            PlayerPrefs.Save();
        }

        [System.Serializable]
        private class LoginRequest
        {
            public string email;
            public string password;
        }

        [System.Serializable]
        private class LoginResponse
        {
            public string token;
            public string refreshToken;
            public double tokenExpires;
            public User user;
        }

        [System.Serializable]
        private class User
        {
            // Define user fields here
        }
    }
}
