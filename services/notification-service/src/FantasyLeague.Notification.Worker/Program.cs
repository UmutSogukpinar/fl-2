using FantasyLeague.Notification.Infrastructure.Messaging.RabbitMq;
using FantasyLeague.Notification.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services.
builder.Services.AddNotificationConsumers();
builder.Services.AddRabbitMqConsumers(builder.Configuration);

builder.Services.AddSingleton<
    IRabbitMqConnectionProvider,
    RabbitMqConnectionProvider
>();

var app = builder.Build();
app.Run();
