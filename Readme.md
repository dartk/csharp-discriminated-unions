# CSharpDiscriminatedUnions

CSharpDiscriminatedUnions is a [discriminated union](https://en.wikipedia.org/wiki/Tagged_union)
type generator for C#. Generated types are similar
to [F# discriminated unions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions).

- [Discriminated Unions](#discriminated-unions)
- [Case matching](#case-matching)
    * [Switch methods](#switch-methods)
    * [`switch` statements and expressions](#switch-statements-and-expressions)
- [More examples](#more-examples)
    * [Tree structures](#tree-structures)
    * [Option type](#option-type)
    * [Result type](#result-type)

## Discriminated Unions

Discriminated unions represent values that can be one of a number of cases. Each case has a unique
name and can store values of different types, as opposed to the standard `enum` type, that can only
be of an integral numeric type.

Here is an example of a `Shape` type declaration:

```c#
[DiscriminatedUnion]
public partial class Shape
{
    [Case] public static partial Shape Dot();
    [Case] public static partial Shape Circle(double radius);
    [Case] public static partial Shape Rectangle(double width, double length);
}
```

We declared three cases:

1) `Dot` doesn't have any data
2) `Circle` has `radius`
3) `Rectangle` has `width` and `length`

To create a `Shape` instance we just use one of the static methods that define cases:

```c#
var dot = Shape.Dot();
var circle = Shape.Circle(5.0);
var rectangle = Shape.Rectangle(2.0, 4.0);
```

## Case matching

There are two ways to perform case matching:

* Generated `Switch` methods
* Standard `switch` statements and expressions

### Switch methods

We can process values of the `Shape` class declared above using generated `Switch` methods:

```c#
double Area(Shape shape)
{
    return shape.Switch(
        Dot: () => 0.0,
        Circle: radius => Math.PI * radius * radius,
        Rectangle: (width, length) => width * length);
}

bool IsCircle(Shape shape)
{
    return shape.Switch(
        Circle: _ => true,
        Default: _ => false);
}
```

There are four overloads of `Switch`: two that return a value and another two that return `void`.
In each pair, one method requires a handler function for every possible case and the
other requires a default handler, while all the other handlers are optional.

Generated `Switch` methods:

```c#
// Returns value
partial class Shape
{
    public TResult Switch<TResult>(
        Func<TResult> Dot,
        Func<double, TResult> Circle,
        Func<double, double, TResult> Rectangle);

    public TResult Switch<TResult>(
        Func<Shape, TResult> Default,
        Func<TResult>? Dot = null,
        Func<double, TResult>? Circle = null,
        Func<double, double, TResult>? Rectangle = null);
}
        
        
// Returns void
partial class Shape
{
    public void Switch(
        Action Dot,
        Action<double> Circle,
        Action<double, double> Rectangle);

    public void Switch(
        Action<Shape> Default,
        Action? Dot = null,
        Action<double>? Circle = null,
        Action<double, double>? Rectangle = null);
}
```

These overloads of `Switch` achieve exhaustive case matching, meaning that you will either have to
provide handlers for all of the possible cases or provide a default handler. And if you add another
case to the union later on, then all the invocations that don't provide a default handler will give
an error at compile time.

#### `switch` statements and expressions

There is another way that we can work with a union. The generator creates a `enum` within the
union and a property `Case` to identify cases:

```c#
partial class Shape
{
    public enum Enum {
        Dot,
        Circle,
        Rectangle,
    }
        
    public Enum Case { get; }
}
```

And additional members are generated:

```c#
partial class Shape
{
    public bool IsDot { get; }
    public bool IsCircle { get; }
    public bool IsRectangle { get; }
    
    public bool TryGetCircle(out double radius);
    public bool TryGetRectangle(out double width, out double length);
    
    // Throw InvalidOperationException if the case is not correct
    public double GetCircle();
    public (double width, double length) GetRectangle();
}
```

We can use standard switch statement or expression to process a union:

```c#
double Area(Shape shape)
{
    switch (shape.Case)
    {
        case Shape.Enum.Dot:
            return 0.0;
            
        case Shape.Enum.Circle:
            var radius = shape.GetCircle();
            return Math.PI * radius;
            
        case Shape.Enum.Rectangle:
            var (width, length) = shape.GetRectangle();
            return width * length;
            
        default:
            throw new ArgumentOutOfRangeException();
    }
}
```

However, by using switch statement or expression we are losing compile time check for
exhaustiveness. If we add a new case to the union later on, our code will compile but it will give
us a runtime error on a valid case, as opposed to the `Switch` methods that will give us an error at
compilation.

## Generated code

To save the generated code into a file during the build process, set the project properties
`EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` in the `.csproj` file.

For Example:

```xml

<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>
        $(BaseIntermediateOutputPath)\GeneratedFiles
    </CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

<details>
    <summary>Full content of the generated file Shape.g.cs</summary>

```c#
#nullable enable

public partial class Shape {

    public enum Enum {
        Dot,
        Circle,
        Rectangle,
    }
        
    private Shape(Enum Case, double Circle_radius = default!, double Rectangle_width = default!, double Rectangle_length = default!) {
        this.Case = Case;
        this.Circle_radius = Circle_radius;
        this.Rectangle_width = Rectangle_width;
        this.Rectangle_length = Rectangle_length;
    }
    
    public Enum Case { get; }
    private readonly double Circle_radius;
    private readonly double Rectangle_width;
    private readonly double Rectangle_length;
    
    public static partial Shape Dot() {
        return new Shape(Case: Enum.Dot);
    }
    
    public bool IsDot => this.Case == Enum.Dot;
    
    public static partial Shape Circle(double radius) {
        return new Shape(Circle_radius: radius, Case: Enum.Circle);
    }
    
    public bool IsCircle => this.Case == Enum.Circle;
    
    public bool TryGetCircle(out double radius) {
        radius = this.Circle_radius;
        return this.IsCircle;
    }
    
    public double GetCircle() {
        if (!this.IsCircle) {
            throw new InvalidOperationException($"Cannot get 'Circle' for '{this.Case}'.");
        }
        
        return this.Circle_radius;
    }
    
    public static partial Shape Rectangle(double width, double length) {
        return new Shape(Rectangle_width: width, Rectangle_length: length, Case: Enum.Rectangle);
    }
    
    public bool IsRectangle => this.Case == Enum.Rectangle;
    
    public bool TryGetRectangle(out double width, out double length) {
        width = this.Rectangle_width;
        length = this.Rectangle_length;
        return this.IsRectangle;
    }
    
    public (double width, double length) GetRectangle() {
        if (!this.IsRectangle) {
            throw new InvalidOperationException($"Cannot get 'Rectangle' for '{this.Case}'.");
        }
        
        return (this.Rectangle_width, this.Rectangle_length);
    }

    public TResult Switch<TResult>(Func<TResult> Dot, Func<double, TResult> Circle, Func<double, double, TResult> Rectangle) {
        switch (this.Case) {
            case Enum.Dot: return Dot();
            case Enum.Circle: return Circle(this.Circle_radius);
            case Enum.Rectangle: return Rectangle(this.Rectangle_width, this.Rectangle_length);
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }

    public TResult Switch<TResult>(Func<Shape, TResult> Default, Func<TResult>? Dot = null, Func<double, TResult>? Circle = null, Func<double, double, TResult>? Rectangle = null) {
        switch (this.Case) {
            case Enum.Dot: return Dot != null ? Dot() : Default(this);
            case Enum.Circle: return Circle != null ? Circle(this.Circle_radius) : Default(this);
            case Enum.Rectangle: return Rectangle != null ? Rectangle(this.Rectangle_width, this.Rectangle_length) : Default(this);
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }

    public void Switch(Action Dot, Action<double> Circle, Action<double, double> Rectangle) {
        switch (this.Case) {
            case Enum.Dot: Dot(); break;
            case Enum.Circle: Circle(this.Circle_radius); break;
            case Enum.Rectangle: Rectangle(this.Rectangle_width, this.Rectangle_length); break;
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }

    public void Switch(Action<Shape> Default, Action? Dot = null, Action<double>? Circle = null, Action<double, double>? Rectangle = null) {
        switch (this.Case) {
            case Enum.Dot: if (Dot != null) { Dot(); } else { Default(this); } break;
            case Enum.Circle: if (Circle != null) { Circle(this.Circle_radius); } else { Default(this); } break;
            case Enum.Rectangle: if (Rectangle != null) { Rectangle(this.Rectangle_width, this.Rectangle_length); } else { Default(this); } break;
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }
    
    public override string ToString() {
        switch (this.Case) {
            case Enum.Dot: return $"Dot()";
            case Enum.Circle: return $"Circle({this.Circle_radius})";
            case Enum.Rectangle: return $"Rectangle({this.Rectangle_width}, {this.Rectangle_length})";
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }
}
```

</details>

## More examples

### Tree structures

Following examples are based on examples
from [Microsoft's documentation on F# discriminated unions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions).

Union that represents a simple tree structure:

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
    
Console.WriteLine(tree.Sum())   // 10
```

The code above creates a following tree structure:

```
    0
   / \
  1   4
 / \
2   3
```

Abstract syntax tree union:

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

### Option type

[Option type](https://en.wikipedia.org/wiki/Option_type) is a type that represents some value or
absence of a value. Such a type can be defined in the following way:

```c#
[DiscriminatedUnion]
readonly partial record struct Option<T>
{
    [Case] public static partial Option<T> None();
    [Case] public static partial Option<T> Some(T value);
}
```

A new instance can be created using `Option<int>.Some(10)` or `Option<int>.None()`, but that's not
very convenient. Let's declare additional types and extend `Option<T>` with implicit conversion
operators:

```c#
static class Option
{
    public readonly record struct NoneStruct;
    public readonly record struct SomeStruct<T>(T Value);
    
    public static readonly NoneStruct None = new NoneStruct();
    public static SomeStruct<T> Some<T>(T value) => new SomeStruct<T>(value);
}


readonly partial record struct Option<T> {
    public static implicit operator Option<T>(NoneStruct _) => None();
    public static implicit operator Option<T>(SomeStruct<T> some) => Some(some.Value);
}
```

Now we can use `Option.Some(10)` and `Option.None` with implicit type conversion to create new
instances.

```c#
Option<int> GetLength(string str) =>
    str != null ? Option.Some(str.Length) : Option.None;
```

With the help of `using static` directive, our code gets even more concise:

```c#
using static Option;

Option<int> GetLength(string str) =>
    str != null ? Some(str.Length) : None;
```

One of the cases, where it would be great to have an option type is for the Linq methods that end
with `OrDefault`(`FirstOrDefault`, `LastOrDefault`, `SingleOrDefault`, `ElementAtOrDefault`). These
methods are useless with value types when an enumerable can contain the default value. Here is an
example:

```c#
static void ReportFirstItem<T>(IEnumerable<T> numbers)
    where T : struct, IEquatable<T>
{
    var first = numbers.FirstOrDefault();

    var str = first.Equals(default)
        ? "Enumerable is empty or the first element is a default value"
        : $"First item: {first}";

    Console.WriteLine(str);
}
```

This function will return the same output when given an empty and a non-empty sequence with a
default value at the beginning:

```c# 
ReportFirstItem(new Vector3[] { });
// Enumerable is empty or the first element is default value

ReportFirstItem(new Vector3[] { Vector3.Zero });
// Enumerable is empty or the first element is default value
```

To solve the issue we can declare a method `TryFirst` that will return a nullable value:

```c#
static class EnumerableOptionExtensions
{
    public static T? TryFirst<T>(this IEnumerable<T> @this)
        where T : struct, IEquatable<T>
    {
      foreach (var item in @this)
      {
          return item;
      }

      return null;
    }
}
```

But now we are creating an extra object on the heap because of boxing.

Because we defined our option type as a record struct, an optional value will be allocated on the
stack (if it's not captured later on). We can implement `TryFirst` method using option type the
following way:

```c#
static class EnumerableOptionExtensions
{
    public static Option<T> TryFirst<T>(this IEnumerable<T> @this)
    {
      foreach (var item in @this)
      {
          return Some(item);
      }

      return None;
    }
}
```

Now we can easily determine if an enumerable is empty and avoid unnecessary boxing of a struct:

```c#
static void ReportFirstItem<T>(IEnumerable<T> numbers)
{
    var str = numbers.TryFirst().Switch(
        None: () => "Enumerable is empty",
        Some: first => $"First item: {first}");

    Console.WriteLine(str);
}


ReportFirstItem(new Vector3[] { });              
// Sequence is empty

ReportFirstItem(new Vector3[] { Vector3.Zero });
// First item: <0 0 0>
```

### Result type

[Result type](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results) can be
used for composable error handling.

```c#
[DiscriminatedUnion]
partial record Result<TOk, TError>
{
    [Case] public static partial Result<TOk, TError> Ok(TOk ok);
    [Case] public static partial Result<TOk, TError> Error(TError error);
}
```

> The following example is based on the
> article [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) by Scott Wlaschin.
> I also recommend reading
> [Against Railway-Oriented Programming](https://fsharpforfunandprofit.com/posts/against-railway-oriented-programming/)
> by Scott Wlashin.

First let's declare a bind operator:

```c#
partial record Result<TOk, TError>
{
    public Result<TResult, TError> Bind<TResult>(Func<TOk, Result<TResult, TError>> bind) =>
        this.Switch(
            Ok: bind,
            Error: Result<TResult, TError>.Error);
}
```

Given a user input:

```c#
record Input(string Email, string Name);
```

We can validate the data like this:

```c#
using Result = Result<Input, string>;
using static Result<Input, string>;


Result Validate(Input input)
{
    return (input.Name == ""
            ? Error("Name must not be blank")
            : Ok(input))
        .Bind(static input => input.Name.Length > 50
            ? Error("Name must not be longer than 50 chars")
            : Ok(input))
        .Bind(static input => input.Email == ""
            ? Error("Email must not be blank")
            : Ok(input));
}


Validate(new Input(Name: "", Email: "")));
// Error("Name must not be blank")

Validate(new Input(Name: new string('~', 51), Email: "")));
// Error("Name must not be longer than 50 chars")

Validate(new Input(Name: "My name", Email: "")));                
// Error("Email must not be blank")

Validate(new Input(Name: "My name", Email: "myemail@mail.com"));
// Ok
```
