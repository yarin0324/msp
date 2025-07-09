using MediatR;

namespace Common.MediatR.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Starting {typeof(TRequest).Name} at {DateTime.Now}");
            
            var response = await next();

            Console.WriteLine($"Finished {typeof(TRequest).Name} at {DateTime.Now}");

            return response;
        }
    }
}
