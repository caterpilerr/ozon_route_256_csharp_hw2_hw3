using System;

namespace WeatherClient.Entities
{
    public record struct AggregatedAmbientData
    {
        public int SensorId { get; set; }
        public SensorType SensorType { get; set; }
        public float AvgTemperature { get; set; }
        public float AvgHumidity { get; set; }
        public int MinCo2 { get; set; }
        public int MaxCo2 { get; set; }
        public DateTime Time { get; set; }
    }
}