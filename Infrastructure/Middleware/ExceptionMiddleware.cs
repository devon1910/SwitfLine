using Domain.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _log;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
        {
            _log = log;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var message = $"An error occured please try again later";

            string error = string.Format("Oops!!!  => Context => {0} => {1} | {2} | {3}", context?.Request?.Path.Value, exception?.Message, exception?.InnerException?.InnerException, exception?.StackTrace);

            var result = Result<string>.InternalError(message);

            _log.LogInformation("Oops!!!  => Context => {0} => {1} | {2} | {3}", context?.Request?.Path.Value, exception?.Message, exception?.InnerException?.InnerException, exception?.StackTrace);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return context.Response.WriteAsync(JsonConvert.SerializeObject(result));
        }

    }
}

