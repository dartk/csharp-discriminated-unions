// ReSharper disable NotAccessedPositionalProperty.Global


using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace CSharpDiscriminatedUnions;


public record TypeDeclarationInfo(string Declaration);
public record NamespaceDeclarationInfo(string? Declaration, string UsingStatements);


public record DiscriminatedUnionTypeInfo(
    ImmutableArray<NamespaceDeclarationInfo> NamespaceDeclarations,
    ImmutableArray<TypeDeclarationInfo> TypeDeclarations,
    string Name,
    string NameWithParameters,
    string UniqueName,
    ImmutableArray<UnionCaseInfo> Cases,
    bool GenerateToString)
{
    public IEnumerable<UnionCaseParameterInfo> Parameters =>
        this.Cases.SelectMany(@case => @case.Parameters);
}


public record UnionCaseInfo(
    string Name,
    string Type,
    ImmutableArray<UnionCaseParameterInfo> Parameters);


public record UnionCaseParameterInfo(
    string Type,
    string Name,
    string FieldName
);