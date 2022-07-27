namespace WeatherClient.Entities;

public class RateLimiterConfiguration
{
    public int MaxRequests { get; set; }
    public int TimeWindowInSeconds { get; set; } 
}