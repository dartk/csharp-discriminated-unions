# About

Dartk.CSharp.DiscriminatedUnions is a [discriminated union](https://en.wikipedia.org/wiki/Tagged_union) source generator for C#.

Discriminated unions represent values that can be one of a number of cases. Each case has a unique name and can store values of different types, as opposed to the standard `enum` type, that can only be of an integral numeric type.

More information is available at the [dartk/CSharp.DiscriminatedUnions](https://github.com/dartk/csharp-discriminated-unions) github page.

## Installation

Generated code does not depend on the package at runtime. Therefore, it is safe to set the option `PrivateAssets="all"` to avoid propagating the dependency on the package:

```xml
<ItemGroup>
    <PackageReference Include="Dartk.CSharp.DiscriminatedUnions" Version="0.1.0" PrivateAssets="all" />
</ItemGroup>
```

## How to Use

#### Declaring a Discriminated Union

```c#
[DiscriminatedUnion]
partial class Shape
{
    [Case] public static partial Shape Dot();
    [Case] public static partial Shape Circle(double radius);
    [Case] public static partial Shape Rectangle(double width, double length);
}
```


#### Creating an Instance

```c#
var dot = Shape.Dot();
var circle = Shape.Circle(5.0);
var rectangle = Shape.Rectangle(2.0, 4.0);
```


#### Case Matching

```c#
double Area(Shape shape) => shape.Switch(
    Dot: () => 0.0,
    Circle: radius => Math.PI * radius * radius,
    Rectangle: (width, length) => width * length
);

bool IsCircle(Shape shape) => shape.Switch(
    Circle: _ => true,
    Default: _ => false
);
```

