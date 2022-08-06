using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CSharpDiscriminatedUnions.Tests;


[DiscriminatedUnion]
public partial record Choice3<T0, T1, T2> {
    private record Union(
        T0 Case0,
        T1 Case1,
        T2 Case2
    );
}


[TestClass]
public class Tests {

    private const int IntValue = 12;
    private const float FloatValue = 34f;
    private const string StringValue = "Hello, World!";


    private static (
        Choice3<int, float, string> case0,
        Choice3<int, float, string> case1,
        Choice3<int, float, string> case2
        ) CreateUnions() => (
        Choice3<int, float, string>.Case0(IntValue),
        Choice3<int, float, string>.Case1(FloatValue),
        Choice3<int, float, string>.Case2(StringValue)
    );


    [TestMethod]
    public void CasePropertyValueTest() {
        var (union0, union1, union2) = CreateUnions();

        Assert.AreEqual<Choice3Enum>(Choice3Enum.Case0, union0.Case);
        Assert.AreEqual<Choice3Enum>(Choice3Enum.Case1, union1.Case);
        Assert.AreEqual<Choice3Enum>(Choice3Enum.Case2, union2.Case);
    }


    [TestMethod]
    public void SwitchExhaustive() {
        var (union0, union1, union2) = CreateUnions();

        Assert.AreEqual<int>(
            IntValue,
            union0.Switch(
                Case0: value => value,
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}")
            )
        );

        Assert.AreEqual<float>(
            FloatValue,
            union1.Switch(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => value,
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}")
            )
        );

        Assert.AreEqual<string>(
            StringValue,
            union2.Switch(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => value
            )
        );
    }


    [TestMethod]
    public void SwitchNonExhaustive() {
        var (union0, union1, union2) = CreateUnions();

        Assert.AreEqual<int>(
            IntValue,
            union0.Switch(
                Case0: value => value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            )
        );

        Assert.IsTrue(
            (bool) union0.Switch(
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}"),
                Default: value => true
            )
        );

        Assert.AreEqual<float>(
            FloatValue,
            union1.Switch(
                Case1: value => value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            )
        );

        Assert.IsTrue(
            (bool) union1.Switch(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}"),
                Default: value => true
            )
        );

        Assert.AreEqual<string>(
            StringValue,
            union2.Switch(
                Case2: value => value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            )
        );

        Assert.IsTrue(
            (bool) union2.Switch(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Default: value => true
            )
        );
    }


    [TestMethod]
    public void DoExhaustive() {
        var (union0, union1, union2) = CreateUnions();

        {
            var result = 0;
            union0.Do(
                Case0: value => result = value,
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}")
            );
            Assert.AreEqual(IntValue, result);
        }

        {
            var result = 0f;
            union1.Do(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => result = value,
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}")
            );
            Assert.AreEqual(FloatValue, result);
        }

        {
            var result = "";
            union2.Do(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => result = value
            );
            Assert.AreEqual(StringValue, result);
        }
    }


    [TestMethod]
    public void DoNonExhaustive() {
        var (union0, union1, union2) = CreateUnions();

        {
            var result = 0;
            union0.Do(
                Case0: value => result = value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            );
            Assert.AreEqual(IntValue, result);

            var isDefault = false;
            union0.Do(
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}"),
                Default: value => isDefault = true
            );
            Assert.IsTrue(isDefault);
        }

        {
            var result = 0f;
            union1.Do(
                Case1: value => result = value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            );
            Assert.AreEqual(FloatValue, result);

            var isDefault = false;
            union1.Do(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case2: value => throw new InvalidOperationException($"Unexpected Case2 Value {value}"),
                Default: value => isDefault = true
            );
            Assert.IsTrue(isDefault);
        }

        {
            var result = "";
            union2.Do(
                Case2: value => result = value,
                Default: value => throw new InvalidOperationException($"Unexpected Default Value {value}")
            );
            Assert.AreEqual(StringValue, result);

            var isDefault = false;
            union2.Do(
                Case0: value => throw new InvalidOperationException($"Unexpected Case0 Value {value}"),
                Case1: value => throw new InvalidOperationException($"Unexpected Case1 Value {value}"),
                Default: value => isDefault = true
            );
            Assert.IsTrue(isDefault);
        }
    }


    [TestMethod]
    public void GetCaseValue() {
        var (union0, union1, union2) = CreateUnions();

        Assert.AreEqual<int>(IntValue, union0.GetCase0());
        Assert.ThrowsException<InvalidOperationException>(() => union0.GetCase1());
        Assert.ThrowsException<InvalidOperationException>(() => union0.GetCase2());

        Assert.ThrowsException<InvalidOperationException>(() => union1.GetCase0());
        Assert.AreEqual<float>(FloatValue, union1.GetCase1());
        Assert.ThrowsException<InvalidOperationException>(() => union1.GetCase2());


        Assert.ThrowsException<InvalidOperationException>(() => union2.GetCase0());
        Assert.ThrowsException<InvalidOperationException>(() => union2.GetCase1());
        Assert.AreEqual<string>(StringValue, union2.GetCase2());
    }


    [TestMethod]
    public void TryGetCaseValue() {
        var (union0, union1, union2) = CreateUnions();

        {
            Assert.IsTrue((bool) union0.TryGetCase0(out var case0));
            Assert.IsFalse((bool) union0.TryGetCase1(out _));
            Assert.IsFalse((bool) union0.TryGetCase2(out _));

            Assert.AreEqual<int>(IntValue, case0);
        }

        {
            Assert.IsFalse((bool) union1.TryGetCase0(out _));
            Assert.IsTrue((bool) union1.TryGetCase1(out var case1));
            Assert.IsFalse((bool) union1.TryGetCase2(out _));

            Assert.AreEqual<float>(FloatValue, case1);
        }

        {
            Assert.IsFalse((bool) union2.TryGetCase0(out _));
            Assert.IsFalse((bool) union2.TryGetCase1(out _));
            Assert.IsTrue((bool) union2.TryGetCase2(out var case2));

            Assert.AreEqual<string>(StringValue, case2);
        }
    }


}