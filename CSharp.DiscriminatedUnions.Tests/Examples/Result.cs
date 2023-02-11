using System;
using CSharp.DiscriminatedUnions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Result = CSharpDiscriminatedUnions.Tests.Examples.Result
    <CSharpDiscriminatedUnions.Tests.Examples.Input, string>;
using static CSharpDiscriminatedUnions.Tests.Examples.Result
    <CSharpDiscriminatedUnions.Tests.Examples.Input, string>;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial record Result<TOk, TError>
{
    [Case] public static partial Result<TOk, TError> Ok(TOk ok);
    [Case] public static partial Result<TOk, TError> Error(TError error);


    public Result<TResult, TError> Bind<TResult>(Func<TOk, Result<TResult, TError>> bind)
    {
        return this.Switch(
            Ok: bind,
            Error: Result<TResult, TError>.Error);
    }
}


public record Input(string Email, string Name);


[TestClass]
public class RailwayOrientedProgramming
{
    public static Result Validate(Input input)
    {
        return (input.Name == ""
                ? Error("Name must not be blank")
                : Ok(input))
            .Bind(static input => input.Name.Length > 50
                ? Error("Name must not be longer than 50 chars")
                : Ok(input))
            .Bind(static input => input.Email == ""
                ? Error("Email must not be blank")
                : Ok(input));
    }


    [TestMethod]
    public void TestValidation()
    {
        var input = new Input(Name: "My name", Email: "myemail@mail.com");
        Assert.AreEqual(Ok(input), Validate(input));
        
        Assert.AreEqual(Error("Name must not be blank"),
            Validate(new Input(Name: "", Email: "")));

        Assert.AreEqual(Error("Name must not be longer than 50 chars"),
            Validate(new Input(Name: new string('~', 51), Email: "")));
        
        Assert.AreEqual(Error("Email must not be blank"),
            Validate(new Input(Name: "My name", Email: "")));
    }
}