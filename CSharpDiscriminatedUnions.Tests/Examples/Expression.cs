using System.Collections.Generic;
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
}


[TestClass]
public class ExpressionTests
{
    private static int Evaluate(Expression expression, IReadOnlyDictionary<string, int> env) =>
        expression.Switch(
            Number: value => value,
            Variable: name => env[name],
            Add: (left, right) => Evaluate(left, env) + Evaluate(right, env),
            Multiply: (left, right) => Evaluate(left, env) * Evaluate(right, env));


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
        var result = Evaluate(expression, environment);

        Assert.AreEqual(1 + 2 * 2, result);
    }
}