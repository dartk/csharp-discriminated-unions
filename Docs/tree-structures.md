# Tree structures

> Following examples are based on the examples from [Microsoft's documentation on F# discriminated unions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions).

Discriminated unions are a natural fit to represent tree data structures. Below are some examples of how to use the `CSharp.DiscriminatedUnions` for that purpose:


## Simple tree

```c#
[DiscriminatedUnion]
partial class Tree
{
    [Case] public static partial Tree Tip();
    [Case] public static partial Tree Node(int value, Tree left, Tree right);
    
    
    public int Sum() => this.Switch(
        Tip: () => 0,
        Node: (value, left, right) => value + Sum(left) + Sum(right));
}
```

```c#
using static Tree;

var tree = Node(0,
    Node(1,
        Node(2, Tip(), Tip()),
        Node(3, Tip(), Tip())),
    Node(4, Tip(), Tip()));
    
Console.WriteLine(tree.Sum());
// 10
```

The code above creates a following tree structure:

```
    0
   / \
  1   4
 / \
2   3
```

## Abstract syntax tree

```c#
[DiscriminatedUnion]
partial class Expression
{
    [Case] public static partial Expression Number(int value);
    [Case] public static partial Expression Variable(string name);
    [Case] public static partial Expression Add(Expression left, Expression right);
    [Case] public static partial Expression Multiply(Expression left, Expression right);
    
    
    public int Evaluate(IReadOnlyDictionary<string, int> env) => this.Switch(
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
```

```c#
var environment = new Dictionary<string, int>
{
    { "a", 1 },
    { "b", 2 },
    { "c", 3 },
};

var expression = Add(Variable("a"), Multiply(Number(2), Variable("b")));
Console.WriteLine($"{expression} = {expression.Evaluate(environment)}");
// a + 2 * b = 5
```
