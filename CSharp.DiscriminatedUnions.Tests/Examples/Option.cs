using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharp.DiscriminatedUnions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpDiscriminatedUnions.Tests.Examples.Option;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public readonly partial record struct Option<T>
{
    [Case] public static partial Option<T> None();
    [Case] public static partial Option<T> Some(T value);


    public static implicit operator Option<T>(NoneStruct _) => None();
    public static implicit operator Option<T>(SomeStruct<T> some) => Some(some.Value);
}


public static class Option
{
    public static readonly NoneStruct None = new NoneStruct();
    public static SomeStruct<T> Some<T>(T value) => new SomeStruct<T>(value);


    public readonly record struct NoneStruct;
    public readonly record struct SomeStruct<T>(T Value);
}


public static class CollectionOptionExtensions
{
    public static IEnumerable<TOut> Choose<T, TOut>(this IEnumerable<T> @this,
        Func<T, Option<TOut>> func)
    {
        return @this.Select(func)
            .Where(x => x.IsSome)
            .Select(x => x.GetSome());
    }


    public static Option<T> TryFirst<T>(this IEnumerable<T> @this)
    {
        foreach (var item in @this)
        {
            return Some(item);
        }

        return None;
    }


    public static Option<T> TryFirst<T>(this IEnumerable<T> @this, Func<T, bool> func)
    {
        foreach (var item in @this)
        {
            if (func(item))
            {
                return Some(item);
            }
        }

        return None;
    }


    public static Option<T> TryElementAt<T>(this IEnumerable<T> @this, int index)
    {
        if (@this is IReadOnlyList<T> list)
        {
            return index < list.Count ? Some(list[index]) : None;
        }

        var counter = 0;
        foreach (var item in @this)
        {
            if (counter++ == index)
            {
                return Some(item);
            }
        }

        return None;
    }


    public static Option<TValue> TryGetValue<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> @this, TKey key)
    {
        return @this.TryGetValue(key, out var value) ? Some(value) : None;
    }
}


[TestClass]
public class OptionTests
{
    static void ReportFirstItemOrDefault<T>(IEnumerable<T> numbers) where T : struct, IEquatable<T>
    {
        var first = numbers.FirstOrDefault();

        var str = first.Equals(default)
            ? "Sequence is empty or the first element is default value, who knows, we live in uncertain times"
            : $"First item: {first}";

        Console.WriteLine(str);
    }


    static void ReportFirstItem<T>(IEnumerable<T> numbers)
    {
        var str = numbers.TryFirst().Switch(
            None: () => "Sequence is empty",
            Some: first => $"First item: {first}");

        Console.WriteLine(str);
    }


    [TestMethod]
    public void ReportFirstItemWithDefault()
    {
        ReportFirstItemOrDefault(new Vector3[] { });
        ReportFirstItemOrDefault(new Vector3[] { Vector3.Zero });
    }


    [TestMethod]
    public void ReportFirstItemWithOption()
    {
        ReportFirstItem(new Vector3[] { });
        ReportFirstItem(new Vector3[] { Vector3.Zero });
    }


    [DataTestMethod]
    [DataRow(new int[] { }, DisplayName = "Empty")]
    [DataRow(new int[] { 0, 0, 0 }, DisplayName = "Three zeroes")]
    public void GetElementAt(int[] numbers)
    {
        var second = numbers.ElementAtOrDefault(1);
        Console.WriteLine(
            "ElementAtOrDefault: " +
            second switch
            {
                default(int) => "Element not found",
                _ => second.ToString()
            });

        var secondOpt = numbers.TryElementAt(1);
        Console.WriteLine(
            "TryElementAt: " +
            secondOpt.Switch(
                None: () => "Element not found",
                Some: value => value.ToString()
            ));
    }


    [DataTestMethod]
    [DataRow(new[] { 0, 1, 2, 3 }, DisplayName = "With zero")]
    [DataRow(new[] { 1, 2, 3, 4 }, DisplayName = "Without zero")]
    public void GetFirstEvenNumber(int[] numbers)
    {
        var firstEven = numbers.FirstOrDefault(x => x % 2 == 0);
        Console.WriteLine(
            "FirstOrDefault: " +
            firstEven switch
            {
                default(int) => "Even number not found",
                _ => $"First even: {firstEven}"
            });

        var firstEvenOpt = numbers.TryFirst(x => x % 2 == 0);
        Console.WriteLine(
            "TryFirst: " +
            firstEvenOpt.Switch(
                None: () => "Even number not found",
                Some: value => $"First even: {value}"
            ));
    }
}