using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PayFlow.Api.Filters;

/// <summary>
/// Action filter that runs FluentValidation validators before the action executes.
/// Returns 400 with validation errors if validation fails.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator == null) continue;

            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Type = "https://payflow.dev/errors/validation",
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                context.Result = new BadRequestObjectResult(problemDetails);
                return;
            }
        }

        await next();
    }
}
