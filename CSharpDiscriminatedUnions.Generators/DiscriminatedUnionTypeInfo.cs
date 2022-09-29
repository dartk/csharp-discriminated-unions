// ReSharper disable NotAccessedPositionalProperty.Global

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;


namespace CSharpDiscriminatedUnions.Generators;


internal record DiscriminatedUnionTypeInfo(
    string? Namespace,
    string Name,
    string UniqueName,
    string NameWithParameters,
    string Keywords,
    ImmutableArray<UnionCaseInfo> Cases
) {

    public IEnumerable<UnionCaseParameterInfo> Parameters =>
        this.Cases.SelectMany(@case => @case.Parameters);

    public string InputParameters =>
        string.Join(
            ", ",
            this.Parameters
                .Select(x => $"{x.Type} {x.FieldName} = default!")
                .Append("Enum Case = default!")
        );
}


internal record UnionCaseInfo(
    string Name,
    string Type,
    ImmutableArray<UnionCaseParameterInfo> Parameters
) {

    public string InputParameters => string.Join(
        ", ", this.Parameters.Select(x => $"{x.Type} {x.Name}")
    );

    public string OutputParameters => string.Join(
        ", ", this.Parameters.Select(x => $"out {x.Type} {x.Name}")
    );

    public string ConstructorParameters => string.Join(
        ", ",
        this.Parameters.Select(x => $"{x.FieldName}: {x.Name}")
            .Append($"Case: Enum.{this.Name}")
    );

    public string ParameterTypes => string.Join(
        ", ", this.Parameters.Select(x => x.Type)
    );

    public string ParameterFields => string.Join(
        ", ", this.Parameters.Select(x => $"this.{x.FieldName}")
    );

    public string ToStringValue => $"$\"{this.Name}({string.Join(", ", this.Parameters.Select(x => $"{{this.{x.FieldName}}}"))})\"";

}


internal record UnionCaseParameterInfo(
    string Type,
    string Name,
    string FieldName
);
