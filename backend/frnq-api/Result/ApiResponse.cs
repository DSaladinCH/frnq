using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace DSaladin.Frnq.Api.Result;

public partial class ApiResponses
{
    public static ApiResponse Created201 => ApiResponse.Create(CodeDescriptionModel.Created, HttpStatusCode.Created);
    public static ApiResponse EmptyFields400 => ApiResponse.Create(CodeDescriptionModel.EmptyFields, HttpStatusCode.BadRequest);
    public static ApiResponse InvalidFields400 => ApiResponse.Create(CodeDescriptionModel.InvalidFields, HttpStatusCode.BadRequest);
    public static ApiResponse Unauthorized401 => ApiResponse.Create(CodeDescriptionModel.Unauthorized, HttpStatusCode.Unauthorized);
    public static ApiResponse Forbidden403 => ApiResponse.Create(CodeDescriptionModel.Forbidden, HttpStatusCode.Forbidden);
    public static ApiResponse NotFound404 => ApiResponse.Create(CodeDescriptionModel.NotFound, HttpStatusCode.NotFound);
    public static ApiResponse Conflict409 => ApiResponse.Create(CodeDescriptionModel.Conflict, HttpStatusCode.Conflict);
    public static ApiResponse InternalServerError500 => ApiResponse.Create(CodeDescriptionModel.InternalError, HttpStatusCode.InternalServerError);
}

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
        string returnString = "{}";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        switch (contentType.ToLower())
        {
            //case "application/xml":
            //    using (StringWriter stringwriter = new())
            //    {
            //        new XmlSerializer(returnValue.GetType()).Serialize(stringwriter, returnValue);
            //        returnString = stringwriter.ToString();
            //    }
            //    break;
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

    public static ApiResponse Create(CodeDescriptionModel codeDescription, HttpStatusCode statusCode)
    {
        return new ApiResponse() { Code = codeDescription.Code, Description = codeDescription.Description, StatusCode = statusCode };
    }

    public static ApiResponse Create(string code, string description, HttpStatusCode statusCode)
    {
        return new ApiResponse() { Code = code, Description = description, StatusCode = statusCode };
    }

    public ApiResponse<T> Convert<T>(T? value = default)
    {
        return new()
        {
            Code = Code,
            Description = Description,
            StatusCode = StatusCode,
            Value = value
        };
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

    public static ApiResponse<T> Create(T? value, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create(T? value, CodeDescriptionModel codeDescription, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { Code = codeDescription.Code, Description = codeDescription.Description, StatusCode = statusCode, Value = value };
    }

    public static ApiResponse<T> Create(T? value, string code, string description, HttpStatusCode statusCode)
    {
        return new ApiResponse<T>() { Code = code, Description = description, StatusCode = statusCode, Value = value };
    }
}