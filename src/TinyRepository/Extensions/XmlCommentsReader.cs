using System.Xml.Linq;

namespace TinyRepository.Extensions;

public static class XmlCommentsReader
{
    public static string? GetPropertySummary(Type type, string propertyName)
    {
        try
        {
            var asm = type.Assembly;
            var xmlPath = Path.ChangeExtension(asm.Location, ".xml");

            if (!File.Exists(xmlPath))
            {
                return null;
            }

            var doc = XDocument.Load(xmlPath);
            // Member name for property: "P:Namespace.TypeName.PropertyName"
            var memberName = $"P:{type.FullName}.{propertyName}";
            var member = doc.Root?
                .Element("members")?
                .Elements("member")
                .FirstOrDefault(m => string.Equals((string)m.Attribute("name") ?? string.Empty, memberName, StringComparison.Ordinal));

            var summary = member?.Element("summary")?.Value?.Trim();

            return string.IsNullOrWhiteSpace(summary) ? null : summary;
        }
        catch
        {
            return null;
        }
    }
}
