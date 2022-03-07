using System.Net;
using System.Net.Http.Headers;
using PusherClient;

namespace web;

public class PusherPlaygroundHttpAuthorizer : IAuthorizer, IAuthorizerAsync
{
    private readonly IHttpClientFactory _clientFactory;
    private const string ENDPOINT = "/api/pusher/auth/server";

    public PusherPlaygroundHttpAuthorizer(IHttpClientFactory clientFactory, AuthenticationHeaderValue? authenticationHeader = null)
    {
        _clientFactory = clientFactory;
        AuthenticationHeader = authenticationHeader;
    }

    public AuthenticationHeaderValue? AuthenticationHeader { get; set; }

    public TimeSpan? Timeout { get; set; }

    public string? Authorize(string channelName, string socketId)
    {
        try
        {
            return AuthorizeAsync(channelName, socketId).Result;
        }
        catch (AggregateException ex)
        {
            throw ex.InnerException!;
        }
    }

    public async Task<string?> AuthorizeAsync(string channelName, string socketId)
    {
        using var httpClient = _clientFactory.CreateClient(nameof(PusherPlaygroundHttpAuthorizer));
        using HttpContent content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
        {
            new("channel_name", channelName),
            new("socket_id", socketId)
        });

        HttpResponseMessage httpResponseMessage;
        try
        {
            PreAuthorize(httpClient);
            httpResponseMessage = await httpClient.PostAsync("/api/pusher/auth/server", content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var code = ErrorCodes.ChannelAuthorizationError;

            if (ex is TaskCanceledException)
            {
                code = ErrorCodes.ChannelAuthorizationTimeout;
            }

            throw new ChannelAuthorizationFailureException(code,ENDPOINT, channelName, socketId, ex);
        }

        switch (httpResponseMessage.StatusCode)
        {
            case HttpStatusCode.RequestTimeout:
            case HttpStatusCode.GatewayTimeout:
                throw new ChannelAuthorizationFailureException($"Authorization timeout ({httpResponseMessage.StatusCode}).",
                    ErrorCodes.ChannelAuthorizationTimeout, ENDPOINT, channelName, socketId);
            case HttpStatusCode.Forbidden:
                throw new ChannelUnauthorizedException(ENDPOINT, channelName, socketId);
            default:
                try
                {
                    httpResponseMessage.EnsureSuccessStatusCode();
                    var response = await httpResponseMessage.Content.ReadAsStringAsync();
                    return response;
                }
                catch (Exception ex)
                {
                    throw new ChannelAuthorizationFailureException(ErrorCodes.ChannelAuthorizationError, ENDPOINT, channelName, socketId,
                        ex);
                }
        }
    }

    protected virtual void PreAuthorize(HttpClient httpClient)
    {
        if (Timeout.HasValue)
        {
            httpClient.Timeout = Timeout.Value;
        }

        if (AuthenticationHeader == null)
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeader;
    }
}