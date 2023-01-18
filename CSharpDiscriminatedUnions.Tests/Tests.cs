using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CSharpDiscriminatedUnions.Tests;


[DiscriminatedUnion]
public readonly partial struct Union
{
    [Case] public static partial Union Zero();
    [Case] public static partial Union One(int value);
    [Case] public static partial Union Two(int value0, int value1);
    [Case] public static partial Union Three(int value0, int value1, int value2);
}


[TestClass]
public class Tests
{
    private static (Union, Union, Union, Union) CreateUnions()
    {
        return (
            Union.Zero(),
            Union.One(10),
            Union.Two(10, 20),
            Union.Three(10, 20, 30)
        );
    }


    [TestMethod]
    public void CasePropertyValueTest()
    {
        var (zero, one, two, three) = CreateUnions();

        Assert.AreEqual(Union.Enum.Zero, zero.Case);
        Assert.AreEqual(Union.Enum.One, one.Case);
        Assert.AreEqual(Union.Enum.Two, two.Case);
        Assert.AreEqual(Union.Enum.Three, three.Case);
    }


    [TestMethod]
    public void ToStringTest()
    {
        var zero = Union.Zero();
        var one = Union.One(10);
        var two = Union.Two(10, 20);
        var three = Union.Three(10, 20, 30);

        Assert.AreEqual("Zero()", zero.ToString());
        Assert.AreEqual("One(10)", one.ToString());
        Assert.AreEqual("Two(10, 20)", two.ToString());
        Assert.AreEqual("Three(10, 20, 30)", three.ToString());
    }


    [TestMethod]
    public void SwitchExhaustive()
    {
        var (zero, one, two, three) = CreateUnions();

        Assert.AreEqual(
            (Union.Enum.Zero, 0, (0, 0), (0, 0, 0)),
            Switch(zero)
        );

        Assert.AreEqual(
            (Union.Enum.One, 10, (0, 0), (0, 0, 0)),
            Switch(one)
        );

        Assert.AreEqual(
            (Union.Enum.Two, 0, (10, 20), (0, 0, 0)),
            Switch(two)
        );

        Assert.AreEqual(
            (Union.Enum.Three, 0, (0, 0), (10, 20, 30)),
            Switch(three)
        );

        static (Union.Enum, int, (int, int), (int, int, int)) Switch(
            Union union
        )
        {
            return union.Switch(
                Zero: () => (Union.Enum.Zero, 0, (0, 0), (0, 0, 0)),
                One: x => (Union.Enum.One, x, (0, 0), (0, 0, 0)),
                Two: (x, y) => (Union.Enum.Two, 0, (x, y), (0, 0, 0)),
                Three: (x, y, z) => (Union.Enum.Three, 0, (0, 0), (x, y, z))
            );
        }
    }


    [TestMethod]
    public void SwitchNonExhaustive()
    {
        var (zero, one, two, three) = CreateUnions();

        Assert.AreEqual(0, zero.Switch(
            Zero: () => 0,
            Default: x => throw new InvalidOperationException(x.ToString())
        ));

        Assert.AreEqual(10, one.Switch(
            One: x => x,
            Default: x => throw new InvalidOperationException(x.ToString())
        ));

        Assert.AreEqual((10, 20), two.Switch(
            Two: (x, y) => (x, y),
            Default: x => throw new InvalidOperationException(x.ToString())
        ));

        Assert.AreEqual((10, 20, 30), three.Switch(
            Three: (x, y, z) => (x, y, z),
            Default: x => throw new InvalidOperationException(x.ToString())
        ));
    }


    [TestMethod]
    public void DoExhaustive()
    {
        var (zero, one, two, three) = CreateUnions();

        Assert.AreEqual(
            (Union.Enum.Zero, 0, (0, 0), (0, 0, 0)),
            Do(zero)
        );

        Assert.AreEqual(
            (Union.Enum.One, 10, (0, 0), (0, 0, 0)),
            Do(one)
        );

        Assert.AreEqual(
            (Union.Enum.Two, 0, (10, 20), (0, 0, 0)),
            Do(two)
        );

        Assert.AreEqual(
            (Union.Enum.Three, 0, (0, 0), (10, 20, 30)),
            Do(three)
        );

        static (Union.Enum, int, (int, int), (int, int, int)) Do(
            Union union
        )
        {
            (Union.Enum, int, (int, int), (int, int, int)) result = default;
            union.Do(
                Zero: () => result = (Union.Enum.Zero, 0, (0, 0), (0, 0, 0)),
                One: x => result = (Union.Enum.One, x, (0, 0), (0, 0, 0)),
                Two: (x, y) => result = (Union.Enum.Two, 0, (x, y), (0, 0, 0)),
                Three: (x, y, z) => result = (Union.Enum.Three, 0, (0, 0), (x, y, z))
            );

            return result;
        }
    }


