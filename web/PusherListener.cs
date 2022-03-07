using System.Text.Json;
using PusherClient;
using web.Constants;
using web.Models;

public class PusherListener : BackgroundService
{
    private readonly Pusher _pusher;
    private readonly ILogger<PusherListener> _logger;

    public PusherListener(Pusher pusher, ILogger<PusherListener> logger)
    {
        _pusher = pusher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Wiring up pusher handlers");

        await _pusher.ConnectAsync();
        _pusher.Error += OnError;
        _pusher.Subscribed += OnSubscribed;
        _pusher.BindAll(OnAll);

        await _pusher.SubscribeAsync(MessagingConstants.PRIVATE_CHANNEL);

        var channel = await _pusher.SubscribePresenceAsync<ChatMember>(MessagingConstants.PRESENCE_CHANNEL);
        channel.MemberAdded += OnMemberAdded;
        channel.MemberRemoved += OnMemberRemoved;
    }

    private void OnMemberRemoved(object sender, KeyValuePair<string, ChatMember> member)
    {
        _logger.LogInformation("Member Removed - {Member}", member.Value.Name);
    }

    private void OnSubscribed(object sender, Channel channel)
    {
        _logger.LogInformation("Pusher Subscribed - {Channel} {Type}", channel.Name, channel.ChannelType);
    }

    private void OnError(object sender, PusherException error)
    {
        _logger.LogError(error, "Pusher Error");
    }

    private void OnAll(string sender, PusherEvent @event)
    {
        _logger.LogInformation("Pusher Event on {Channel} - {Event} : {Data}", @event.ChannelName, @event.EventName, JsonSerializer.Serialize(@event.Data));
    }

    private void OnMemberAdded(object sender, KeyValuePair<string, ChatMember> member)
    {
        _logger.LogInformation("Member added {MemberName}", member.Value.Name);
    }
}