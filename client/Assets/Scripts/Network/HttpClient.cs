using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TexasHoldem.Network
{
    public static class HttpClient
    {
        private static string _authToken;

        public static void SetAuthToken(string token)
        {
            _authToken = token;
        }

        public static void ClearAuthToken()
        {
            _authToken = null;
        }

        public static async Task<T> GetAsync<T>(string url) where T : class
        {
            using var request = UnityWebRequest.Get(url);
            SetHeaders(request);
            
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return ProcessResponse<T>(request);
        }

        public static async Task<T> PostAsync<T>(string url, object data) where T : class
        {
            string json = data != null ? JsonUtility.ToJson(data) : "{}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return ProcessResponse<T>(request);
        }

        public static async Task<T> PutAsync<T>(string url, object data) where T : class
        {
            string json = data != null ? JsonUtility.ToJson(data) : "{}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "PUT");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return ProcessResponse<T>(request);
        }

        public static async Task<bool> DeleteAsync(string url)
        {
            using var request = UnityWebRequest.Delete(url);
            SetHeaders(request);

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            return request.result == UnityWebRequest.Result.Success;
        }

        private static void SetHeaders(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
        }

        private static T ProcessResponse<T>(UnityWebRequest request) where T : class
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"HTTP Error: {request.error}");
                
                if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                {
                    try
                    {
                        var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        throw new HttpException(request.responseCode, error.error ?? request.error);
                    }
                    catch (Exception)
                    {
                        throw new HttpException(request.responseCode, request.error);
                    }
                }
                
                throw new HttpException(request.responseCode, request.error);
            }

            if (string.IsNullOrEmpty(request.downloadHandler?.text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"JSON Parse Error: {ex.Message}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                throw;
            }
        }
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
    }

    public class HttpException : Exception
    {
        public long StatusCode { get; }

        public HttpException(long statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