    [TestMethod]
    public void DoNonExhaustive()
    {
        var (zero, one, two, three) = CreateUnions();

        {
            var result = 0;
            zero.Do(
                Zero: () => result = 1,
                Default: x => throw new InvalidOperationException(x.ToString())
            );

            Assert.AreEqual(1, result);
        }

        {
            var result = 0;
            one.Do(
                One: x => result = x,
                Default: x => throw new InvalidOperationException(x.ToString())
            );

            Assert.AreEqual(10, result);
        }

        {
            var result = (0, 0);
            two.Do(
                Two: (x, y) => result = (x, y),
                Default: x => throw new InvalidOperationException(x.ToString())
            );

            Assert.AreEqual((10, 20), result);
        }

        {
            var result = (0, 0, 0);
            three.Do(
                Three: (x, y, z) => result = (x, y, z),
                Default: x => throw new InvalidOperationException(x.ToString())
            );

            Assert.AreEqual((10, 20, 30), result);
        }
    }


    [TestMethod]
    public void GetCaseValue()
    {
        var (zero, one, two, three) = CreateUnions();


        Assert.ThrowsException<InvalidOperationException>(
            () => zero.GetOne()
        );
        Assert.ThrowsException<InvalidOperationException>(
            () => zero.GetTwo()
        );
        Assert.ThrowsException<InvalidOperationException>(
            () => zero.GetThree()
        );


        Assert.AreEqual(10, one.GetOne());
        Assert.ThrowsException<InvalidOperationException>(
            () => one.GetTwo()
        );
        Assert.ThrowsException<InvalidOperationException>(
            () => one.GetThree()
        );


        Assert.ThrowsException<InvalidOperationException>(
            () => two.GetOne()
        );
        Assert.AreEqual((10, 20), two.GetTwo());
        Assert.ThrowsException<InvalidOperationException>(
            () => one.GetThree()
        );


        Assert.ThrowsException<InvalidOperationException>(
            () => two.GetOne()
        );
        Assert.ThrowsException<InvalidOperationException>(
            () => one.GetTwo()
        );
        Assert.AreEqual((10, 20, 30), three.GetThree());
    }


    [TestMethod]
    public void IsCase()
    {
        var (zero, one, two, three) = CreateUnions();

        Assert.IsTrue(zero.IsZero);
        Assert.IsFalse(zero.IsOne);
        Assert.IsFalse(zero.IsTwo);
        Assert.IsFalse(zero.IsThree);

        Assert.IsFalse(one.IsZero);
        Assert.IsTrue(one.IsOne);
        Assert.IsFalse(one.IsTwo);
        Assert.IsFalse(one.IsThree);

        Assert.IsFalse(two.IsZero);
        Assert.IsFalse(two.IsOne);
        Assert.IsTrue(two.IsTwo);
        Assert.IsFalse(two.IsThree);

        Assert.IsFalse(three.IsZero);
        Assert.IsFalse(three.IsOne);
        Assert.IsFalse(three.IsTwo);
        Assert.IsTrue(three.IsThree);
    }


    [TestMethod]
    public void TryGetCaseValue()
    {
        var (zero, one, two, three) = CreateUnions();

        {
            Assert.IsFalse(zero.TryGetOne(out _));
            Assert.IsFalse(zero.TryGetTwo(out _, out _));
            Assert.IsFalse(zero.TryGetThree(out _, out _, out _));
        }

        {
            Assert.IsTrue(one.TryGetOne(out var x));
            Assert.IsFalse(one.TryGetTwo(out _, out _));
            Assert.IsFalse(one.TryGetThree(out _, out _, out _));

            Assert.AreEqual(10, x);
        }

        {
            Assert.IsFalse(two.TryGetOne(out _));
            Assert.IsTrue(two.TryGetTwo(out var x, out var y));
            Assert.IsFalse(two.TryGetThree(out _, out _, out _));

            Assert.AreEqual((10, 20), (x, y));
        }

        {
            Assert.IsFalse(three.TryGetOne(out _));
            Assert.IsFalse(three.TryGetTwo(out _, out _));
            Assert.IsTrue(three.TryGetThree(out var x, out var y, out var z));

            Assert.AreEqual((10, 20, 30), (x, y, z));
        }
    }
}