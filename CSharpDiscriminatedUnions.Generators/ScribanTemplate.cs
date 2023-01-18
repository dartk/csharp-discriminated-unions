using Scriban;


namespace CSharpDiscriminatedUnions.Generators;


public class ScribanTemplate
{

    private const string TemplateFolder = "ScribanTemplates";


    public static string Render(string file, object? model = null)
    {
        var templateSrc = ManifestResource.ReadAllText(TemplateFolder, file);
        var template = Template.Parse(templateSrc, file);
        return template.Render(model, memberInfo => memberInfo.Name);
    }

}
