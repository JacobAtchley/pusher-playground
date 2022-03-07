namespace web.Constants;

public static class MessagingConstants
{
    private const string CHANNEL = "playground-channel";
    public const string PRIVATE_CHANNEL = $"private-{CHANNEL}";
    public const string PRESENCE_CHANNEL = $"presence-{CHANNEL}";
    public const string EVENT = "message";
}