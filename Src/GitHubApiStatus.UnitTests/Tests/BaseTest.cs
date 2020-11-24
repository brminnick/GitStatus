using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GitStatus.Shared;
using Newtonsoft.Json;

namespace GitHubApiStatus.UnitTests
{
    abstract class BaseTest
    {
        const string _authorizationHeaderKey = "Authorization";

        static readonly HttpClient _client = new()
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", nameof(GitHubApiStatus) },
                { _authorizationHeaderKey, "bearer " + GitHubConstants.PersonalAccessToken }
            }
        };

        protected GitHubApiStatusService GitHubApiStatusService { get; } = new GitHubApiStatusService(_client);

        protected static HttpResponseHeaders CreateHttpResponseHeaders(in int rateLimit, in DateTimeOffset rateLimitResetTime, in int remainingRequestCount, in HttpStatusCode httpStatusCode = HttpStatusCode.OK, in bool isAuthenticated = true)
        {
            if (remainingRequestCount > rateLimit)
                throw new ArgumentOutOfRangeException(nameof(remainingRequestCount), $"{nameof(remainingRequestCount)} must be less than or equal to {nameof(rateLimit)}");

            var httpResponse = new HttpResponseMessage(httpStatusCode)
            {
                Headers =
                {
                    { GitHubApiStatusService.RateLimitHeader, rateLimit.ToString() },
                    { GitHubApiStatusService.RateLimitResetHeader,  GetTimeInUnixEpochSeconds(rateLimitResetTime).ToString() },
                    { GitHubApiStatusService.RateLimitRemainingHeader, remainingRequestCount.ToString() }
                }
            };

            if (isAuthenticated)
                httpResponse.Headers.Vary.Add(_authorizationHeaderKey);

            return httpResponse.Headers;
        }

        protected static Task<HttpResponseMessage> SendValidRestApiRequest() => _client.GetAsync($"{GitHubConstants.GitHubRestApiUrl}/repos/brminnick/GitHubApiStatus");

        protected static Task<HttpResponseMessage> SendValidSearchApiRequest() => _client.GetAsync($"{GitHubConstants.GitHubRestApiUrl}/search/code");

        protected static Task<HttpResponseMessage> SendValidCodeScanningApiRequest() => _client.GetAsync($"{GitHubConstants.GitHubRestApiUrl}/repos/brminnick/GitHubApiStatus/code-scanning/alerts");

        protected static Task SendValidGraphQLApiRequest()
        {
            var graphQLRequest = new GraphQLRequest("query { user(login: \"brminnick\"){ name, company, createdAt}}");
            var serializedGraphQLRequest = JsonConvert.SerializeObject(graphQLRequest);

            return _client.PostAsync(GitHubConstants.GitHubGraphQLApiUrl, new StringContent(serializedGraphQLRequest));
        }

        static long GetTimeInUnixEpochSeconds(in DateTimeOffset dateTimeOffset) => dateTimeOffset.ToUnixTimeSeconds();
    }
}