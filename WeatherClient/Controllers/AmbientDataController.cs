using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WeatherClient.Attributes;
using WeatherClient.Entities;
using WeatherClient.Interfaces;

namespace WeatherClient.Controllers
{
    [Route("sensor/{id:int}")]
    public class AmbientDataController : ControllerBase
    {
        private readonly IAggregatedAmbientDataStore _dataStore;
        private readonly IAmbientDataStreamService _dataStream;
        private readonly ISensorSubscriptionStore _sensorSubscriptionStore;

        public AmbientDataController(
            IAggregatedAmbientDataStore dataStore,
            IAmbientDataStreamService streamService,
            ISensorSubscriptionStore sensorSubscriptionStore)
        {
            _dataStore = dataStore;
            _dataStream = streamService;
            _sensorSubscriptionStore = sensorSubscriptionStore;
        }

        [HttpGet("aggregate")]
        public ActionResult<AggregatedAmbientData> GetAggregatedData(
            int id,
            [FromQuery]DateTime startTime,
            [FromQuery]DateTime endTime)
        {
            var result = _dataStore.GetAggregationForInterval(id, startTime, endTime);

            return result;
        }
        
        [HttpGet("all")]
        [RateLimiter(MaxRequests = 1, TimeWindowInSeconds = 10)]
        public ActionResult<List<AggregatedAmbientData>> GetAllData(int id)
        {
            var result = _dataStore.GetAllData(id);

            return result;
        }

        [HttpGet("subscribe")]
        public async Task<IActionResult> SubscribeForSensor(int id)
        {
            await _dataStream.SendRequest(id, unsubscribe: false);
            _sensorSubscriptionStore.Add(id);

            return Ok();
        }
        
        [HttpGet("unsubscribe")]
        public async Task<IActionResult> UnsubscribeForSensor(int id)
        {
            await _dataStream.SendRequest(id, unsubscribe: true);
            _sensorSubscriptionStore.Remove(id);

            return Ok();
        }
    }
}