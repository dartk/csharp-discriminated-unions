using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;


namespace CSharpDiscriminatedUnions.Generators;


public abstract class IncrementalGeneratorBase<T> : IIncrementalGenerator
{
    private const string ScribanTemplatesFolderResource =
        "CSharpDiscriminatedUnions.Generators.ScribanTemplates";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider(this.Choose, this.Select)
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(provider, this.Produce!);
    }


    protected abstract bool Choose(SyntaxNode node, CancellationToken token);
    protected abstract T? Select(GeneratorSyntaxContext context, CancellationToken token);


    protected abstract void Produce(
        SourceProductionContext context, ImmutableArray<T> items);


    protected static string GetTemplate(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{ScribanTemplatesFolderResource}.{fileName}");
        if (stream == null)
        {
            throw new ArgumentException($"Template '{fileName}' is not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}