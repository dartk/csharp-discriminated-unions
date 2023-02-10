using System.IO;
using Scriban;


namespace CSharpDiscriminatedUnions;


public static class ScribanTemplate
{
    public static Template Parse(params string[] filePath)
    {
        var templateSrc = ManifestResource.ReadAllText(filePath);
        return Template.Parse(templateSrc, Path.Combine(filePath));
    }
}