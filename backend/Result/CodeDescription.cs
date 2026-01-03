using System.ComponentModel.DataAnnotations;

namespace DSaladin.Frnq.Api.Result;

public class CodeDescriptionModel
{
	/// <summary>
	/// The unique identification code
	/// </summary>
	[Required]
	public string Code { get; set; } = string.Empty;
	/// <summary>
	/// A more detailed description
	/// </summary>
	[Required]
	public string Description { get; set; } = string.Empty;

	public CodeDescriptionModel(string code)
	{
		Code = code;
		Description = "";
	}

	public CodeDescriptionModel(string code, string description)
	{
		Code = code.ToUpper();
		Description = description;
	}

	public CodeDescriptionModel(string code, string description, params string[] descriptionArgs)
	{
		Code = code.ToUpper();
		Description = string.Format(description, descriptionArgs);
	}

	public static CodeDescriptionModel Created => new("created", "The record(s) have been created");
	public static CodeDescriptionModel EmptyFields => new("request_emptyfields", "Not all request parameters are fulfilled.");
	public static CodeDescriptionModel InvalidFields => new("request_invalidfields", "Not all request parameters are valid.");
	public static CodeDescriptionModel Unauthorized => new("unauthenticated", "You are not unauthenticated, please authenticate first.");
	public static CodeDescriptionModel Forbidden => new("forbidden", "You are not authorized to perform this operation.");
	public static CodeDescriptionModel NotFound => new("notfound", "The resource you tried to access was not found.");
	public static CodeDescriptionModel Conflict => new("conflict", "The request could not be completed due to a conflict with the current state of the resource.");
	public static CodeDescriptionModel InternalError => new("internalservererror", "There was a internal server error. Please try again later.");
    public static CodeDescriptionModel Ok => new("ok", "The request was successful.");
    public static CodeDescriptionModel NoContent => new("nocontent", "The request was successful but there is no content to return.");
}