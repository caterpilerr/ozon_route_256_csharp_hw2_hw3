using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherClient.Entities;
using WeatherClient.Interfaces;
using AmbientData = WeatherClient.Entities.AmbientData;

namespace WeatherClient.Services
{
    public class AggregatedAmbientDataStore : IAggregatedAmbientDataStore
    {
        private readonly ILogger<IAggregatedAmbientDataStore> _logger;
        private readonly Dictionary<int, Dictionary<DateTime, AggregatedAmbientData>> _store = new();
        private readonly TimeSpan _aggregationInterval;

        public AggregatedAmbientDataStore(ILogger<IAggregatedAmbientDataStore> logger, IConfiguration configuration)
        {
            _logger = logger;
            _aggregationInterval = TimeSpan.FromMinutes(int.Parse(configuration.GetSection("AmbientData:AggregationInterval").Value));
        }

        public void AddAmbientData(AmbientData data)
        {
            _logger.LogDebug(data.ToString());

            var roundedTime = RoundTo(data.CreatedAt, _aggregationInterval);
            if (_store.ContainsKey(data.SensorId))
            {
                var dataForSensor = _store[data.SensorId];
                if (dataForSensor.ContainsKey(roundedTime))
                {
                    dataForSensor[roundedTime] = AddToAggregated(dataForSensor[roundedTime], data);
                }
                else
                {
                    dataForSensor[roundedTime] = CreateAggregation(data, roundedTime);
                }
            }
            else
            {
                _store[data.SensorId] = new Dictionary<DateTime, AggregatedAmbientData>
                {
                    [roundedTime] = CreateAggregation(data, roundedTime)
                };
            }
        }

        public AggregatedAmbientData? GetAggregationForInterval(int sensorId, DateTime startTime, DateTime endTime)
        {
            if (!_store.ContainsKey(sensorId))
            {
                return null;
            }

            var startTimeUtc = startTime.ToUniversalTime();
            var endTimeUtc = endTime.ToUniversalTime();

            var found = _store[sensorId].Values.Where(x => x.Time >= startTimeUtc && x.Time <= endTimeUtc).ToArray();
            
            return found.Length == 0 ? null : Aggregate(found, startTime);
        }

        public List<AggregatedAmbientData> GetAllData(int sensorId)
        {
            if (!_store.ContainsKey(sensorId))
            {
                return null;
            }

            var result = _store[sensorId].Values.ToList();

            return result;
        }

        private static AggregatedAmbientData AddToAggregated(AggregatedAmbientData aggregatedAmbientData,
            AmbientData data)
        {
            aggregatedAmbientData.AvgHumidity = (aggregatedAmbientData.AvgHumidity + data.Humidity) / 2;
            aggregatedAmbientData.AvgTemperature = (aggregatedAmbientData.AvgTemperature + data.Temperature) / 2;
            aggregatedAmbientData.MaxCo2 = Math.Max(aggregatedAmbientData.MaxCo2, data.Co2);
            aggregatedAmbientData.MinCo2 = Math.Min(aggregatedAmbientData.MinCo2, data.Co2);

            return aggregatedAmbientData;
        }
        
        private static AggregatedAmbientData Aggregate(ICollection<AggregatedAmbientData> data, DateTime time)
        {
            return new AggregatedAmbientData
            {
                SensorId = data.First().SensorId,
                AvgTemperature = data.Average(x => x.AvgTemperature),
                AvgHumidity = data.Average(x => x.AvgHumidity),
                MinCo2 = data.Min(x => x.MinCo2),
                MaxCo2 = data.Max(x => x.MaxCo2),
                Time = time
            };
        }

        private static AggregatedAmbientData CreateAggregation(AmbientData data, DateTime time)
        {
            return new AggregatedAmbientData
            {
                SensorId = data.SensorId,
                AvgTemperature = data.Temperature,
                AvgHumidity = data.Humidity,
                MinCo2 = data.Co2,
                MaxCo2 = data.Co2,
                Time = time
            };
        }

        private static DateTime RoundTo(DateTime dateTime, TimeSpan roundInterval)
        {
            var ticks = dateTime.Ticks / roundInterval.Ticks;
            return new DateTime(ticks * roundInterval.Ticks);
        }
    }
}