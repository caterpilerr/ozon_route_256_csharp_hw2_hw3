using System.Collections.Generic;
using WeatherServer.Entities;

namespace WeatherServer.Interfaces
{
    public interface IAmbientDataStore
    {
        public delegate void NewAmbientDataHandler(AmbientData data);
        public event NewAmbientDataHandler NewAmbientData;
        public void AddAmbientData(IEnumerable<AmbientData> data);
        public AmbientData? GetLastDataFromSensor(int id); 
    }
}