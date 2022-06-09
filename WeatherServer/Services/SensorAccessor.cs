using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeatherServer.Entities;
using WeatherServer.Interfaces;

namespace WeatherServer.Services
{
    public class SensorAccessor : ISensorAccessor
    {
        private readonly Random _rand = new();
        private const int MedianOutsideTemperature = 15;
        private const int MedianOutsideHumidity = 65;
        private const int MedianOutsideCo2Level = 350;
        private const int MedianIndoorTemperature = 22;
        private const int MedianIndoorHumidity = 45;
        private const int MedianIndoorCo2Level = 800;

        private readonly int[] _sensorIds =
        {
            1,
            2,
            3
        };

        public async Task<IEnumerable<AmbientData>> GetDataFromAllSensors()
        {
            var result = _sensorIds.Select(sensorId => sensorId switch
            {
                1 => GenerateDataForOutsideSensor(1),
                2 => GenerateDataForIndoorSensor(2),
                3 => GenerateDataForIndoorSensor(3),
                _ => throw new ArgumentException()
            });
                

            return await Task.FromResult(result);
        }

        private AmbientData GenerateDataForOutsideSensor(int id)
        {
            return new AmbientData
            {
                SensorId = id,
                Temperature = MedianOutsideTemperature + MedianOutsideTemperature * _rand.Next(-10, 10) / 100.0f,
                Humidity = MedianOutsideHumidity + MedianOutsideHumidity * _rand.Next(-15, 15) / 100.0f,
                Co2 = MedianOutsideCo2Level + (int)(MedianOutsideCo2Level * _rand.Next(-5, 5) / 100.0f),
                CreatedAt = DateTime.UtcNow
            };
        }

        private AmbientData GenerateDataForIndoorSensor(int id)
        {
            return new AmbientData
            {
                SensorId = id,
                Temperature = MedianIndoorTemperature + MedianIndoorTemperature * _rand.Next(-5, 5) / 100.0f,
                Humidity = MedianIndoorHumidity + MedianIndoorHumidity * _rand.Next(-10, 10) / 100.0f,
                Co2 = MedianIndoorCo2Level + (int)(MedianIndoorCo2Level * _rand.Next(-20, 20) / 100.0),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}