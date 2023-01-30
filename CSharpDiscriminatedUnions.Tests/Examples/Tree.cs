using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpDiscriminatedUnions.Tests.Examples.Tree;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial class Tree
{
    [Case] public static partial Tree Tip();
    [Case] public static partial Tree Node(int value, Tree left, Tree right);


    public int Sum() => this.Switch(
        Tip: () => 0,
        Node: (value, left, right) => value + left.Sum() + right.Sum());
}


[TestClass]
public class TreeTests
{
    [TestMethod]
    public void SumTest()
    {
        var tree = Node(0,
            Node(1,
                Node(2, Tip(), Tip()),
                Node(3, Tip(), Tip())),
            Node(4, Tip(), Tip()));

        Assert.AreEqual(1 + 2 + 3 + 4, tree.Sum());
    }
}