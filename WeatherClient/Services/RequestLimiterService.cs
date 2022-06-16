using System;
using System.Collections.Concurrent;
using WeatherClient.Entities;
using WeatherClient.Interfaces;

namespace WeatherClient.Services;

public class RequestLimiterService : IRequestLimiterService
{
    private readonly ConcurrentDictionary<string, IpRequestsData> _requests = new();
    private readonly object _lock = new();

    public bool IsRequestValid(string clientIp, string path, RateLimiterConfiguration configuration)
    {
        var key = CreateRequestKey(clientIp, path);
        var currentTime = DateTime.Now;
        var result = true;
        if (_requests.TryGetValue(key, out var data))
        {
            lock (_lock)
            {
                if (data.TimeWindowStart + TimeSpan.FromSeconds(configuration.TimeWindowInSeconds) > currentTime)
                {
                    data.RequestsCount++;
                }
                else
                {
                    data.TimeWindowStart = currentTime;
                    data.RequestsCount = 1;
                }

                result = data.RequestsCount <= configuration.MaxRequests;
            }
        }
        else
        {
            _requests[key] = new IpRequestsData
            {
                RequestsCount = 1,
                TimeWindowStart = currentTime
            };
        }

        return result;
    }

    private static string CreateRequestKey(string ipAddress, string path) => $"{ipAddress}_{path}";

    private class IpRequestsData
    {
        public int RequestsCount { get; set; }
        public DateTime TimeWindowStart { get; set; }
    }
}