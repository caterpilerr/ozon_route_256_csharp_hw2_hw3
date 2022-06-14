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
        private readonly List<AmbientData> _currentAggregatingData = new();
        private DateTime? _currentAggregationInterval;
        private readonly TimeSpan _aggregationTimeSpan;

        public AggregatedAmbientDataStore(ILogger<IAggregatedAmbientDataStore> logger, IConfiguration configuration)
        {
            _logger = logger;
            _aggregationTimeSpan =
                TimeSpan.FromMinutes(int.Parse(configuration.GetSection("AmbientData:AggregationInterval").Value));
        }

        public void AddAmbientData(AmbientData data)
        {
            _logger.LogDebug(data.ToString());

            var aggregationInterval = RoundTo(data.CreatedAt, _aggregationTimeSpan);
            if (_currentAggregationInterval < aggregationInterval)
            {
                _currentAggregationInterval = aggregationInterval;
                _currentAggregatingData.Clear();
            }

            _currentAggregatingData.Add(data);

            if (_store.ContainsKey(data.SensorId))
            {
                var dataForSensor = _store[data.SensorId];
                if (dataForSensor.ContainsKey(aggregationInterval))
                {
                    dataForSensor[aggregationInterval] = UpdateAggregated(dataForSensor[aggregationInterval], data);
                }
                else
                {
                    dataForSensor[aggregationInterval] = CreateAggregation(data, aggregationInterval);
                }
            }
            else
            {
                _store[data.SensorId] = new Dictionary<DateTime, AggregatedAmbientData>
                {
                    [aggregationInterval] = CreateAggregation(data, aggregationInterval)
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

        private AggregatedAmbientData UpdateAggregated(AggregatedAmbientData aggregatedAmbientData, AmbientData data)
        {
            var accumulated = _currentAggregatingData.Where(x => x.SensorId == data.SensorId).ToArray();
            aggregatedAmbientData.AvgHumidity = accumulated.Average(x => x.Temperature);
            aggregatedAmbientData.AvgTemperature = accumulated.Average(x => x.Humidity);
            aggregatedAmbientData.MaxCo2 = Math.Max(aggregatedAmbientData.MaxCo2, data.Co2);
            aggregatedAmbientData.MinCo2 = Math.Min(aggregatedAmbientData.MinCo2, data.Co2);

            return aggregatedAmbientData;
        }

        private static AggregatedAmbientData Aggregate(ICollection<AggregatedAmbientData> data, DateTime time)
        {
            return new AggregatedAmbientData
            {
                SensorId = data.First().SensorId,
                SensorType = data.First().SensorType,
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
                SensorType = data.SensorType,
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