// This file is part of the Dartk.CSlns.DiscriminatedUnions package.
// Licensed under the MIT license.


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