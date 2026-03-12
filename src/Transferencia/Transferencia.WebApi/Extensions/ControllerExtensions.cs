using Transferencia.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Transferencia.WebApi.Extensions;

public static class ControllerExtensions
{

    public static IActionResult ToActionResult(this ControllerBase controller, ApplicationResult result)
    {
        if (result.StatusCode == 204)
        {
            return controller.NoContent();
        }
        return controller.StatusCode(result.StatusCode, result.Envelope);
    }

}
