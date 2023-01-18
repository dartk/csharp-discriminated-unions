using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions.Generators;


[Generator]
public class DiscriminatedUnionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider(IsDiscriminatedUnionAttr, GetUnionTypeInfo)
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(provider, GenerateCode);

        return;


        static bool IsDiscriminatedUnionAttr(
            SyntaxNode node, CancellationToken _
        )
        {
            if (node is not AttributeSyntax attribute)
            {
                return false;
            }

            return attribute.Name.ExtractName()
                is Const.DiscriminatedUnion
                or Const.DiscriminatedUnionAttribute;
        }


        static DiscriminatedUnionTypeInfo? GetUnionTypeInfo(
            GeneratorSyntaxContext syntaxContext, CancellationToken _
        )
        {
            var attribute = (AttributeSyntax)syntaxContext.Node;
            if (
                attribute.Parent?.Parent is not TypeDeclarationSyntax typeSyntax
            )
            {
                return null;
            }

            var symbol = syntaxContext
                .SemanticModel
                .GetDeclaredSymbol(typeSyntax);

            if (symbol is not ITypeSymbol typeSymbol)
            {
                return null;
            }

            var members = typeSymbol.GetMembers();
            if (members.IsEmpty)
            {
                return null;
            }

            var caseInfoBuilder = ImmutableArray.CreateBuilder<UnionCaseInfo>(
                members.Length
            );

            foreach (var member in members)
            {
                if (!IsUnionCaseMethod(member, out var method))
                {
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
        )
        {
            if (
                symbol.IsStatic
                && symbol is IMethodSymbol
                {
                    IsPartialDefinition: true,
                    TypeParameters.IsEmpty: true
                } method
                && method.GetAttributes().Any(x =>
                    x.AttributeClass?.ToDisplayString() ==
                    Const.CaseAttributeClass)
            )
            {
                methodSymbol = method;
                return true;
            }
            else
            {
                methodSymbol = null!;
                return false;
            }
        }


        static ImmutableArray<UnionCaseParameterInfo> GetUnionParametersInfo(
            string prefix,
            ImmutableArray<IParameterSymbol> parameters
        )
        {
            var builder = ImmutableArray.CreateBuilder<UnionCaseParameterInfo>(
                parameters.Length
            );

            foreach (var parameter in parameters)
            {
                var type = parameter.Type.ToDisplayString();
                var name = parameter.Name;
                builder.Add(new UnionCaseParameterInfo(type, name, $"{prefix}_{name}"));
            }

            return builder.MoveToImmutable();
        }


        static UnionCaseInfo CreateUnionCaseInfo(IMethodSymbol method)
        {
            return new UnionCaseInfo(
                method.Name,
                method.ReturnType.ToDisplayString(),
                GetUnionParametersInfo(method.Name, method.Parameters)
            );
        }


        static string? GetNamespace(ISymbol type)
        {
            return type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString();
        }


        static string GetTypeDeclarationKeywords(SyntaxNode syntax)
        {
            return string.Join(
                " ",
                syntax
                    .ChildTokens()
                    .TakeWhile(x => x.Kind() != SyntaxKind.IdentifierToken)
                    .Select(x => x.Text)
            );
        }

        static string GetTypeNameWithParameters(ISymbol symbol)
        {
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
        )
        {
            return symbol.ToDisplayString()
                .Replace('<', '[')
                .Replace('>', ']');
        }
    }


    private static void GenerateCode(
        SourceProductionContext context,
        ImmutableArray<DiscriminatedUnionTypeInfo?> types
    )
    {
        foreach (var typeInfo in types)
        {
            if (typeInfo == null)
            {
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
}
