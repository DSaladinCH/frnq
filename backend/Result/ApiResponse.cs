using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Result;

[ExcludeFromCodeCoverage]
public static class ApiResponses
{
    public static ApiResponseBuilder Created201 => new(ApiResponse.Create(CodeDescriptionModel.Created, HttpStatusCode.Created));
    public static ApiResponseBuilder EmptyFields400 => new(ApiResponse.Create(CodeDescriptionModel.EmptyFields, HttpStatusCode.BadRequest));
    public static ApiResponseBuilder InvalidFields400 => new(ApiResponse.Create(CodeDescriptionModel.InvalidFields, HttpStatusCode.BadRequest));
    public static ApiResponseBuilder Unauthorized401 => new(ApiResponse.Create(CodeDescriptionModel.Unauthorized, HttpStatusCode.Unauthorized));
    public static ApiResponseBuilder Forbidden403 => new(ApiResponse.Create(CodeDescriptionModel.Forbidden, HttpStatusCode.Forbidden));
    public static ApiResponseBuilder NotFound404 => new(ApiResponse.Create(CodeDescriptionModel.NotFound, HttpStatusCode.NotFound));
    public static ApiResponseBuilder Conflict409 => new(ApiResponse.Create(CodeDescriptionModel.Conflict, HttpStatusCode.Conflict));
    public static ApiResponseBuilder InternalServerError500 => new(ApiResponse.Create(CodeDescriptionModel.InternalError, HttpStatusCode.InternalServerError));
    public static ApiResponseBuilder Ok200 => new(ApiResponse.Create(CodeDescriptionModel.Ok, HttpStatusCode.OK));
    public static ApiResponseBuilder NoContent204 => new(ApiResponse.Create(CodeDescriptionModel.NoContent, HttpStatusCode.NoContent));
}

[ExcludeFromCodeCoverage]
[DebuggerDisplay("{Code}: {Description} (Statuscode: {StatusCode}) | Success: {Success}")]
public class ApiResponse : ActionResult
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    [JsonIgnore]
    public virtual bool Success
    {
        get { return (int)StatusCode is >= 200 and < 400; }
    }

    [JsonIgnore]
    public virtual bool Failed
    {
        get { return (int)StatusCode is >= 400; }
    }

    public async override Task ExecuteResultAsync(ActionContext context)
    {
        await GetResultAsync(context.HttpContext.Response);
    }

    public virtual async Task GetResultAsync(HttpResponse response)
    {
        response.ContentType = $"application/json; charset=utf-8";
        response.StatusCode = (int)StatusCode;

        if (response.StatusCode == StatusCodes.Status204NoContent)
            return;

        await response.WriteAsync(GetResponseValue(response.HttpContext, new CodeDescriptionModel(Code, Description)));
    }

    public static async Task<ApiResponse> ParseHttpResponseMessage(HttpResponseMessage response)
    {
        ApiResponse apiResponse = JsonSerializer.Deserialize<ApiResponse>(await response.Content.ReadAsStringAsync())!;
        apiResponse.StatusCode = response.StatusCode;
        return apiResponse;
    }

    public StatusCodeResult ToStatusCodeResult()
    {
        return new((int)StatusCode);
    }

    public string GetResponseValue(HttpContext context, object? returnValue)
    {
        string contentType = context.Request.GetTypedHeaders().Accept.ToString() ?? context.Request.GetTypedHeaders().Accept.ToString()!;
        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        string returnString;
        switch (contentType.ToLower())
        {
            default:
                contentType = "application/json";
                if (returnValue is not null)
                    returnString = JsonSerializer.Serialize(returnValue, jsonOptions);
                else
                    returnString = JsonSerializer.Serialize(new CodeDescriptionModel(Code, Description), jsonOptions);
                break;
        }

        return returnString;
    }

    public static ApiResponseBuilder Create(CodeDescriptionModel codeDescription, HttpStatusCode statusCode)
    {
        return new ApiResponseBuilder(new ApiResponse() { Code = codeDescription.Code, Description = codeDescription.Description, StatusCode = statusCode });
    }

    public static ApiResponseBuilder Create(string code, string description, HttpStatusCode statusCode)
    {
        return new ApiResponseBuilder(new ApiResponse() { Code = code, Description = description, StatusCode = statusCode });
    }

    public static ApiResponse<T> Create<T>(T? value, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create<T>(T? value, CodeDescriptionModel codeDescription, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { Code = codeDescription.Code, Description = codeDescription.Description, StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create<T>(T? value, string code, string description, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { Code = code, Description = description, StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create<T>(CodeDescriptionModel codeDescription, HttpStatusCode statusCode, T? value = default)
    {
        return new ApiResponse<T>() { Code = codeDescription.Code, Description = codeDescription.Description, StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create<T>(string code, string description, HttpStatusCode statusCode, T? value = default)
    {
        return new ApiResponse<T>() { Code = code, Description = description, StatusCode = statusCode, Value = value };
    }

    public bool IsResponse(ApiResponse responseToCompare)
    {
        return Code == responseToCompare.Code;
    }

    public ApiResponse FormatDescription(params string[] args)
    {
        ApiResponse response = new() { Code = Code, Description = Description, StatusCode = StatusCode };
        response.Description = string.Format(Description, args);
        return response;
    }
}

[ExcludeFromCodeCoverage]
[DebuggerDisplay("{Code}: {Description} (Statuscode: {StatusCode}) | Success: {Success}")]
public class ApiResponse<T> : ApiResponse
{
    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Value))]
    public override bool Success
    {
        get { return (int)StatusCode is >= 200 and < 400; }
    }

    [JsonIgnore]
    [MemberNotNullWhen(false, nameof(Value))]
    public override bool Failed
    {
        get { return (int)StatusCode is >= 400; }
    }

    //[NotNullIfNotNull(nameof(Value))]
    public T? Value { get; set; }

    public override async Task GetResultAsync(HttpResponse response)
    {
        response.ContentType = $"application/json; charset=utf-8";
        response.StatusCode = (int)StatusCode;
        await response.WriteAsync(GetResponseValue(response.HttpContext, Value));
    }

    public static implicit operator ApiResponse<T>(ApiResponseBuilder builder)
    {
        return new()
        {
            Code = builder.Response.Code,
            Description = builder.Response.Description,
            StatusCode = builder.Response.StatusCode,
            Value = default
        };
    }

    public new ApiResponse<T> FormatDescription(params string[] args)
    {
        ApiResponse<T> response = new() { Code = Code, Description = Description, StatusCode = StatusCode, Value = Value };
        response.Description = string.Format(Description, args);
        return response;
    }
}

[ExcludeFromCodeCoverage]
public class ApiResponseBuilder
{
    internal ApiResponse Response { get; }

    internal ApiResponseBuilder(ApiResponse response)
    {
        Response = response;
    }

    public static implicit operator ApiResponse(ApiResponseBuilder builder) => builder.Response;
}
