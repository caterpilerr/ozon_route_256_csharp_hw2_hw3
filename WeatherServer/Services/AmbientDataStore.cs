using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WeatherServer.Entities;
using WeatherServer.Interfaces;

namespace WeatherServer.Services
{
    public class AmbientDataStore : IAmbientDataStore
    {
        private readonly Dictionary<int, AmbientData> _sensorsData = new();
        private readonly ILogger<AmbientDataStore> _logger;

        public event IAmbientDataStore.NewAmbientDataHandler NewAmbientData;

        public AmbientDataStore(ILogger<AmbientDataStore> logger)
        {
            _logger = logger;
        }
        
        public void AddAmbientData(IEnumerable<AmbientData> data)
        {
            foreach (var ambientData in data)
            {
                _logger.LogDebug(ambientData.ToString());
                _sensorsData[ambientData.SensorId] = ambientData;
                NewAmbientData?.Invoke(ambientData);
            }
        }

        public AmbientData? GetLastDataFromSensor(int id)
        {
            if (_sensorsData.ContainsKey(id))
            {
                return _sensorsData[id];
            }

            return null;
        }
    }
}