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

        // 1) prova a leggere EntityNameAttribute nelle metadata dell'endpoint
        string? entityName = null;

        try
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor?.EndpointMetadata;

            if (endpointMetadata != null)
            {
                var attr = endpointMetadata.FirstOrDefault(m => m.GetType().Name == "EntityNameAttribute");

                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("EntityName");
                    var val = prop?.GetValue(attr) as string;

                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        entityName = val;
                    }
                }
            }
        }
        catch { /* ignore */ }

        // 2) se non trovato, prova a dedurre dal route (previous behavior)
        if (entityName == null)
        {
            var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
            var firstSegment = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if (!string.IsNullOrEmpty(firstSegment))
            {
                var cleaned = firstSegment.Split('?').First();
                var candidate = cleaned.EndsWith("s", StringComparison.OrdinalIgnoreCase) && cleaned.Length > 1
                    ? cleaned.Substring(0, cleaned.Length - 1)
                    : cleaned;
                entityName = char.ToUpperInvariant(candidate[0]) + candidate.Substring(1);
            }
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            return;
        }

        var dto = metadata.GetEntityWhitelistAsync(entityName).GetAwaiter().GetResult();

        if (dto == null)
        {
            return;
        }

        foreach (var p in operation.Parameters)
        {
            if (string.Equals(p.Name, "sort", StringComparison.OrdinalIgnoreCase))
            {
                var aliases = dto.AliasInfos.Select(a => a.Alias).OrderBy(k => k).ToArray();
                var addition = aliases.Length == 0 ? "No sort aliases available." : $"Available sort aliases: {string.Join(", ", aliases)}";

                p.Description = string.IsNullOrWhiteSpace(p.Description) ? addition : $"{p.Description}\n\n{addition}";
            }

            if (string.Equals(p.Name, "include", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "includes", StringComparison.OrdinalIgnoreCase))
            {
                var includes = dto.AliasInfos.Select(a => a.Alias).OrderBy(k => k).ToArray();
                var addition = includes.Length == 0 ? "No include aliases available." : $"Available include aliases: {string.Join(", ", includes)} (comma separated).";

                p.Description = string.IsNullOrWhiteSpace(p.Description) ? addition : $"{p.Description}\n\n{addition}";
            }
        }
    }
}