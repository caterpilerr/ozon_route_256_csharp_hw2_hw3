using System;

namespace WeatherClient.Entities
{
    public record struct AmbientData
    {
        public int SensorId { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public int Co2 { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}