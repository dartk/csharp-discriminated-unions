using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions.Generators;


[Generator]
public class DiscriminatedUnionGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider(IsDiscriminatedUnionAttr, GetUnionTypeInfo)
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(provider, GenerateCode);

        return;


        static bool IsDiscriminatedUnionAttr(
            SyntaxNode node, CancellationToken _
        ) {
            if (node is not AttributeSyntax attribute) {
                return false;
            }

            return attribute.Name.ExtractName()
                is Const.DiscriminatedUnion
                or Const.DiscriminatedUnionAttribute;
        }


        static DiscriminatedUnionTypeInfo? GetUnionTypeInfo(
            GeneratorSyntaxContext syntaxContext, CancellationToken _
        ) {
            var attribute = (AttributeSyntax)syntaxContext.Node;
            if (
                attribute.Parent?.Parent is not TypeDeclarationSyntax typeSyntax
            ) {
                return null;
            }

            var symbol = syntaxContext
                .SemanticModel
                .GetDeclaredSymbol(typeSyntax);

            if (symbol is not ITypeSymbol typeSymbol) {
                return null;
            }

            var members = typeSymbol.GetMembers();
            if (members.IsEmpty) {
                return null;
            }

            var caseInfoBuilder = ImmutableArray.CreateBuilder<UnionCaseInfo>(
                members.Length
            );

            foreach (var member in members) {
                if (!IsUnionCaseMethod(member, out var method)) {
                    continue;
                }

                var caseInfo = CreateUnionCaseInfo(method);
                caseInfoBuilder.Add(caseInfo);
            }

            return new DiscriminatedUnionTypeInfo(
                GetNamespace(typeSymbol),
                typeSymbol.Name,
                GetUniqueTypeName(typeSymbol),
                GetTypeNameWithParameters(typeSymbol),
                GetTypeDeclarationKeywords(typeSyntax),
                caseInfoBuilder.ToImmutable()
            );
        }

        static bool IsUnionCaseMethod(
            ISymbol symbol, out IMethodSymbol methodSymbol
        ) {
            if (
                symbol.IsStatic
                && symbol is IMethodSymbol {
                    IsPartialDefinition: true,
                    TypeParameters.IsEmpty: true
                } method
                && method.GetAttributes().Any(x =>
                    x.AttributeClass?.ToDisplayString() ==
                    Const.CaseAttributeClass)
            ) {
                methodSymbol = method;
                return true;
            }
            else {
                methodSymbol = null!;
                return false;
            }
        }


        static ImmutableArray<UnionCaseParameterInfo> GetUnionParametersInfo(
            string prefix,
            ImmutableArray<IParameterSymbol> parameters
        ) {
            var builder = ImmutableArray.CreateBuilder<UnionCaseParameterInfo>(
                parameters.Length
            );

            foreach (var parameter in parameters) {
                var type = parameter.Type.ToDisplayString();
                var name = parameter.Name;
                builder.Add(new UnionCaseParameterInfo(type, name, $"{prefix}_{name}"));
            }

            return builder.MoveToImmutable();
        }


        static UnionCaseInfo CreateUnionCaseInfo(IMethodSymbol method) {
            return new UnionCaseInfo(
                method.Name,
                method.ReturnType.ToDisplayString(),
                GetUnionParametersInfo(method.Name, method.Parameters)
            );
        }


        static string? GetNamespace(ISymbol type) {
            return type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString();
        }


        static string GetTypeDeclarationKeywords(SyntaxNode syntax) {
            return string.Join(
                " ",
                syntax
                    .ChildTokens()
                    .TakeWhile(x => x.Kind() != SyntaxKind.IdentifierToken)
                    .Select(x => x.Text)
            );
        }

        static string GetTypeNameWithParameters(ISymbol symbol) {
            return string.Join(
                "",
                symbol
                    .ToDisplayParts()
                    .SkipWhile(x =>
                        x.Kind
                            is SymbolDisplayPartKind.NamespaceName
                            or SymbolDisplayPartKind.Punctuation
                    )
            );
        }


        static string GetUniqueTypeName(
            ISymbol symbol
        ) {
            return symbol.ToDisplayString()
                .Replace('<', '[')
                .Replace('>', ']');
        }
    }


    private static void GenerateCode(
        SourceProductionContext context,
        ImmutableArray<DiscriminatedUnionTypeInfo?> types
    ) {
        foreach (var typeInfo in types) {
            if (typeInfo == null) {
                continue;
            }

            const string templateFile = "DiscriminatedUnion.scriban";
            var generated = ScribanTemplate.Render(
                templateFile, new { TypeInfo = typeInfo }
            );
            
            context.AddSource(
                $"{typeInfo.UniqueName}.g.cs",
                generated
            );
        }
    }


