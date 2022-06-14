using System.Threading.Tasks;

namespace WeatherClient.Interfaces;

public interface IAmbientDataStreamService
{
    public Task SendRequest(int sensorId, bool unsubscribe);
}