using System;
using System.Collections.Generic;
using WeatherClient.Entities;

namespace WeatherClient.Interfaces
{
    public interface IAggregatedAmbientDataStore
    {
        void AddAmbientData(AmbientData data);
        AggregatedAmbientData? GetAggregationForInterval(int sensorId, DateTime startTime, DateTime endTime);
        List<AggregatedAmbientData> GetAllData(int sensorId);
    }
}