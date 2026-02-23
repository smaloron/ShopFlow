namespace OrderService.Application.Behaviors;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[MediatR] ▶ Début : {RequestName}", requestName);
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("[MediatR] ✅ Fin : {RequestName} en {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[MediatR] ❌ Erreur : {RequestName} en {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