//     private static string GenerateUnionSource(DiscriminatedUnionTypeInfo info) {
//         var src = new StringBuilder();
//
//         var (typeDecl, typeSymbol, casesType) = info;
//
//         var typeIdentifierToken = typeDecl
//             .ChildTokens()
//             .First(x => x.Kind() == SyntaxKind.IdentifierToken);
//
//         var typeName = typeIdentifierToken.Text;
//         var typeNameWithParameters = string.Join(
//             "",
//             typeSymbol
//                 .ToDisplayParts()
//                 .SkipWhile(x =>
//                     x.Kind
//                         is SymbolDisplayPartKind.NamespaceName
//                         or SymbolDisplayPartKind.Punctuation
//                 )
//         );
//
//         var enumName = typeName + "Enum";
//
//         var cases = casesType!
//             .GetMembers()
//             .Where(x =>
//                 !x.IsImplicitlyDeclared && x.Kind is SymbolKind.Property
//             )
//             .Cast<IPropertySymbol>()
//             .ToImmutableArray();
//
//         if (cases.IsEmpty) {
//             return "";
//         }
//
//         src.AppendLine("#nullable enable");
//         var namespaces = cases
//             .Select(x => x.Type)
//             .Select(x => x.ContainingNamespace)
//             .Where(x => !x.IsGlobalNamespace)
//             .Select(x => x.ToString())
//             .Append("CSharpDiscriminatedUnions")
//             .Append("System")
//             .Distinct();
//
//         foreach (var item in namespaces) {
//             src.AppendLine($"using {item};");
//         }
//
//         src.AppendLine();
//         src.AppendLine();
//
//         var typeHasNamespace =
//             !typeSymbol.ContainingNamespace.IsGlobalNamespace;
//         if (typeHasNamespace) {
//             src.AppendLine(
//                 $"namespace {typeSymbol.ContainingNamespace.ToDisplayString()} {{");
//         }
//
//         src.AppendLine();
//         src.AppendLine();
//
//         src.AppendLine($@"
// public enum {enumName} {{
//     {string.Join(Separator(1, ","), cases.Select((x, i) => $"{x.Name} = {i}"))}
// }}
// ");
//
//         var keywords = typeDecl.ChildTokens()
//             .TakeWhile(x => x.Kind() != SyntaxKind.IdentifierToken)
//             .Select(x => x.Text);
//
//         src.AppendLine();
//         src.AppendLine();
//         src.AppendLine(
//             $"{string.Join(" ", keywords)} {typeNameWithParameters} {{");
//
//         src.AppendLine($@"
//     public {enumName} Case {{ get; }}
// ");
//         src.AppendLine();
//
//         foreach (var prop in cases) {
//             src.AppendLine(
//                 $"    private readonly {prop.Type.ToDisplayString()} _{prop.Name};");
//         }
//
//         src.AppendLine();
//
//         src.AppendLine($@"
//     private {typeName}(
//         {enumName} @case,
//         {string.Join(Separator(2, ","), cases.Select(x => $"{x.Type.ToDisplayString()} {x.Name}"))}
//     ) {{
//         this.Case = @case;
//         {string.Join(Separator(2), cases.Select(x => $"this._{x.Name} = {x.Name};"))}
//     }}
// ");
//
//         foreach (var prop in cases) {
//             src.AppendLine($@"
//     public static {typeNameWithParameters} {prop.Name}({prop.Type.ToDisplayString()} {prop.Name}) {{
//         return new {typeNameWithParameters}(
//             {enumName}.{prop.Name},
//             {string.Join(Separator(3, ","), cases.Select(x => x.Equals(prop) ? prop.Name : "default!"))}
//         );
//     }}
// ");
//         }
//
//
//         foreach (var prop in cases) {
//             src.AppendLine($@"
//     public bool TryGet{prop.Name}(out {prop.Type.ToDisplayString()} {prop.Name}) {{
//         {prop.Name} = this._{prop.Name}!;
//         return this.Case == {enumName}.{prop.Name};
//     }}
// ");
//         }
//
//         foreach (var prop in cases) {
//             src.AppendLine($@"
//     public {prop.Type.ToDisplayString()} Get{prop.Name}() =>
//         this.Is{prop.Name}
//         ? this._{prop.Name}
//         : throw new InvalidOperationException($""Cannot get {prop.Name} from {{this.Case}}"");
// ");
//         }
//
//         foreach (var prop in cases) {
//             src.AppendLine($@"
//     public bool Is{prop.Name} => this.Case == {enumName}.{prop.Name};
// ");
//         }
//
//         var hasToStringOverride =
//             typeSymbol
//                 .GetMembers("ToString")
//                 .Any(member => member is IMethodSymbol {
//                     IsOverride: true, IsImplicitlyDeclared: false
//                 });
//
//         if (!hasToStringOverride) {
//             src.AppendLine($@"
//     public override string ToString() {{
//         return this.Switch(
//             {string.Join(Separator(3, ","), cases.Select(x => $@"{x.Name}: value => $""{x.Name}({{value}})"""))}
//         );
//     }}
// ");
//         }
//
//
//         src.Append($@"
//     public T Switch<T>(
//         Func<{typeNameWithParameters}, T> Default,
//         {string.Join(Separator(2, ","), cases.Select(x => $"Func<{x.Type.ToDisplayString()}, T>? {x.Name} = null"))}
//     ) {{
//         switch (this.Case) {{
// ");
//
//         foreach (var prop in cases) {
//             src.AppendLine(
//                 $"            case {enumName}.{prop.Name}: return {prop.Name} != null ? {prop.Name}(this._{prop.Name}!) : Default(this);");
//         }
//
//         src.AppendLine(
//             "            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
//         src.AppendLine("        }");
//         src.AppendLine("    }");
//         src.AppendLine();
//
//         src.Append($@"
//     public void Do(
//         Action<{typeNameWithParameters}> Default,
//         {string.Join(Separator(2, ","), cases.Select(x => $"Action<{x.Type.ToDisplayString()}>? {x.Name} = null"))}
//     ) {{
//         switch (this.Case) {{
// ");
//
//         foreach (var prop in cases) {
//             src.AppendLine(
//                 $"            case {enumName}.{prop.Name}: if ({prop.Name} != null) {prop.Name}(this._{prop.Name}!); else Default(this); return;");
//         }
//
//         src.AppendLine(
//             "            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
//         src.AppendLine("        }");
//         src.AppendLine("    }");
//         src.AppendLine();
//
//         src.Append($@"
//     public T Switch<T>(
//         {string.Join(Separator(2, ","), cases.Select(x => $"Func<{x.Type.ToDisplayString()}, T> {x.Name}"))}
//     ) {{
//         switch (this.Case) {{
// ");
//
//         foreach (var prop in cases) {
//             src.AppendLine(
//                 $"            case {enumName}.{prop.Name}: return {prop.Name}(this._{prop.Name}!);");
//         }
//
//         src.AppendLine(
//             "            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
//         src.AppendLine("        }");
//         src.AppendLine("    }");
//         src.AppendLine();
//
//
//         src.Append($@"
//     public void Do(
//         {string.Join(Separator(2, ","), cases.Select(x => $"Action<{x.Type.ToDisplayString()}> {x.Name}"))}
//     ) {{
//         switch (this.Case) {{
// ");
//
//         foreach (var prop in cases) {
//             src.AppendLine(
//                 $"            case {enumName}.{prop.Name}: {prop.Name}(this._{prop.Name}!); return;");
//         }
//
//         src.AppendLine(
//             "            default: throw new ArgumentOutOfRangeException($\"Invalid union case '{this.Case}'\");");
//         src.AppendLine("        }");
//         src.AppendLine("    }");
//         src.AppendLine();
//
//
//         string Separator(int offset, string separator = "") {
//             var builder = new StringBuilder();
//             builder.AppendLine(separator);
//             for (var i = 0; i < offset; ++i) {
//                 builder.Append("    ");
//             }
//
//             return builder.ToString();
//         }
//
//
//         src.AppendLine("}");
//         if (typeHasNamespace) {
//             src.AppendLine();
//             src.AppendLine();
//             src.AppendLine("}");
//         }
//
//         return src.ToString();
//     }
}
