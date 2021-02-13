using Newtonsoft.Json;
using PhotoStorageAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoStorageAPI.ExternalApi
{
    public class PhotosExternalClient : IPhotosExternalClient
    {
        private static readonly object locker = new object();

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PhotosApiClientSettings _externalApiSettings;

        private AuthResponse _authData { get; set; }

        public PhotosExternalClient(IHttpClientFactory httpClientFactory, PhotosApiClientSettings externalApiSettings)
        {
            if (!Uri.IsWellFormedUriString(externalApiSettings.BaseAddresUrl, UriKind.Absolute))
                throw new UriFormatException($"Bad formed URI: {externalApiSettings.BaseAddresUrl}");

            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _externalApiSettings = externalApiSettings ?? throw new ArgumentNullException(nameof(externalApiSettings));
        }

        public async Task<List<PhotosPage>> GetAllPhotosPages()
        {
            string relativePath = "images?page=";
            var responseMessage = await SendGetRequest($"{relativePath}1");
            if (responseMessage == null)
                return null;

            string pageResponseBody = await responseMessage.Content.ReadAsStringAsync();
            PhotosPage photosPage = JsonConvert.DeserializeObject<PhotosPage>(pageResponseBody);

            List<Task<HttpResponseMessage>> pagesResponseMessages = new List<Task<HttpResponseMessage>>();
            for(int i = 0; i < photosPage.PageCount; i++)
            {
                pagesResponseMessages.Add(SendGetRequest($"{relativePath}{i}"));
            }

            IEnumerable<HttpContent> pagesContents = (await Task.WhenAll(pagesResponseMessages))
                .Where(m => m != null && (m?.IsSuccessStatusCode ?? false))
                .Select(response => response.Content);
            IEnumerable<string> pagesJsons = await Task.WhenAll(pagesContents.Select(content => content.ReadAsStringAsync()));

            return pagesJsons.Select(pageJson => JsonConvert.DeserializeObject<PhotosPage>(pageJson)).ToList();
        }

        public async Task<List<DetailedPhoto>> GetAllPhotosDetails(List<string> photosIds)
        {
            if (photosIds.Count == 0)
                return new List<DetailedPhoto>();

            List<Task<HttpResponseMessage>> photosResponseMessages = new List<Task<HttpResponseMessage>>();
            foreach(string photoId in photosIds)
            {
                photosResponseMessages.Add(SendGetRequest($"/images/{photoId}"));
            }

            IEnumerable<HttpContent> photosContents = (await Task.WhenAll(photosResponseMessages))
                .Where(m => m != null && (m?.IsSuccessStatusCode ?? false))
                .Select(response => response.Content);
            IEnumerable<string> photosJsons = await Task.WhenAll(photosContents.Select(content => content.ReadAsStringAsync()));

            return photosJsons.Select(photoJson => JsonConvert.DeserializeObject<DetailedPhoto>(photoJson)).ToList();
        }

        private HttpClient CreateClient()
        {
            HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_externalApiSettings.TimeoutSeconds);
            httpClient.BaseAddress = new Uri(_externalApiSettings.BaseAddresUrl);

            return httpClient;
        }

        private async Task<HttpResponseMessage> SendGetRequest(string relativePath)
        {
            if (_authData == null)
                await RefreshAuth();

            HttpClient httpClient = CreateClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, relativePath);
            requestMessage.Headers.Authorization = AuthenticationHeaderValue.Parse($"Bearer {_authData.Token}");

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            bool wasAuthSuccessful = responseMessage.StatusCode != HttpStatusCode.Unauthorized;
            if (!wasAuthSuccessful)
            {
                for (int i = 0; i < _externalApiSettings.ApiRetriesCount && !wasAuthSuccessful; i++)
                    wasAuthSuccessful = await RefreshAuth();
                if (!wasAuthSuccessful)
                    return null;
            }

            AuthenticationHeaderValue authenticationHeaderValue =
                AuthenticationHeaderValue.Parse($"Bearer {_authData.Token}");

            requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{relativePath}1");
            requestMessage.Headers.Authorization = authenticationHeaderValue;

            return await httpClient.SendAsync(requestMessage);
        }

        private async Task<bool> RefreshAuth()
        {
            var authRequestMessage = new HttpRequestMessage(HttpMethod.Post, "auth");
            string authRequestBody = JsonConvert.SerializeObject(new AuthRequest { ApiKey = _externalApiSettings.ApiKey });
            authRequestMessage.Content = new StringContent(authRequestBody, Encoding.UTF8, "application/json");

            HttpClient httpClient = CreateClient();

            HttpResponseMessage authResponseMessage = await httpClient.SendAsync(authRequestMessage);

            if (!authResponseMessage.IsSuccessStatusCode)
                return false;

            string authResponseBody = await authResponseMessage.Content.ReadAsStringAsync();

            if (!Monitor.TryEnter(locker, TimeSpan.FromSeconds(5)))
                return false;
            _authData = JsonConvert.DeserializeObject<AuthResponse>(authResponseBody);
            Monitor.Exit(locker);

            return true;
        }
    }
}
