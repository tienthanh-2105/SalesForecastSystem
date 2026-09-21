using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;

namespace SalesForecastSystem.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromServiceResult<T>(
        ServiceResult<T> result,
        Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Error!;
        if (error.Type == ServiceErrorType.Validation)
        {
            ModelState.AddModelError(error.FieldName ?? string.Empty, error.Message);
            return ValidationProblem(ModelState);
        }

        var statusCode = error.Type switch
        {
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: error.Message);
    }
}
