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
        private static bool isAuthenticated = false;

        public static bool IsAuthenticated => isAuthenticated;

        public static IEnumerator Login(string email, string password, System.Action<bool> callback)
        {
            var loginUrl = baseUrl + loginEndpoint;
            var formData = new WWWForm();
            formData.AddField("email", email);
            formData.AddField("password", password);

            using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, formData))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                var jsonBody = JsonUtility.ToJson(new { email, password });
                byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Login Error: {request.error}");
                    callback(false);
                }
                else
                {
                    var jsonResponse = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                    token = response.token;
                    refreshToken = response.refreshToken;
                    tokenExpires = response.tokenExpires;
                    isAuthenticated = true;
                    callback(true);
                }
            }
        }
        
        public static void Logout()
        {
            token = null;
            refreshToken = null;
            tokenExpires = 0;
            isAuthenticated = false;
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
