using System.Collections.Generic;

namespace WeatherClient.Interfaces;

public interface ISensorSubscriptionStore
{
   public void Add(int sensorId);
   public void Remove(int sensorId);
   public IEnumerable<int> GetAll();
}