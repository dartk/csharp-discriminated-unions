using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharpDiscriminatedUnions;


public static class TypeDeclarationSyntaxUtil
{
    public static string? ToString(SyntaxNode node)
    {
        return node switch
        {
            StructDeclarationSyntax @struct => ToString(@struct),
            ClassDeclarationSyntax @class => ToString(@class),
            RecordDeclarationSyntax @record => ToString(@record),
            _ => null
        };
    }


    public static string ToString(TypeDeclarationSyntax syntax)
    {
        var declaration =
            $"{string.Join(" ", syntax.Modifiers)} {syntax.Keyword} {syntax.Identifier}";

        return syntax.TypeParameterList is { Parameters: var parameters }
            ? $"{declaration}<{string.Join(", ", parameters)}>"
            : declaration;
    }


    public static string ToString(RecordDeclarationSyntax syntax)
    {
        var declaration =
            $"{string.Join(" ", syntax.Modifiers)} record {syntax.ClassOrStructKeyword} {syntax.Identifier}";

        return syntax.TypeParameterList is { Parameters: var parameters }
            ? $"{declaration}<{string.Join(", ", parameters)}>"
            : declaration;
    }
}