using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TinyRepository.Metadata.Interfaces;

namespace TinyRepository.Filters;

/// <summary>
/// An <see cref="IOperationFilter"/> that enriches OpenAPI operation parameter descriptions
/// with metadata-driven information (for example available sort and include aliases).
/// </summary>
/// <remarks>
/// This filter augments the Swagger/OpenAPI documentation for endpoints by appending
/// available alias information to request parameters named <c>sort</c>, <c>include</c> or <c>includes</c>.
///
/// Operation:
/// 1. If the operation has no parameters, the filter returns immediately.
/// 2. The filter attempts to discover the associated entity name:
///    a. First, it looks for an <c>EntityNameAttribute</c> in the endpoint metadata and reads its <c>EntityName</c> property.
///    b. If not found, it falls back to the first segment of the API relative path and converts it to a singular,
///       PascalCase entity name (previous behavior).
/// 3. If no entity name can be determined, the operation is left unchanged.
/// 4. If an entity name is discovered, the filter synchronously calls
///    <see cref="IMetadataService.GetEntityWhitelistAsync(string)"/> to obtain the whitelist DTO for the entity.
///    If the DTO is null, the operation is left unchanged.
/// 5. For each operation parameter:
///    - If the parameter name is <c>sort</c> (case-insensitive), the filter appends available sort aliases.
///    - If the parameter name is <c>include</c> or <c>includes</c> (case-insensitive), the filter appends available include aliases.
///
/// Notes:
/// - This implementation uses <c>GetAwaiter().GetResult()</c> to call an async metadata API synchronously; ensure the <see cref="IMetadataService"/>
///   is safe to be called in this manner and consider replacing with an asynchronous approach if appropriate.
/// - The filter is intended to be registered with Swashbuckle/SwaggerGen so that parameter descriptions in the generated
///   OpenAPI document include helpful alias information for API consumers.
/// </remarks>
/// <example>
/// <code>
/// // Example registration in Program.cs / Startup.cs:
/// services.AddSwaggerGen(c =>
/// {
///     c.OperationFilter&lt;MetadataOperationFilter&gt;();
/// });
/// </code>
/// </example>
/// <param name="metadata">The metadata service used to resolve entity whitelist and alias information.</param>
public class MetadataOperationFilter(IMetadataService metadata) : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null || operation.Parameters.Count == 0)
        {
            return;
        }

        // 1) try to read EntityNameAttribute from endpoint metadata
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

        // 2) if not found, try to deduce from the route (previous behavior)
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