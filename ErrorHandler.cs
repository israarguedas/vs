using MIBServiceFunctionApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp
{
    public static class ErrorHandler
    {
        public static IActionResult HandleException(Exception ex)
        {
            var errorResponse = new ErrorResponse
            {
                ErrorCode = "500",
                ErrorMessage = "An unexpected error occurred.",
                Details = ex.Message
            };

            if (ex is FileNotFoundException)
            {
                errorResponse.ErrorCode = "404";
                errorResponse.ErrorMessage = "MIB file not found.";
            }
            else if (ex is UnauthorizedAccessException)
            {
                errorResponse.ErrorCode = "401";
                errorResponse.ErrorMessage = "Unauthorized access.";
            }
            else if (ex is ArgumentException)
            {
                errorResponse.ErrorCode = "400";
                errorResponse.ErrorMessage = "Bad request.";
            }

            return new ObjectResult(errorResponse)
            {
                StatusCode = int.Parse(errorResponse.ErrorCode)
            };
        }
    }

}
