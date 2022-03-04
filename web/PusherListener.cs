using PusherClient;
using web.Constants;
using web.Models;

public class PusherListener : BackgroundService
{
    private PusherClient.Pusher _pusher;

    public PusherListener(Pusher pusher)
    {
        _pusher = pusher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5_000);
        var channel = await _pusher.SubscribePresenceAsync<ChatMember>(MessagingConstants.PresenceChannel);
        channel.MemberAdded += (sender, member) =>
        {
            Console.WriteLine($"********* Member added {member.Key}");
        };
    }
}