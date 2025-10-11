using FileParserService.Models;
using FileParserService.Services;
using Shared.Config;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<XmlMonitorOptions>(builder.Configuration.GetSection("XmlMonitor"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<XmlMonitorService>();

var app = builder.Build();
app.Run();