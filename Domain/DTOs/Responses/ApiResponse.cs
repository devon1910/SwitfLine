using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.Responses
{
    public class Result<T>
    {
        [JsonPropertyName("data")] public T? Data { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
        [JsonPropertyName("status")] public bool Status { get; init; }
        [JsonIgnore] public int StatusCode { get; init; }

        public static Result<T> Ok(T? data, string message = "Operation completed successfully")
            => new()
            {
                Data = data,
                Message = message,
                Status = true,
                StatusCode = 200,
            };

        public static Result<T> Created(T? data, string message = "Operation completed successfully")
            => new()
            {
                Data = data,
                Message = message,
                Status = true,
                StatusCode = 201,
            };

        public static Result<T> Failed(string message = "Bad Request", T? data = default)
            => new()
            {
                Data = data,
                Message = message,
                Status = false,
                StatusCode = 400,
            };

        public static Result<T> NotFound(string message = "Resource not found", T? data = default) =>
            new()
            {
                Data = data,
                Message = message,
                Status = false,
                StatusCode = 404,
            };

        public static Result<T> InternalError(string message = "An error occured please try again later",
            T? data = default)
            => new()
            {
                Data = data,
                Message = message,
                Status = false,
                StatusCode = 500,
            };

        public static Result<T> ClientCancelled(string message = "Your request has been cancelled", T? data = default)
            => new()
            {
                Message = message,
                Data = data,
                Status = false,
                StatusCode = 499,
            };

        public static Result<T> InternalError(Exception ex, T? data = default)
            => new()
            {
                Data = data,
                Message = ex.Message,
                Status = false,
                StatusCode = 500
            };

        public static Result<T> Unauthorized(string message = "Unauthorized", T? data = default)
            => new()
            {
                Data = data,
                Message = message,
                Status = false,
                StatusCode = 401,
            };

        public static Result<T> Forbidden(string message = "You do not have the permission to view this resource",
            T? data = default)
            => new()
            {
                Data = data,
                Message = message,
                Status = false,
                StatusCode = 403,
            };
    }
}
