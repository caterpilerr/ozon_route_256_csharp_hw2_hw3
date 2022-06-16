using WeatherClient.Entities;

namespace WeatherClient.Interfaces;

public interface IRequestLimiterService
{
    public bool IsRequestValid(string clientIp, string path, RateLimiterConfiguration configuration);
}