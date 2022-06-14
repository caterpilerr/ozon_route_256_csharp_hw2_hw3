using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WeatherServer.Contracts;
using WeatherServer.Interfaces;
using AmbientData = WeatherServer.Contracts.AmbientData;
using Enum = System.Enum;

namespace WeatherServer.Services
{
    public class AmbientDataMonitorService : AmbientDataMonitor.AmbientDataMonitorBase
    {
        private static readonly object Lock = new();

        private readonly ConcurrentDictionary<int, HashSet<IServerStreamWriter<AmbientData>>> _sensorSubscriptions =
            new();

        public AmbientDataMonitorService(IAmbientDataStore ambientDataStore)
        {
            ambientDataStore.NewAmbientData += OnNewAmbientDataReceiving;
        }
        
        public override async Task GetDataForSensor(AmbientDataRequest request,
            IServerStreamWriter<AmbientData> responseStream, ServerCallContext context)
        {
            Subscribe(request.SensorId, responseStream);

            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200);
            }

            Unsubscribe(request.SensorId, responseStream); 
        }

        public override async Task SubscribeForSensorData(IAsyncStreamReader<SensorSubscriptionRequest> requestStream, IServerStreamWriter<AmbientData> responseStream,
            ServerCallContext context)
        {
            var subscribedId = new HashSet<int>();
            while (await requestStream.MoveNext())
            {
                var request = requestStream.Current;
                if (request.Unsubscribe)
                {
                    Unsubscribe(request.SensorId, responseStream);
                    subscribedId.Remove(request.SensorId);
                }
                else
                {
                    Subscribe(request.SensorId, responseStream);
                    subscribedId.Add(request.SensorId);
                }
            }

            foreach (var sensorId in subscribedId)
            {
                Unsubscribe(sensorId, responseStream);
            }
        }

        private void Subscribe(int sensorId, IServerStreamWriter<AmbientData> stream)
        {
            _sensorSubscriptions.AddOrUpdate(sensorId,
                new HashSet<IServerStreamWriter<AmbientData>> { stream }, (_, existing) =>
                {
                    lock (Lock)
                    {
                        existing.Add(stream);
                    }

                    return existing;
                }); 
        }

        private void Unsubscribe(int sensorId, IServerStreamWriter<AmbientData> stream)
        {
            if (_sensorSubscriptions.TryGetValue(sensorId, out var set))
            {
                lock (Lock)
                {
                    set.Remove(stream);
                }
            } 
        }

        private async void OnNewAmbientDataReceiving(Entities.AmbientData data)
        {
            var response = new AmbientData
            {
                SensorId = data.SensorId,
                SensorType = Enum.Parse<AmbientData.Types.SensorType>(data.SensorType.ToString()),
                Temperature = data.Temperature,
                Humidity = data.Humidity,
                Co2 = data.Co2,
                CreatedAt = Timestamp.FromDateTime(data.CreatedAt)
            };

            if (!_sensorSubscriptions.TryGetValue(data.SensorId, out var subscribers))
            {
                return;
            }

            IServerStreamWriter<AmbientData>[] streams;
            lock (Lock)
            {
                streams = subscribers.ToArray();
            }

            foreach (var stream in streams)
            {
                await stream.WriteAsync(response);
            }
        }
    }
}