namespace CrisClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    public class CrisClient
    {
        private const int MinRetryBackoffInMilliseconds = 10;
        private const int MaxRetryBackoffInMilliseconds = 100;
        private const int MaxNumberOfRetries = 5;
        private const string OneAPIOperationLocationHeaderKey = "Operation-Location";

        private readonly HttpClient client;
        private readonly string speechToTextBasePath;

        private CrisClient(HttpClient client)
        {
            this.client = client;
            speechToTextBasePath = "api/speechtotext/v1.0/";
        }

        public static async Task<CrisClient> CreateApiV1ClientAsync(string username, string key, string hostName, int port)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(25);
            client.BaseAddress = new UriBuilder(Uri.UriSchemeHttps, hostName, port).Uri;

            var tokenProviderPath = "/oauth/ctoken";
            var clientToken = await CreateClientTokenAsync(client, hostName, port, tokenProviderPath, username, key).ConfigureAwait(false);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", clientToken.AccessToken);

            return new CrisClient(client);
        }

        public Task<IEnumerable<Transcription>> GetTranscriptionsAsync()
        {
            var path = $"{this.speechToTextBasePath}Transcriptions";
            return this.GetAsync<IEnumerable<Transcription>>(path);
        }

        public Task<Transcription> GetTranscriptionAsync(Guid id)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/{id}";
            return this.GetAsync<Transcription>(path);
        }

        public Task<Uri> PostTranscriptionAsync(string locale, string subscriptionKey, Uri recordingsUrl)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/";
            var transcriptionDefinition = TranscriptionDefinition.Create(locale, subscriptionKey, recordingsUrl);

            return this.PostAsJsonAsync<TranscriptionDefinition>(path, transcriptionDefinition);
        }

        public Task<Uri> PostTranscriptionAsync(string locale, string subscriptionKey, Uri recordingsUrl, IEnumerable<Guid> modelIds)
        {
            var models = modelIds.Select(m => ModelIdentity.Create(m)).ToList();

            var path = $"{this.speechToTextBasePath}Transcriptions/";
            var transcriptionDefinition = TranscriptionDefinition.Create(locale, subscriptionKey, recordingsUrl, models);

            return this.PostAsJsonAsync<TranscriptionDefinition>(path, transcriptionDefinition);
        }

        public Task<Transcription> GetTranscriptionAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return this.GetAsync<Transcription>(location.AbsolutePath);
        }

        public Task DeleteTranscriptionAsync(Guid id)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/{id}";
            return this.client.DeleteAsync(path);
        }

        private static async Task<Token> CreateClientTokenAsync(HttpClient client, string hostName, int port, string tokenProviderPath, string username, string key)
        {
            var uriBuilder = new UriBuilder("https", hostName, port, tokenProviderPath);

            var form = new Dictionary<string, string>
                    {
                        { "grant_type", "client_credentials" },
                        { "client_id", "cris" },
                        { "client_secret", key },
                        { "username", username }
                    };

            var rand = new Random();

            for (var retries = 0; retries < MaxNumberOfRetries; retries++)
            {
                HttpResponseMessage response = null;
                try
                {
                    response = await client.PostAsync(uriBuilder.Uri, new FormUrlEncodedContent(form)).ConfigureAwait(false);
                    var token = await response.Content.ReadAsAsync<Token>().ConfigureAwait(false);

                    if (string.IsNullOrEmpty(token.Error))
                    {
                        return token;
                    }
                }
                catch (HttpRequestException)
                {
                    // Didn't work. Too bad. Try again.
                }
                finally
                {
                    response?.Dispose();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(rand.Next(MinRetryBackoffInMilliseconds, MaxRetryBackoffInMilliseconds)))
                    .ConfigureAwait(false);
            }

            throw new InvalidOperationException("Exceeded maximum number of retries for getting a token.");
        }

        private static Uri GetLocationFromPostResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new NotImplementedException();
            }

            IEnumerable<string> headerValues;
            if (response.Headers.TryGetValues(OneAPIOperationLocationHeaderKey, out headerValues))
            {
                if (headerValues.Any())
                {
                    return new Uri(headerValues.First());
                }
            }

            return response.Headers.Location;
        }

        private async Task<Uri> PostAsJsonAsync<TPayload>(string path, TPayload payload)
        {
            using (var response = await this.client.PostAsJsonAsync(path, payload).ConfigureAwait(false))
            {
                return GetLocationFromPostResponse(response);
            }
        }

        private async Task<TResponse> GetAsync<TResponse>(string path)
        {
            using (var response = await this.client.GetAsync(path).ConfigureAwait(false))
            {
                var contentType = response.Content.Headers.ContentType;

                if (response.IsSuccessStatusCode && string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var result = await response.Content.ReadAsAsync<TResponse>().ConfigureAwait(false);

                    return result;
                }

                throw new NotImplementedException();
            }
        }
    }
}
