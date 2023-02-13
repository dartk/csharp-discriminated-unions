# Railway Oriented Programming

> The following example is based on the article [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) by Scott Wlaschin.

Additional reading: 
* [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) by Scott Wlaschin.
* [Against Railway-Oriented Programming](https://fsharpforfunandprofit.com/posts/against-railway-oriented-programming/) by Scott Wlashin.


## Result type

[Result type](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results) can be used for composable error handling.

```c#
[DiscriminatedUnion]
partial record Result<TOk, TError>
{
    [Case] public static partial Result<TOk, TError> Ok(TOk ok);
    [Case] public static partial Result<TOk, TError> Error(TError error);
}
```

First let's declare a bind operator:

```c#
partial record Result<TOk, TError>
{
    public Result<TResult, TError> Bind<TResult>(Func<TOk, Result<TResult, TError>> bind) =>
        this.Switch(
            Ok: bind,
            Error: Result<TResult, TError>.Error);
}
```

Given a user input:

```c#
record Input(string Email, string Name);
```

We can validate the data like this:

```c#
using Result = Result<Input, string>;
using static Result<Input, string>;


Result Validate(Input input)
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


Validate(new Input(Name: "", Email: "")));
// Error("Name must not be blank")

Validate(new Input(Name: new string('~', 51), Email: "")));
// Error("Name must not be longer than 50 chars")

Validate(new Input(Name: "My name", Email: "")));                
// Error("Email must not be blank")

Validate(new Input(Name: "My name", Email: "myemail@mail.com"));
// Ok
```
