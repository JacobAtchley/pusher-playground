using System.Text.Json;
using MatBlazor;
using Microsoft.Extensions.Options;
using PusherClient;
using PusherServer;
using web;
using web.Constants;
using web.Data;
using web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddMatBlazor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<HttpClient>(provider =>
{
    var factory = provider.GetService<IHttpClientFactory>();
    var client = factory.CreateClient();
    return client;
});
builder.Services.AddMatToaster(config =>
{
    config.Position = MatToastPosition.BottomRight;
    config.PreventDuplicates = true;
    config.NewestOnTop = true;
    config.ShowCloseButton = true;
    config.MaximumOpacity = 95;
    config.VisibleStateDuration = 3000;
});
builder.Services.Configure<PusherPlaygroundConfig>(builder.Configuration.GetSection("Pusher"));
builder.Services.AddHttpClient(nameof(PusherPlaygroundHttpAuthorizer))
    .ConfigureHttpClient(x =>
    {
        x.BaseAddress = new Uri("https://localhost:7212");
        x.Timeout = TimeSpan.FromMinutes(5);
    })
    .ConfigurePrimaryHttpMessageHandler(x => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    });
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetService<IOptions<PusherPlaygroundConfig>>()?.Value!;
    var httpFactory = provider.GetService<IHttpClientFactory>();
    var pusher = new PusherClient.Pusher(options.Key, new()
    {
        Cluster = options.Cluster,
        Authorizer = new PusherPlaygroundHttpAuthorizer(httpFactory)
    });
    return pusher;
});
builder.Services.AddScoped(provider =>
{
    var options = provider.GetService<IOptions<PusherPlaygroundConfig>>()?.Value!;
    var pusher = new PusherServer.Pusher(options.AppId, options.Key, options.Secret, new PusherServer.PusherOptions
    {
        Cluster = options.Cluster,
    });

    return pusher;
});

builder.Services.AddHostedService<PusherListener>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapPost("/api/pusher/auth/{username}", (
    string username,
    PusherServer.Pusher pusherServer,
    HttpContext context) =>
{
    string channelName = context.Request.Form["channel_name"];
    string socketId = context.Request.Form["socket_id"];

    string? authData = null;

    // if (Channel.GetChannelType(channelName) == ChannelTypes.Presence)
    // {
        var channelData = new PresenceChannelData
        {
            user_id = socketId,
            user_info = new { Name = username }
        };

        authData = pusherServer.Authenticate(channelName, socketId, channelData).ToJson();
    // }
    // else
    // {
    //     authData = pusherServer.Authenticate(channelName, socketId).ToJson();
    // }

    return authData;
});
app.Run();