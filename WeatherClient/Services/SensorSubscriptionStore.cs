using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using WeatherClient.Interfaces;

namespace WeatherClient.Services;

public class SensorSubscriptionStore : ISensorSubscriptionStore
{
    private readonly ConcurrentDictionary<int, bool> _store = new()
    {
        [1] = true,
        [2] = true
    };

    public void Add(int sensorId)
    {
        _store.TryAdd(sensorId, true);
    }

    public void Remove(int sensorId)
    {
        _store.Remove(sensorId, out _);
    }

    public IEnumerable<int> GetAll()
    {
        return _store.Keys.ToArray();
    }
}