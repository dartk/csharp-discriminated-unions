using System;
using System.Collections.Generic;
using CSharp.DiscriminatedUnions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpDiscriminatedUnions.Tests.Examples.Expression;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial class Expression
{
    [Case] public static partial Expression Number(int value);
    [Case] public static partial Expression Variable(string name);
    [Case] public static partial Expression Add(Expression left, Expression right);
    [Case] public static partial Expression Multiply(Expression left, Expression right);


    public int Evaluate(IReadOnlyDictionary<string, int> env) =>
        this.Switch(
            Number: value => value,
            Variable: name => env[name],
            Add: (left, right) => left.Evaluate(env) + right.Evaluate(env),
            Multiply: (left, right) => left.Evaluate(env) * right.Evaluate(env));


    public override string ToString() => this.Switch(
        Number: value => value.ToString(),
        Variable: name => name,
        Add: (left, right) => $"{left} + {right}",
        Multiply: (left, right) => $"{left} * {right}");
}


[TestClass]
public class ExpressionTests
{
    [TestMethod]
    public void EvaluateTest()
    {
        var environment = new Dictionary<string, int>
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 },
        };

        var expression = Add(Variable("a"), Multiply(Number(2), Variable("b")));
        var result = expression.Evaluate(environment);

        Console.WriteLine($"{expression} = {expression.Evaluate(environment)}");

        Assert.AreEqual("a + 2 * b", expression.ToString());
        Assert.AreEqual(1 + 2 * 2, result);
    }
}