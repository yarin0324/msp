using System.Net;
using System.Text.Json;
using OrderService.Domain.Exceptions;
using Serilog;
using ArgumentNullException = OrderService.Domain.Exceptions.ArgumentNullException;

namespace OrderService.WebApi.Middleware.Exception
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (System.Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, System.Exception exception)
        {
            // Default
            var code = HttpStatusCode.InternalServerError;
            var errorResponse = new ErrorResponse
            {
                IsSuccess = false,
                ErrorCode = nameof(HttpStatusCode.InternalServerError),
                ErrorMessage = "An unexpected error occurred.",
                Details = exception.Message
            };

            switch (exception)
            {
                case InvalidOrderAmountException invalidOrderAmountException:
                    code = HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "InvalidOrderAmount";
                    errorResponse.ErrorMessage = "Invalid Order Amount";
                    errorResponse.Details = invalidOrderAmountException.Message;
                    break;
                case ArgumentNullException argumentNullException:
                    code = HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "ArgumentNull";
                    errorResponse.ErrorMessage = "Invalid request data";
                    errorResponse.Details = argumentNullException.Message;
                    break;
                default:
                    Log.Error(exception, $"Unexpected error: {exception}");
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

            return context.Response.WriteAsync(result);
        }
    }
}
