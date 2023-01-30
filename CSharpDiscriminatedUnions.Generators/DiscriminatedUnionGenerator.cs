using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions.Generators;


[Generator]
public class NewDiscriminatedUnionGenerator : IncrementalGeneratorBase<DiscriminatedUnionTypeInfo>
{
    private const string DiscriminatedUnion = "DiscriminatedUnion";
    private const string DiscriminatedUnionAttribute = "DiscriminatedUnionAttribute";
    private const string CaseAttributeClass = "CSharpDiscriminatedUnions.CaseAttribute";


    protected override bool Choose(SyntaxNode node, CancellationToken token) =>
        node is AttributeSyntax attribute
        && attribute.Name.ExtractName() is DiscriminatedUnion or DiscriminatedUnionAttribute;


    protected override DiscriminatedUnionTypeInfo? Select(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (attribute.Parent?.Parent is not TypeDeclarationSyntax typeSyntax)
        {
            return null;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(typeSyntax);
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
        foreach (var @namespace in namespaces)
        {
            Log(@namespace.Declaration);
        }

        return new DiscriminatedUnionTypeInfo(
            namespaces,
            types,
            typeSymbol.Name,
            GetTypeNameWithParameters(typeSymbol),
            GetUniqueTypeName(typeSymbol),
            cases,
            !overridesToString
        );
    }


    protected override void Produce(SourceProductionContext context,
        ImmutableArray<DiscriminatedUnionTypeInfo> items)
    {
        var scribanTemplate =
            ScribanTemplate.Parse("ScribanTemplates", "DiscriminatedUnion.scriban");

        foreach (var typeInfo in items)
        {
            var generated = scribanTemplate.Render(
                new { TypeInfo = typeInfo },
                member => member.Name
            );

            context.AddSource(
                $"{typeInfo.UniqueName}.g.cs",
                generated
            );
        }
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


    private static void Log(object? obj)
    {
        File.AppendAllText(@"C:\Users\user\Desktop\tmp\out.txt", obj + Environment.NewLine);
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


//
// [Generator]
// public class DiscriminatedUnionGenerator : IIncrementalGenerator
// {
//     public void Initialize(IncrementalGeneratorInitializationContext context)
//     {
//         var provider = context
//             .SyntaxProvider
//             .CreateSyntaxProvider(IsDiscriminatedUnionAttr, GetUnionTypeInfo)
//             .Where(x => x is not null)
//             .Collect();
//
//         context.RegisterSourceOutput(provider, GenerateCode);
//
//         return;
//
//
//         static bool IsDiscriminatedUnionAttr(
//             SyntaxNode node, CancellationToken _
//         )
//         {
//             if (node is not AttributeSyntax attribute)
//             {
//                 return false;
//             }
//
//             return attribute.Name.ExtractName()
//                 is Const.DiscriminatedUnion
//                 or Const.DiscriminatedUnionAttribute;
//         }
//
//
//         static DiscriminatedUnionTypeInfo? GetUnionTypeInfo(
//             GeneratorSyntaxContext syntaxContext, CancellationToken _
//         )
//         {
//         }
//
//
//         static ImmutableArray<UnionCaseParameterInfo> GetUnionParametersInfo(
//             string prefix,
//             ImmutableArray<IParameterSymbol> parameters
//         )
//         {
//             var builder = ImmutableArray.CreateBuilder<UnionCaseParameterInfo>(
//                 parameters.Length
//             );
//
//             foreach (var parameter in parameters)
//             {
//                 var type = parameter.Type.ToDisplayString();
//                 var name = parameter.Name;
//                 builder.Add(new UnionCaseParameterInfo(type, name, $"{prefix}_{name}"));
//             }
//
//             return builder.MoveToImmutable();
//         }
//
//
//         static UnionCaseInfo CreateUnionCaseInfo(IMethodSymbol method)
//         {
//             return new UnionCaseInfo(
//                 method.Name,
//                 method.ReturnType.ToDisplayString(),
//                 GetUnionParametersInfo(method.Name, method.Parameters)
//             );
//         }
//
//
//         static string? GetNamespace(ISymbol type)
//         {
//             return type.ContainingNamespace.IsGlobalNamespace
//                 ? null
//                 : type.ContainingNamespace.ToDisplayString();
//         }
//
//
//         static string GetTypeDeclarationKeywords(SyntaxNode syntax)
//         {
//             return string.Join(
//                 " ",
//                 syntax
//                     .ChildTokens()
//                     .TakeWhile(x => x.Kind() != SyntaxKind.IdentifierToken)
//                     .Select(x => x.Text)
//             );
//         }
//
//         static string GetTypeNameWithParameters(ISymbol symbol)
//         {
//             return string.Join(
//                 "",
//                 symbol
//                     .ToDisplayParts()
//                     .SkipWhile(x =>
//                         x.Kind
//                             is SymbolDisplayPartKind.NamespaceName
//                             or SymbolDisplayPartKind.Punctuation
//                     )
//             );
//         }
//
//
//         static string GetUniqueTypeName(
//             ISymbol symbol
//         )
//         {
//             return symbol.ToDisplayString()
//                 .Replace('<', '[')
//                 .Replace('>', ']');
//         }
//     }
//
//
//     private static void GenerateCode(
//         SourceProductionContext context,
//         ImmutableArray<DiscriminatedUnionTypeInfo?> types
//     )
//     {
//         foreach (var typeInfo in types)
//         {
//             if (typeInfo == null)
//             {
//                 continue;
//             }
//
//             const string templateFile = "DiscriminatedUnion.scriban";
//             var generated = ScribanTemplate.Render(
//                 templateFile, new { TypeInfo = typeInfo }
//             );
//
//             context.AddSource(
//                 $"{typeInfo.UniqueName}.g.cs",
//                 generated
//             );
//         }
//     }
// }