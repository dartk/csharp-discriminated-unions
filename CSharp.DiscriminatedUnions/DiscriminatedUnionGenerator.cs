using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharp.DiscriminatedUnions;


[Generator]
public class NewDiscriminatedUnionGenerator : IIncrementalGenerator
{
    private const string DiscriminatedUnion = nameof(DiscriminatedUnion);
    private const string DiscriminatedUnionAttribute = nameof(DiscriminatedUnionAttribute);
    private const string CaseAttributeClass = "CSharp.DiscriminatedUnions.CaseAttribute";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("Attributes.cs", """
                using System;


                namespace CSharp.DiscriminatedUnions
                {
                    /// <summary>
                    /// Marks a type as a discriminated union for source generation by
                    /// <a href="https://github.com/dartk/csharp-discriminated-unions">CSharp.DiscriminatedUnion</a>.
                    /// </summary>
                    /// <example>
                    /// <code>
                    /// [DiscriminatedUnion]
                    /// public partial class Shape
                    /// {
                    ///     [Case] public static partial Shape Dot();
                    ///     [Case] public static partial Shape Circle(double radius);
                    ///     [Case] public static partial Shape Rectangle(double width, double length);
                    /// }
                    /// </code>
                    /// </example>
                    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                    internal class DiscriminatedUnionAttribute : Attribute
                    {
                    }


                    /// <summary>
                    /// Declares cases for a <see cref="DiscriminatedUnionAttribute">Discriminated Union</see>.
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Method)]
                    internal class CaseAttribute : Attribute
                    {
                    }
                }
                """);
        });

        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is AttributeSyntax attribute &&
                attribute.Name.ExtractName() is DiscriminatedUnion or DiscriminatedUnionAttribute,
            transform: static (context, token) =>
            {
                var attribute = (AttributeSyntax)context.Node;

                if (attribute.Parent?.Parent is not TypeDeclarationSyntax typeSyntax)
                {
                    return null;
                }

                var symbol = context.SemanticModel.GetDeclaredSymbol(typeSyntax, token);
                if (symbol is not ITypeSymbol typeSymbol)
                {
                    return null;
                }

                var cases = GetCasesInfo(typeSymbol);
                if (cases.IsEmpty)
                {
                    return null;
                }

                var overridesToString = OverridesToString(typeSymbol);
                var (namespaces, types) = GetDeclarationInfo(typeSyntax);
                
                token.ThrowIfCancellationRequested();

                return new DiscriminatedUnionTypeInfo(
                    namespaces,
                    types,
                    typeSymbol.Name,
                    GetTypeNameWithParameters(typeSymbol),
                    GetUniqueTypeName(typeSymbol),
                    cases,
                    !overridesToString
                );
            }).Collect();

        context.RegisterSourceOutput(provider, static (context, items) =>
        {
            var scribanTemplate =
                ScribanTemplate.Parse("ScribanTemplates", "DiscriminatedUnion.scriban");

            foreach (var typeInfo in items)
            {
                if (typeInfo == null) continue;
                
                var generated = scribanTemplate.Render(
                    new { TypeInfo = typeInfo },
                    member => member.Name
                );

                context.AddSource(
                    $"{typeInfo.UniqueName}.g.cs",
                    generated
                );
            }
        });
    }


    private static bool OverridesToString(ITypeSymbol typeSymbol)
    {
        var members = typeSymbol.GetMembers();
        return members
            .Any(x => x is IMethodSymbol
            {
                IsOverride: true,
                IsImplicitlyDeclared: false,
                Name: "ToString",
                Parameters.IsEmpty: true
            });
    }


    private static ImmutableArray<UnionCaseInfo> GetCasesInfo(ITypeSymbol typeSymbol)
    {
        var members = typeSymbol.GetMembers();
        if (members.IsEmpty)
        {
            return ImmutableArray<UnionCaseInfo>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<UnionCaseInfo>(members.Length);

        foreach (var member in members)
        {
            if (!TryGetCaseMethod(member, out var method))
            {
                continue;
            }

            var caseInfo = CreateUnionCaseInfo(method);
            builder.Add(caseInfo);
        }

        return builder.ToImmutableArray();
    }


    private static bool TryGetCaseMethod(ISymbol symbol, out IMethodSymbol methodSymbol)
    {
        if (
            symbol.IsStatic
            && symbol is IMethodSymbol
            {
                IsPartialDefinition: true,
                TypeParameters.IsEmpty: true
            } method
            && method
                .GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString() == CaseAttributeClass))
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


    private static UnionCaseInfo CreateUnionCaseInfo(IMethodSymbol method)
    {
        return new UnionCaseInfo(
            method.Name,
            method.ReturnType.ToDisplayString(),
            GetUnionParametersInfo(method.Name, method.Parameters)
        );
    }


    private static ImmutableArray<UnionCaseParameterInfo> GetUnionParametersInfo(
        string prefix, ImmutableArray<IParameterSymbol> parameters)
    {
        var builder = ImmutableArray.CreateBuilder<UnionCaseParameterInfo>(parameters.Length);

        foreach (var parameter in parameters)
        {
            var type = parameter.Type.ToDisplayString();
            var name = parameter.Name;
            builder.Add(new UnionCaseParameterInfo(type, name, $"{prefix}_{name}"));
        }

        return builder.MoveToImmutable();
    }


    private static string? GetNamespace(ISymbol type)
    {
        return type.ContainingNamespace.IsGlobalNamespace
            ? null
            : type.ContainingNamespace.ToDisplayString();
    }


    private static (ImmutableArray<NamespaceDeclarationInfo>, ImmutableArray<TypeDeclarationInfo>)
        GetDeclarationInfo(SyntaxNode targetNode)
    {
        var namespaceDeclarations = ImmutableArray.CreateBuilder<NamespaceDeclarationInfo>();
        var typeDeclarations = ImmutableArray.CreateBuilder<TypeDeclarationInfo>();

        foreach (var node in targetNode.AncestorsAndSelf())
        {
            switch (node)
            {
                case NamespaceDeclarationSyntax namespaceSyntax:
                {
                    var name = namespaceSyntax.Name.ToString();
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(name, GetChildUsingStatements(node)));
                    break;
                }
                case FileScopedNamespaceDeclarationSyntax namespaceSyntax:
                {
                    var name = namespaceSyntax.Name.ToString();
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(name, GetChildUsingStatements(node)));
                    break;
                }
                case CompilationUnitSyntax:
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(null, GetChildUsingStatements(node)));
                    break;
                default:
                {
                    if (TypeDeclarationSyntaxUtil.ToString(node) is { } declaration)
                    {
                        typeDeclarations.Add(new TypeDeclarationInfo(declaration));
                    }

                    break;
                }
            }
        }

        typeDeclarations.Reverse();
        namespaceDeclarations.Reverse();

        return (namespaceDeclarations.ToImmutableArray(), typeDeclarations.ToImmutableArray());
    }


    private static string GetChildUsingStatements(SyntaxNode node)
    {
        var builder = new StringBuilder();

        var usingSeq = node.ChildNodes()
            .Where(x => x is UsingDirectiveSyntax or UsingStatementSyntax);

        foreach (var item in usingSeq)
        {
            builder.AppendLine(item.ToString());
        }

        return builder.ToString();
    }


    private static string GetUniqueTypeName(ISymbol symbol)
    {
        return symbol.ToDisplayString()
            .Replace('<', '[')
            .Replace('>', ']');
    }


    private static string GetTypeNameWithParameters(ISymbol symbol)
    {
        return string.Join("",
            symbol.ToDisplayParts().SkipWhile(x => x.Kind is SymbolDisplayPartKind.Punctuation));
    }
}