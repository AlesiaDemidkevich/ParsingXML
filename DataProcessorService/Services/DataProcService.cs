using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Config;
using Shared.Models;
using System.Text;
using System.Text.Json;

namespace DataProcessorService.Services 
{
    public class DataProcService: BackgroundService
    {
        private readonly ILogger<DataProcService> _logger;
        private readonly RabbitMqOptions _opts;
        private readonly string _connString;
        private IConnection? _conn;
        private IModel? _ch;

        public DataProcService(ILogger<DataProcService> logger, IOptions<RabbitMqOptions> rmq, IConfiguration config)
        {
            _logger = logger;
            _opts = rmq.Value;
            _connString = config.GetConnectionString("Sqlite") ?? "Data Source=data.db";
            EnsureDb();
        }

        private void EnsureDb()
        {
            using var connection = new SqliteConnection(_connString);
            connection.Open();
            new SqliteCommand(
                "CREATE TABLE IF NOT EXISTS ModuleStates (ModuleCategoryID INTEGER PRIMARY KEY, ModuleState TEXT NOT NULL)", connection
            ).ExecuteNonQuery();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _opts.HostName,
                Port = _opts.Port,
                UserName = _opts.UserName,
                Password = _opts.Password
            };

            _conn = factory.CreateConnection();
            _ch = _conn.CreateModel();

            _ch.ExchangeDeclare(_opts.Exchange, "direct", durable: true);
            _ch.QueueDeclare(_opts.Queue, durable: true, exclusive: false, autoDelete: false);
            _ch.QueueBind(_opts.Queue, _opts.Exchange, _opts.RoutingKey);

            var consumer = new EventingBasicConsumer(_ch);
            consumer.Received += (s, e) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(e.Body.ToArray());
                    var msg = JsonSerializer.Deserialize<FileMessage>(json);
                    if (msg != null)
                    {
                        SaveModules(msg.Modules);
                    }
                    _ch.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Message processing failed");
                    _ch.BasicNack(e.DeliveryTag, false, false);
                }
            };

            _ch.BasicConsume(_opts.Queue, false, consumer);
            return Task.CompletedTask;
        }

        private void SaveModules(IEnumerable<ModuleModel> modules)
        {
            using var connection = new SqliteConnection(_connString);
            connection.Open();

            foreach (var module in modules)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO ModuleSates(ModuleCategoryID, ModuleState)
                VALUES($id, $state)
                ON CONFLICT(ModuleCategoryID) DO UPDATE SET ModuleState = $state;";
                cmd.Parameters.AddWithValue("$id", module.ModuleCategoryID);
                cmd.Parameters.AddWithValue("$state", module.ModuleState.ToString());
                cmd.ExecuteNonQuery();
            }

            _logger.LogInformation("Updated {count} modules", modules.Count());
        }
    }
}
