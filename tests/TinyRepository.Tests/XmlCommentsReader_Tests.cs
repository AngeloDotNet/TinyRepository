using System.Reflection;
using TinyRepository.Extensions;
using TinyRepository.Tests.Models;

namespace TinyRepository.Tests;

public class XmlCommentsReader_Tests
{
    // Create a small temp XML doc next to the test assembly to test XmlCommentsReader fallback
    [Fact]
    public void XmlCommentsReader_Reads_Property_Summary_From_TemporaryXml()
    {
        var asm = Assembly.GetExecutingAssembly();
        var xmlPath = Path.ChangeExtension(asm.Location, ".xml");

        // create a minimal xml doc with a property summary for the Article.Title property (if exists)
        var typeFullName = typeof(Article).FullName;
        if (typeFullName == null)
        {
            return;
        }

        var memberName = $"P:{typeFullName}.Title";
        var xml = $@"<?xml version=""1.0""?>
<doc>
  <members>
    <member name=""{memberName}"">
      <summary>Test summary from temporary xml</summary>
    </member>
  </members>
</doc>";
        try
        {
            File.WriteAllText(xmlPath, xml);

            var summary = XmlCommentsReader.GetPropertySummary(typeof(Article), "Title");
            Assert.Equal("Test summary from temporary xml", summary);
        }
        finally
        {
            try
            {
                File.Delete(xmlPath);
            }
            catch { }
        }
    }
}