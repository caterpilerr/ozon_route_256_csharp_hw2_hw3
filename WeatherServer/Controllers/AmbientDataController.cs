using Microsoft.AspNetCore.Mvc;
using WeatherServer.Entities;
using WeatherServer.Interfaces;

namespace WeatherServer.Controllers
{
    [Route("[controller]")]
    public class AmbientDataController : ControllerBase
    {
        private readonly IAmbientDataStore _ambientDataStore;
        
        public AmbientDataController(IAmbientDataStore ambientDataStore)
        {
            _ambientDataStore = ambientDataStore;
        }
        
        [HttpGet]
        public ActionResult<AmbientData> Get([FromQuery] int sensorId)
        {
            var data =_ambientDataStore.GetLastDataFromSensor(sensorId);

            return data;
        }
    }
}