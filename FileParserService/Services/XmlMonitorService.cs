using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Config;
using Shared.Models;
using FileParserService.Models;

namespace FileParserService.Services
    {

    public class XmlMonitorService : BackgroundService
    {
        private readonly ILogger<XmlMonitorService> _logger;
        private readonly XmlMonitorOptions _options;
        private readonly RabbitMqOptions _rmqOptions;
        private readonly Random _rnd = new();


        public XmlMonitorService(ILogger<XmlMonitorService> logger, IOptions<XmlMonitorOptions> opts, IOptions<RabbitMqOptions> rmqOpts)
        {
            _logger = logger;
            _options = opts.Value;
            _rmqOptions = rmqOpts.Value;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("XML Monitor Service started. Folder: {folder}", _options.FolderPath);
            Directory.CreateDirectory(_options.FolderPath);


            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = Directory.GetFiles(_options.FolderPath, "*.xml");
                    var tasks = new List<Task>();


                    foreach (var file in files)
                    {
                        // Обрабатываем каждый файл в отдельном потоке
                        tasks.Add(Task.Run(() => ProcessFile(file), stoppingToken));
                    }

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scanning");
                }


                await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
            }
        }

        private void ProcessFile(string path)
        {
            _logger.LogInformation("Processing {file}", path);

            try
            {
                var serializer = new XmlSerializer(typeof(InstrumentStatus));
                using var fs = File.OpenRead(path);
                var instrument = (InstrumentStatus?)serializer.Deserialize(fs);
                if (instrument == null || instrument.DeviceStatuses.Count == 0)
                {
                    _logger.LogWarning("No modules in {file}", path);
                    return;
                }

                var modules = new List<ModuleModel>();
                foreach (var device in instrument.DeviceStatuses)
                {
                    string moduleState = "NotReady";

                    try
                    {
                        var innerXml = System.Net.WebUtility.HtmlDecode(device.RapidControlStatus);
                        object? statusObj = device.ModuleCategoryID switch
                        {
                            "SAMPLER" => DeserializeInnerXml<CombinedSamplerStatus>(innerXml),
                            "QUATPUMP" => DeserializeInnerXml<CombinedPumpStatus>(innerXml),
                            "COLCOMP" => DeserializeInnerXml<CombinedOvenStatus>(innerXml),
                            _ => null
                        };

                        if (statusObj != null)
                            moduleState = statusObj.GetType().GetProperty("ModuleState")?.GetValue(statusObj)?.ToString() ?? "NotReady";
                    }
                    catch { moduleState = "NotReady"; }

                    modules.Add(new ModuleModel
                    {
                        ModuleCategoryID = device.ModuleCategoryID.GetHashCode(),
                        ModuleName = device.ModuleCategoryID,
                        ModuleState = Enum.TryParse<ModuleState>(moduleState, true, out var st) ? st : ModuleState.NotReady
                    });
                }

                var fileMsg = new FileMessage
                {
                    FileName = Path.GetFileName(path),
                    Modules = modules
                };

                var json = JsonSerializer.Serialize(fileMsg);
                PublishToRabbit(json);

                _logger.LogInformation("Published message from {file}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process {file}", path);
            }
        }

        private T? DeserializeInnerXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return (T?)serializer.Deserialize(reader);
        }

        private void PublishToRabbit(string json)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rmqOptions.HostName,
                Port = _rmqOptions.Port,
                UserName = _rmqOptions.UserName,
                Password = _rmqOptions.Password
            };

            using var connection = factory.CreateConnection();
            using var ch = connection.CreateModel();

            ch.ExchangeDeclare(_rmqOptions.Exchange, ExchangeType.Direct, durable: true);
            ch.QueueDeclare(_rmqOptions.Queue, durable: true, exclusive: false, autoDelete: false);
            ch.QueueBind(_rmqOptions.Queue, _rmqOptions.Exchange, _rmqOptions.RoutingKey);

            var body = Encoding.UTF8.GetBytes(json);
            ch.BasicPublish(_rmqOptions.Exchange, _rmqOptions.RoutingKey, null, body);

        }

        private ModuleState RandomState()
        {
            var vals = Enum.GetValues(typeof(ModuleState)).Cast<ModuleState>().ToArray();
            return vals[_rnd.Next(vals.Length)];
        }
    }
}