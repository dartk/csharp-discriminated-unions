### Option type

[Option type](https://en.wikipedia.org/wiki/Option_type) is a type that represents some value or absence of a value. Such a type can be defined in the following way:

```c#
[DiscriminatedUnion]
readonly partial record struct Option<T>
{
    [Case] public static partial Option<T> None();
    [Case] public static partial Option<T> Some(T value);
}
```

A new instance can be created using `Option<int>.Some(10)` or `Option<int>.None()`, but that's not very convenient. Let's declare additional types and extend `Option<T>` with implicit conversion operators:

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

Now we can use `Option.Some(10)` and `Option.None` with implicit type conversion to create new instances.

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

One of the cases, where it would be great to have an option type is for the Linq methods that end with `OrDefault`(`FirstOrDefault`, `LastOrDefault`, `SingleOrDefault`, `ElementAtOrDefault`). These methods are useless with value types when an enumerable can contain the default value. Here is an example:

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

This function will return the same output when given an empty and a non-empty sequence with a default value at the beginning:

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

Because we defined our option type as a record struct, an optional value will be allocated on the stack (if it's not captured later on). We can implement `TryFirst` method using option type the following way:

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
