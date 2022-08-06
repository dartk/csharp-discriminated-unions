using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions.Generators;


internal record DiscriminatedUnionTypeInfo(
    TypeDeclarationSyntax TypeDeclaration,
    ITypeSymbol TypeSymbol,
    INamedTypeSymbol? CasesType
);