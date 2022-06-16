using System;

namespace WeatherClient.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RateLimiterAttribute : Attribute
{
    public int MaxRequests { get; set; }
    public int TimeWindowInSeconds { get; set; }
}