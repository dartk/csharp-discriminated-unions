using System;
using CSharp.DiscriminatedUnions;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial class Shape
{
    [Case] public static partial Shape Dot();
    [Case] public static partial Shape Circle(double radius);
    [Case] public static partial Shape Rectangle(double width, double length);


    public static string ToString(Shape shape)
    {
        return shape.Switch(
            Dot: () => "Dot()",
            Circle: radius => $"Radius(radius: {radius})",
            Rectangle: (width, length) => $"Rectangle(width: {width}, length: {length})"
        );
    }


    public static double Area(Shape shape)
    {
        return shape.Switch(
            Dot: () => 0.0,
            Circle: radius => Math.PI * radius * radius,
            Rectangle: (width, length) => width * length);
    }


    public static bool TryGetCircleRadius(Shape shape, out double radius)
    {
        (var result, radius) = shape.Switch(
            Circle: r => (true, r),
            Default: _ => (false, 0.0)
        );

        return result;
    }


    double OtherArea(Shape shape)
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
}