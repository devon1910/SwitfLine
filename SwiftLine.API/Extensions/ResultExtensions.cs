using Domain.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace SwiftLine.API.Extensions
{
    public static class ResultExtensions
    {
        public static ActionResult<Result<T>> ToActionResult<T>(this Result<T> result)
        {
            return new ObjectResult(result)
            {
                StatusCode = result.StatusCode
            };
        }
    }
}
