using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherClient.Entities;
using WeatherClient.Interfaces;
using WeatherServer.Contracts;
using static System.Threading.Tasks.Task;
using AmbientData = WeatherClient.Entities.AmbientData;

namespace WeatherClient.Services
{
    public class AmbientDataStreamService : BackgroundService, IAmbientDataStreamService
    {
        private const int ConnectionRetryDelay = 1000;
        private readonly ISensorSubscriptionStore _sensorSubscriptionStore;
        private readonly IAggregatedAmbientDataStore _dataStore;
        private readonly ILogger<AmbientDataStreamService> _logger;
        private IClientStreamWriter<SensorSubscriptionRequest> _currentStreamWriter;

        public AmbientDataStreamService(
            IAggregatedAmbientDataStore dataStore,
            ISensorSubscriptionStore sensorSubscriptionStore,
            ILogger<AmbientDataStreamService> logger)
        {
            _dataStore = dataStore;
            _sensorSubscriptionStore = sensorSubscriptionStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new AmbientDataMonitor.AmbientDataMonitorClient(channel);
            var call = client.SubscribeForSensorData(cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _currentStreamWriter = call.RequestStream;
                    foreach (var sensorId in _sensorSubscriptionStore.GetAll())
                    {
                        await SendRequest(sensorId, false);
                    }

                    while (await call.ResponseStream.MoveNext(stoppingToken))
                    {
                        var data = call.ResponseStream.Current;
                        var model = new AmbientData
                        {
                            SensorId = data.SensorId,
                            SensorType = Enum.Parse<SensorType>(data.SensorType.ToString()),
                            Temperature = data.Temperature,
                            Humidity = data.Humidity,
                            Co2 = data.Co2,
                            CreatedAt = data.CreatedAt.ToDateTime()
                        };

                        _dataStore.AddAmbientData(model);
                    }
                }
                catch (RpcException)
                {
                    _currentStreamWriter = null;
                    call.Dispose();
                    await Delay(ConnectionRetryDelay, CancellationToken.None);
                    call = client.SubscribeForSensorData(cancellationToken: stoppingToken);
                }
            }

            await call.RequestStream.CompleteAsync();
            call.Dispose();
        }

        public async Task SendRequest(int sensorId, bool unsubscribe)
        {
            var request = new SensorSubscriptionRequest
            {
                SensorId = sensorId,
                Unsubscribe = unsubscribe
            };

            await _currentStreamWriter.WriteAsync(request);
            _logger.LogDebug($"Request sent: {request}");
        }
    }
}