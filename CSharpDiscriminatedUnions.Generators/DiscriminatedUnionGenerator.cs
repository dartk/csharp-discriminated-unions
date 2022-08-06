using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions.Generators;


[Generator]
public class DiscriminatedUnionGenerator : IIncrementalGenerator {

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider((node, _) => {
                    if (node is not AttributeSyntax attribute) {
                        return false;
                    }

                    return attribute.Name.ExtractName() is (Const.DiscriminatedUnion
                        or Const.DiscriminatedUnionAttribute);
                },
                (syntaxContext, token) => {
                    var attribute = (AttributeSyntax) syntaxContext.Node;
                    var classDeclaration = attribute.Parent?.Parent;
                    if (classDeclaration is not TypeDeclarationSyntax classDeclarationSyntax) {
                        return default;
                    }

                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not ITypeSymbol type) {
                        return default;
                    }

                    var casesType = type.GetTypeMembers("Union").SingleOrDefault();

                    return new DiscriminatedUnionTypeInfo(classDeclarationSyntax, type, casesType);
                })
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(provider, GenerateCode);
    }


    private static void GenerateCode(SourceProductionContext context,
        ImmutableArray<DiscriminatedUnionTypeInfo?> types) {
        foreach (var typeInfo in types) {
            if (typeInfo == null || typeInfo.CasesType == null) {
                continue;
            }

            context.AddSource($"{typeInfo.TypeSymbol.Name}.g.cs", GenerateUnionSource(typeInfo));
        }
    }


    private static string GenerateUnionSource(DiscriminatedUnionTypeInfo info) {
        var src = new StringBuilder();


        var (typeDecl, typeSymbol, casesType) = info;
        var typeIdentifierToken = typeDecl.ChildTokens().First(x => x.Kind() == SyntaxKind.IdentifierToken);
        var typeName = typeIdentifierToken.Text;
        var typeNameWithParameters = string.Join("",
            typeSymbol.ToDisplayParts().SkipWhile(x =>
                x.Kind is SymbolDisplayPartKind.NamespaceName or SymbolDisplayPartKind.Punctuation));

        var enumName = typeName + "Enum";

        var cases = casesType!
            .GetMembers()
            .Where(x => !x.IsImplicitlyDeclared && x.Kind is SymbolKind.Property)
            .Cast<IPropertySymbol>()
            .ToImmutableArray();

        if (cases.IsEmpty) {
            return "";
        }

        src.AppendLine("#nullable enable");
        var namespaces = cases
            .Select(x => x.Type)
            .Select(x => x.ContainingNamespace)
            .Where(x => !x.IsGlobalNamespace)
            .Select(x => x.ToString())
            .Append("CSharpDiscriminatedUnions")
            .Append("System")
            .Distinct();

        foreach (var item in namespaces) {
            src.AppendLine($"using {item};");
        }

        src.AppendLine();
        src.AppendLine();

        var typeHasNamespace = !typeSymbol.ContainingNamespace.IsGlobalNamespace;
        if (typeHasNamespace) {
            src.AppendLine($"namespace {typeSymbol.ContainingNamespace.ToDisplayString()} {{");
        }

        src.AppendLine();
        src.AppendLine();

        src.AppendLine($@"
public enum {enumName} {{
    {string.Join(Separator(1, ","), cases.Select((x, i) => $"{x.Name} = {i}"))}
}}
");

        var keywords = typeDecl.ChildTokens()
            .TakeWhile(x => x.Kind() != SyntaxKind.IdentifierToken)
            .Select(x => x.Text);

        src.AppendLine();
        src.AppendLine();
        src.AppendLine($"{string.Join(" ", keywords)} {typeNameWithParameters} {{");

        src.AppendLine($@"
    public {enumName} Case {{ get; }}
");
        src.AppendLine();

        foreach (var prop in cases) {
            src.AppendLine($"    private readonly {prop.Type.ToDisplayString()} _{prop.Name};");
        }
        src.AppendLine();
        
        src.AppendLine($@"
    private {typeName}(
        {enumName} @case,
        {string.Join(Separator(2, ","), cases.Select(x => $"{x.Type.ToDisplayString()} {x.Name}"))}
    ) {{
        this.Case = @case;
        {string.Join(Separator(2), cases.Select(x => $"this._{x.Name} = {x.Name};"))}
    }}
");

        foreach (var prop in cases) {
            src.AppendLine($@"
    public static {typeNameWithParameters} {prop.Name}({prop.Type.ToDisplayString()} {prop.Name}) {{
        return new {typeNameWithParameters}(
            {enumName}.{prop.Name},
            {string.Join(Separator(3, ","), cases.Select(x => x.Equals(prop) ? prop.Name : "default!"))}
        );
    }}
");
        }


        foreach (var prop in cases) {
            src.AppendLine($@"
    public bool TryGet{prop.Name}(out {prop.Type.ToDisplayString()} {prop.Name}) {{
        {prop.Name} = this._{prop.Name}!;
        return this.Case == {enumName}.{prop.Name};
    }}
");
        }

        foreach (var prop in cases) {
            src.AppendLine($@"
    public {prop.Type.ToDisplayString()} Get{prop.Name}() =>
        this.Is{prop.Name}
        ? this._{prop.Name}
        : throw new InvalidOperationException($""Cannot get {prop.Name} from {{this.Case}}"");
");
        }

        foreach (var prop in cases) {
            src.AppendLine($@"
    public bool Is{prop.Name} => this.Case == {enumName}.{prop.Name};
");
        }

        var hasToStringOverride =
            typeSymbol
                .GetMembers("ToString")
                .Any(member => member is IMethodSymbol { IsOverride: true, IsImplicitlyDeclared: false });

        if (!hasToStringOverride) {
            src.AppendLine($@"
    public override string ToString() {{
        return this.Switch(
            {string.Join(Separator(3, ","), cases.Select(x => $@"{x.Name}: value => $""{x.Name}({{value}})"""))}
        );
    }}
");
        }


        src.Append($@"
    public T Switch<T>(
        Func<{typeNameWithParameters}, T> Default,
        {string.Join(Separator(2, ","), cases.Select(x => $"Func<{x.Type.ToDisplayString()}, T>? {x.Name} = null"))}
    ) {{
        switch (this.Case) {{
");

        foreach (var prop in cases) {
            src.AppendLine(
                $"            case {enumName}.{prop.Name}: return {prop.Name} != null ? {prop.Name}(this._{prop.Name}!) : Default(this);");
        }

        src.AppendLine("            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
        src.AppendLine("        }");
        src.AppendLine("    }");
        src.AppendLine();

        src.Append($@"
    public void Do(
        Action<{typeNameWithParameters}> Default,
        {string.Join(Separator(2, ","), cases.Select(x => $"Action<{x.Type.ToDisplayString()}>? {x.Name} = null"))}
    ) {{
        switch (this.Case) {{
");

        foreach (var prop in cases) {
            src.AppendLine(
                $"            case {enumName}.{prop.Name}: if ({prop.Name} != null) {prop.Name}(this._{prop.Name}!); else Default(this); return;");
        }

        src.AppendLine("            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
        src.AppendLine("        }");
        src.AppendLine("    }");
        src.AppendLine();

        src.Append($@"
    public T Switch<T>(
        {string.Join(Separator(2, ","), cases.Select(x => $"Func<{x.Type.ToDisplayString()}, T> {x.Name}"))}
    ) {{
        switch (this.Case) {{
");

        foreach (var prop in cases) {
            src.AppendLine($"            case {enumName}.{prop.Name}: return {prop.Name}(this._{prop.Name}!);");
        }

        src.AppendLine("            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
        src.AppendLine("        }");
        src.AppendLine("    }");
        src.AppendLine();


        src.Append($@"
    public void Do(
        {string.Join(Separator(2, ","), cases.Select(x => $"Action<{x.Type.ToDisplayString()}> {x.Name}"))}
    ) {{
        switch (this.Case) {{
");

        foreach (var prop in cases) {
            src.AppendLine($"            case {enumName}.{prop.Name}: {prop.Name}(this._{prop.Name}!); return;");
        }

        src.AppendLine("            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
        src.AppendLine("        }");
        src.AppendLine("    }");
        src.AppendLine();


        string Separator(int offset, string separator = "") {
            var builder = new StringBuilder();
            builder.AppendLine(separator);
            for (var i = 0; i < offset; ++i) {
                builder.Append("    ");
            }

            return builder.ToString();
        }


        src.AppendLine("}");
        if (typeHasNamespace) {
            src.AppendLine();
            src.AppendLine();
            src.AppendLine("}");
        }

        return src.ToString();
    }

}