using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherServer.Entities;

namespace WeatherServer.Interfaces
{
    public interface ISensorAccessor
    {
        public Task<IEnumerable<AmbientData>> GetDataFromAllSensors();
    }
}