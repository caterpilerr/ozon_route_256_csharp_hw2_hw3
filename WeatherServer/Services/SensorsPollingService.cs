using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WeatherServer.Interfaces;

namespace WeatherServer.Services
{
    public class SensorsPollingService : BackgroundService
    {
        private readonly int _pollingFrequency;
        private const int MinimumPollingFrequency = 100;
        private const int MaximumPollingFrequency = 2000;
        private readonly ISensorAccessor _sensorAccessor;
        private readonly IAmbientDataStore _ambientDataStore;
        
        public SensorsPollingService(ISensorAccessor sensorAccessor, IAmbientDataStore ambientDataStore, IConfiguration configuration)
        {
            _sensorAccessor = sensorAccessor;
            _ambientDataStore = ambientDataStore;
            var inputPollingFrequency = int.Parse(configuration.GetSection("AmbientData:Sensors:PollingFrequency").Value);
            _pollingFrequency = inputPollingFrequency switch
            {
                > MaximumPollingFrequency => MaximumPollingFrequency,
                < MinimumPollingFrequency => MinimumPollingFrequency,
                _ => inputPollingFrequency,
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var receivedData = await _sensorAccessor.GetDataFromAllSensors();
                _ambientDataStore.AddAmbientData(receivedData);

                await Task.Delay(_pollingFrequency, CancellationToken.None);
            }
        }
    }
}