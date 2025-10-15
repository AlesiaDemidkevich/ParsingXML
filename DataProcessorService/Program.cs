using DataProcessorService.Services;
using Shared.Config;

using SQLitePCL;

Batteries_V2.Init();

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddHostedService<DataProcService>();

var app = builder.Build();
app.Run();