using Http = Microsoft.AspNetCore.Http;

namespace TennisClub.Api.Common.Results;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        return result.Status switch
        {
            ResultStatus.Success =>
                onSuccess is not null && result.Value is not null
                    ? onSuccess(result.Value)
                    : Http.Results.Ok(result.Value),

            ResultStatus.Unauthorized =>
                Http.Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized),

            ResultStatus.NotFound =>
                Http.Results.NotFound(new { error = result.Error }),

            ResultStatus.Invalid =>
                Http.Results.BadRequest(new { error = result.Error, failures = result.Failures }),

            ResultStatus.Conflict =>
                Http.Results.Conflict(new { error = result.Error }),

            _ => Http.Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    public static IResult ToHttpResult(this Result result)
    {
        return result.Status switch
        {
            ResultStatus.Success => Http.Results.NoContent(),
            ResultStatus.Unauthorized =>
                Http.Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized),
            ResultStatus.NotFound => Http.Results.NotFound(new { error = result.Error }),
            ResultStatus.Invalid =>
                Http.Results.BadRequest(new { error = result.Error, failures = result.Failures }),
            ResultStatus.Conflict => Http.Results.Conflict(new { error = result.Error }),
            _ => Http.Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
