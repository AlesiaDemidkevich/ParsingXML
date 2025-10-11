using DataProcessorService.Services;
using Shared.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<DataProcService>();

var app = builder.Build();
app.Run();