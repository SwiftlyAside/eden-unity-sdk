using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor.Scripts.Manager
{
    public static class AuthManager
    {
        private const string baseUrl = "https://dev-api.eden-world.net/";
        private const string loginEndpoint = "v2/auth/email/login";

        private static string _token;
        private static string _refreshToken;
        private static double _tokenExpires;

        public static string UserEmail { get; private set; }

        public static bool IsAuthenticated { get; private set; }

        public static void Initialize()
        {
            _token = PlayerPrefs.GetString("token");
            _refreshToken = PlayerPrefs.GetString("refreshToken");
            _tokenExpires = PlayerPrefs.GetString("tokenExpires", "0") == "0" ? 0 : double.Parse(PlayerPrefs.GetString("tokenExpires"));
            UserEmail = PlayerPrefs.GetString("userEmail");
            IsAuthenticated = !string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_refreshToken) && _tokenExpires > Time.time;
        }

        public static IEnumerator Login(string email, string password, Action<bool> callback)
        {
            var loginUrl = baseUrl + loginEndpoint;
            using var request = UnityWebRequest.PostWwwForm(loginUrl, "");
            var jsonBody = JsonUtility.ToJson(new LoginRequest { email = email, password = password });
            byte[] bodyRaw = new UTF8Encoding().GetBytes(jsonBody);
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
                _token = response.token;
                _refreshToken = response.refreshToken;
                _tokenExpires = response.tokenExpires;
                UserEmail = response.user.email;
                IsAuthenticated = true;
                    
                PlayerPrefs.SetString("token", _token);
                PlayerPrefs.SetString("refreshToken", _refreshToken);
                PlayerPrefs.SetString("tokenExpires", _tokenExpires.ToString());
                PlayerPrefs.SetString("userEmail", UserEmail);
                PlayerPrefs.Save();
                
                callback(true);
            }
        }
        
        public static void Logout(Action callback)
        {
            _token = null;
            _refreshToken = null;
            _tokenExpires = 0;
            IsAuthenticated = false;
            PlayerPrefs.DeleteKey("token");
            PlayerPrefs.DeleteKey("refreshToken");
            PlayerPrefs.DeleteKey("tokenExpires");
            PlayerPrefs.Save();
            callback();
        }

        [Serializable]
        private class LoginRequest
        {
            public string email;
            public string password;
        }

        [Serializable]
        private class LoginResponse
        {
            public string token;
            public string refreshToken;
            public double tokenExpires;
            public User user;
        }

        [Serializable]
        private class User
        {
            // Define user fields here
            public string email;
        }
    }
}
