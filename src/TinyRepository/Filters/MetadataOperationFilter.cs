using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TinyRepository.Metadata.Interfaces;

namespace TinyRepository.Filters;

public class MetadataOperationFilter(IMetadataService metadata) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null || operation.Parameters.Count == 0)
        {
            return;
        }

        // try to infer entity name from route template: take first segment (e.g. "articles" => "Article")
        var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
        var firstSegment = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        if (string.IsNullOrEmpty(firstSegment))
        {
            return;
        }

        // remove route parameters, e.g. "articles/{id}" -> "articles"
        var cleaned = firstSegment.Split('?').First();

        // naive singularize: remove trailing 's' if present; fallback to capitalized
        var candidate = cleaned.EndsWith("s", StringComparison.OrdinalIgnoreCase) && cleaned.Length > 1
            ? cleaned.Substring(0, cleaned.Length - 1) : cleaned;

        // capitalize
        var entityName = char.ToUpperInvariant(candidate[0]) + candidate.Substring(1);

        // fetch metadata (synchronously via .Result is okay at startup; but to avoid blocking use .GetAwaiter().GetResult())
        var dto = metadata.GetEntityWhitelistAsync(entityName).GetAwaiter().GetResult();

        if (dto == null)
        {
            return;
        }

        // find sort/include parameters in operation and append description of aliases
        foreach (var p in operation.Parameters)
        {
            if (string.Equals(p.Name, "sort", StringComparison.OrdinalIgnoreCase))
            {
                var aliases = dto.Aliases.Keys.OrderBy(k => k).ToArray();
                var addition = aliases.Length == 0 ? "No sort aliases available." : $"Available sort aliases: {string.Join(", ", aliases)}";

                p.Description = string.IsNullOrWhiteSpace(p.Description) ? addition : $"{p.Description}\n\n{addition}";
            }

            if (string.Equals(p.Name, "include", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "includes", StringComparison.OrdinalIgnoreCase))
            {
                var includes = dto.Aliases.Keys.OrderBy(k => k).ToArray(); // alias map contains include aliases too
                var addition = includes.Length == 0 ? "No include aliases available." : $"Available include aliases: {string.Join(", ", includes)} (comma separated)";

                p.Description = string.IsNullOrWhiteSpace(p.Description) ? addition : $"{p.Description}\n\n{addition}";
            }
        }
    }
}