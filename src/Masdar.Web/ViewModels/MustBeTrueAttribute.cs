using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Masdar.Web.ViewModels;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public MustBeTrueAttribute()
    {
        ErrorMessage = "You must confirm that you can share this material.";
    }

    public override bool IsValid(object? value) => value is bool b && b;

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-mustbetrue", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        attributes.TryAdd(key, value);
    }
}
