# CSlns.DiscriminatedUnions

Discriminated union type generator for C#. Generated types are similar in
functionality to F# discriminated unions.

> #### [F# reference on dicscriminated unions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions)
> Discriminated unions provide support for values that can be one of a number of
> named cases. Unlike unions in some other languages like union type in C++ or a
> variant type in Visual Basic, however, each of the possible
> options is given a case identifier. The case identifiers are names for the
> various possible types of values that objects of this type could be.

For a type to be processed as a discriminated union following conditions need to
be met:

* Type has to be partial, classes, structs and records are supported
* Type has to contain definition of inner type named `Union`, public properties
  of which are
  used to define name and value type for cases
* Type has to have a `DiscriminatedUnion` attribute

```c#
[DiscriminatedUnion]
public partial class Result<TOk, TError> {
    private record Union(
        TOk Ok,
        TError Error
    );
}
```

This code will generate following
file [Result.g.cs](./Docs/GeneratedFiles/Result.g.cs)

To save generated files during build process you need to
set `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath`
properties in project file. For Example:

```xml

<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>
        $(BaseIntermediateOutputPath)\GeneratedFiles
    </CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Most concise way to define inner type `Union` is to use a record type, but
regular class can be used. Following definition is equivalent to the previous
one.

```cs
[DiscriminatedUnion]
public partial record Result<TOk, TError> {
    private class Union {
        public TOk Ok { get; }
        public TError Error { get; }
    }
}
```

## Code Examples

### Type declaration

```c#
[DiscriminatedUnion]
public partial record Shape {
    private record Union(
        double Circle,
        double Square,
        (double Side0, double Side1) Rectangle
    );
}
```

This code will generate file [Shape.g.cs](./Docs/GeneratedFiles/Shape.g.cs)

### Creating instances

```c#
var circle = Shape.Circle(10.0);
var square = Shape.Square(5.0);
var rectangle = Shape.Rectangle((2.0, 8.0));
```

### Case Matching

```c#
double GetArea(Shape shape) =>
    shape.Switch(
        Circle: radius => Math.PI * radius * radius,
        Square: side => side * side,
        Rectangle: rectangle => rectangle.Side0 * rectangle.Side1
    );
    
double GetCircleRadius(Shape shape) =>
    shape.Switch(
        Circle: r => r,
        Default: other =>
            throw new InvalidOperationException($"{other} is not a circle")
    );
```

Or

```c#
double GetArea(Shape shape) {
    switch (shape.Case) {
        case ShapeEnum.Circle:
            var radius = shape.GetCircle();
            return Math.PI * radius * radius;
        case ShapeEnum.Square:
            var side = shape.GetSquare();
            return side * side;
        case ShapeEnum.Rectangle:
            var (side0, side1) = shape.GetRectangle();
            return side0 * side1;
        default:
            throw new ArgumentOutOfRangeException();
    }
}

double GetCircleRadius(Shape shape) =>
    shape.Case switch {
        ShapeEnum.Circle => shape.GetCircle(),
        _ => throw new InvalidOperationException($"{shape} is not a circle") 
    };
```

### Equivalent code in F#

```f#
type Shape =
    | Circle of Radius: double
    | Square of Side: double
    | Rectangle of Side0: double * Side1: double
    
let circle = Shape.Circle 10.0
let square = Shape.Square 5.0
let rectangle = Shape.Rectangle (2.0, 8.0)

let GetArea shape =
    match shape with
    | Circle radius -> Math.PI * radius * radius
    | Square side -> side * side
    | Rectangle (side0, side1) -> side0 * side1
    
let GetCircleRadius shape =
    match shape with
    | Circle r -> r
    | other -> raise (InvalidOperationException (sprintf $"%A{otherShape} is not a circle"))
```