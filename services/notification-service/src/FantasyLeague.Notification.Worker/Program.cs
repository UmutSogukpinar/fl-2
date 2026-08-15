using FantasyLeague.Notification.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNotificationConsumers();
builder.Services.AddRabbitMqConsumers(builder.Configuration);

var app = builder.Build();
app.Run();
