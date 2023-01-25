using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpDiscriminatedUnions.Tests.Examples.Tree;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial class Tree
{
    [Case] public static partial Tree Tip();
    [Case] public static partial Tree Node(int value, Tree left, Tree right);
}


[TestClass]
public class TreeTests
{
    private static int Sum(Tree tree) => tree.Switch(
        Tip: () => 0,
        Node: (value, left, right) => value + Sum(left) + Sum(right)
    );


    [TestMethod]
    public void SumTest()
    {
        var tree = Node(0,
            Node(1,
                Node(2,
                    Tip(),
                    Tip()),
                Node(3,
                    Tip(),
                    Tip())),
            Node(4,
                Tip(),
                Tip()));

        Assert.AreEqual(1 + 2 + 3 + 4, Sum(tree));
    }
}