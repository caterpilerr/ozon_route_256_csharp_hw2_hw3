using WeatherClient.Entities;

namespace WeatherClient.Interfaces;

public interface IRequestLimiterService
{
    public bool IsRequestValid(string clientIp, string endpoint, RateLimiterConfiguration configuration);
}