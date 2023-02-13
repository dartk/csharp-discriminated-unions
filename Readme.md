# CSharp.DiscriminatedUnions

A [discriminated union](https://en.wikipedia.org/wiki/Tagged_union) source generator for C#. Discriminated unions represent values that can be one of a number of cases. Each case has a unique name and can store values of different types, as opposed to the standard `enum` type, that can only be of an integral numeric type.


## Installation

Install the NuGet package `Dartk.CSharp.DiscriminatedUnions`:

```
dotnet add package Dartk.CSharp.DiscriminatedUnions
```

If you want to avoid propagating the generator package dependency, set the option `PrivateAssets="all"` in the project file. And to support runtime reflection on discriminated unions add an explicit reference to the package `Dartk.CSharp.DiscriminatedUnions.Runtime`:

```xml
<ItemGroup>
    <PackageReference Include="Dartk.CSharp.DiscriminatedUnions"
                      Version="0.1.0-alpha6"
                      PrivateAssets="all" />
    
    <PackageReference Include="Dartk.CSharp.DiscriminatedUnions.Runtime"
                      Version="0.1.0-alpha3" />
</ItemGroup>
```


## Declaring a Discriminated Union

*Discriminated unions* are declared as partial types with a `[DiscriminatedUnion]` attribute. `struct`, `class` and `record` types are supported.

*Cases* are defined as a static partial methods for which the following is true:
* Return type is the union type itself.
* Method has a `[Case]` attribute.

```c#
[DiscriminatedUnion]
public partial class Shape
{
    [Case] public static partial Shape Dot();
    [Case] public static partial Shape Circle(double radius);
    [Case] public static partial Shape Rectangle(double width, double length);
}
```

The code above declares a discriminated union `Shape` with three cases:

1) `Dot` doesn't have any data
2) `Circle` has `radius`
3) `Rectangle` has `width` and `length`


## Creating instances

One of the case defining static methods are used to create an instance of the union:

```c#
var dot = Shape.Dot();
var circle = Shape.Circle(5.0);
var rectangle = Shape.Rectangle(2.0, 4.0);
```


## Case matching

### `Switch` methods

Exhaustive case matching can be performed using `Switch` methods:

```c#
double Area(Shape shape) => shape.Switch(
    Dot: () => 0.0,
    Circle: radius => Math.PI * radius * radius,
    Rectangle: (width, length) => width * length
);
```

```c#
bool IsCircle(Shape shape) => shape.Switch(
    Circle: _ => true,
    Default: _ => false
);
```

There are four overloads of `Switch`: two that return a value and another two that return `void`.
In each pair, one method requires a handler function for every possible case and the
other requires a default handler, while all the other handlers are optional.

<details>
    <summary>Generated <code>Switch</code> methods</summary>

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

</details>

These overloads of `Switch` achieve exhaustive case matching, meaning that you will either have to
provide handlers for all of the possible cases or provide a default handler. And if you add another
case to the union later on, then all the invocations that don't provide a default handler will give
an error on compilation.


### `switch` statement or expression

Case matching can also be done using the standard `switch` statement or expression:

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


## Generated code

To save the generated code into a file during the build process, set the project properties
`EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` in the `.csproj` file:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <!--Generated files will be saved to 'obj\GeneratedFiles'-->
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\GeneratedFiles </CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

<details>
    <summary>Full generated code</summary>

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

* [Tree structures](./Docs/tree-structures.md)
* [Option type](./Docs/option.md)
* [Railway Oriented Programming](./Docs/railway-oriented-programming.md)
