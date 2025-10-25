using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DSaladin.Frnq.Api.ModelBinders;

public class FlexibleDateTimeBinder : IModelBinder
{
	public Task BindModelAsync(ModelBindingContext context)
	{
		var value = context.ValueProvider.GetValue(context.ModelName).FirstValue;

		if (string.IsNullOrEmpty(value))
		{
			context.Result = ModelBindingResult.Failed();
			return Task.CompletedTask;
		}

		DateTime parsed;

		// Try ISO 8601 or culture-specific formats
		if (DateTime.TryParse(value, out parsed))
		{
			context.Result = ModelBindingResult.Success(parsed);
			return Task.CompletedTask;
		}

		// Try Unix timestamp (seconds since epoch)
		if (long.TryParse(value, out var unix))
		{
			try
			{
				parsed = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
				context.Result = ModelBindingResult.Success(parsed);
				return Task.CompletedTask;
			}
			catch
			{
				// fall through
			}
		}

		context.Result = ModelBindingResult.Failed();
		return Task.CompletedTask;
	}
}