using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WeatherClient.Attributes;
using WeatherClient.Entities;
using WeatherClient.Interfaces;

namespace WeatherClient.Middlewares;

public class RateLimiterMiddleware
{
    private const int DefaultMaxRequests = 5;
    private const int DefaultTimeWindowInSeconds = 1;
    private readonly RequestDelegate _next;
    private readonly IRequestLimiterService _limiterService;
    private readonly Dictionary<string, RateLimiterConfiguration> _clientLimits = new();
    private readonly RateLimiterConfiguration _globalRateLimits = new()
    {
        MaxRequests = DefaultMaxRequests,
        TimeWindowInSeconds = DefaultTimeWindowInSeconds
    };

    public RateLimiterMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IRequestLimiterService limiterService)
    {
        _next = next;
        _limiterService = limiterService;

        var globalSettings = configuration.GetSection("RateLimiter:Global").Get<RateLimiterConfiguration>();
        if (globalSettings is not null)
        {
            _globalRateLimits = globalSettings;
        }

        var clientLimits = configuration.GetSection("RateLimiter:Clients")
            .Get<Dictionary<string, RateLimiterConfiguration>>();
        
        if (clientLimits is not null)
        {
            foreach (var limit in clientLimits)
            {
                _clientLimits.Add(limit.Key, limit.Value);
            }
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress;
        if (clientIp == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var endpoint = context.GetEndpoint();
        var clientIp4 = clientIp.MapToIPv4().ToString();
        var isValidRequest = ValidateRequest(clientIp4, endpoint);
        if (!isValidRequest)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await _next(context);
    }

    private bool ValidateRequest(string ipAddress, Endpoint endpoint)
    {
        var endpointName = endpoint.DisplayName;
        if (_clientLimits.TryGetValue(ipAddress, out var limit))
        {
            return _limiterService.IsRequestValid(ipAddress, endpointName, limit);
        }

        var rateLimiterAttribute = endpoint?.Metadata.GetMetadata<RateLimiterAttribute>();

        if (rateLimiterAttribute is not null)
        {
            var limitsFromAttribute = new RateLimiterConfiguration
            {
                MaxRequests = rateLimiterAttribute.MaxRequests,
                TimeWindowInSeconds = rateLimiterAttribute.TimeWindowInSeconds
            };

            return _limiterService.IsRequestValid(ipAddress, endpointName, limitsFromAttribute);
        }

        return _limiterService.IsRequestValid(ipAddress, endpointName, _globalRateLimits);
    }
}