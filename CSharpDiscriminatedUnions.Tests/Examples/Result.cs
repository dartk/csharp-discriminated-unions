using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static CSharpDiscriminatedUnions.Tests.Examples.Result;


namespace CSharpDiscriminatedUnions.Tests.Examples;


[DiscriminatedUnion]
public partial record Result<TOk, TError>
{
    [Case] public static partial Result<TOk, TError> Ok(TOk ok);
    [Case] public static partial Result<TOk, TError> Error(TError error);


    public static implicit operator Result<TOk, TError>(OkStruct<TOk> ok) =>
        Ok(ok.Value);


    public static implicit operator Result<TOk, TError>(ErrorStruct<TError> error) =>
        Error(error.Value);
}


public static class Result
{
    public static OkStruct<T> Ok<T>(T value) => new(value);
    public static ErrorStruct<T> Error<T>(T value) => new(value);

    public readonly record struct ErrorStruct<T>(T Value);
    public readonly record struct OkStruct<T>(T Value);
}


public static partial class ReadIntegerFromFile
{
    [DiscriminatedUnion]
    public partial record Error
    {
        [Case] public static partial Error FileDoesNotExist(string fileName);
        [Case] public static partial Error FileIsLocked(string fileName);
        [Case] public static partial Error CannotParseString(string str);
    }


    public static Result<int, Error> Execute(string fileName)
    {
        try
        {
            if (!File.Exists(fileName))
            {
                return Error(Error.FileDoesNotExist(fileName));
            }

            var text = File.ReadAllText(fileName);
            if (!int.TryParse(text, out var number))
            {
                return Error(Error.CannotParseString(text));
            }

            return Ok(number);
        }
        catch (IOException ex)
        {
            if (ex.HResult == -2147024864)
            {
                return Error(Error.FileIsLocked(fileName));
            }
            
            throw;
        }
    }
}


[TestClass]
public class ResultTests
{
    [TestMethod]
    public void Test()
    {
        File.WriteAllText("number.txt", "10");
        Assert.AreEqual(Ok(10), ReadIntegerFromFile.Execute("number.txt"));

        Assert.AreEqual(
            Error(ReadIntegerFromFile.Error.FileDoesNotExist("does not exist.txt")),
            ReadIntegerFromFile.Execute("does not exist.txt"));

        using (File.CreateText("locked file.txt"))
        {
            Assert.AreEqual(
                Error(ReadIntegerFromFile.Error.FileIsLocked("locked file.txt")),
                ReadIntegerFromFile.Execute("locked file.txt"));
        }
    }
}