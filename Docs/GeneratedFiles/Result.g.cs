#nullable enable
using CSharpDiscriminatedUnions;
using System;





public enum ResultEnum {
    Ok = 0,
    Error = 1
}



public partial record Result<TOk, TError> {

    public ResultEnum Case { get; }


    private readonly TOk _Ok;
    private readonly TError _Error;


    private Result(
        ResultEnum @case,
        TOk Ok,
        TError Error
    ) {
        this.Case = @case;
        this._Ok = Ok;
        this._Error = Error;
    }


    public static Result<TOk, TError> Ok(TOk Ok) {
        return new Result<TOk, TError>(
            ResultEnum.Ok,
            Ok,
            default!
        );
    }


    public static Result<TOk, TError> Error(TError Error) {
        return new Result<TOk, TError>(
            ResultEnum.Error,
            default!,
            Error
        );
    }


    public bool TryGetOk(out TOk Ok) {
        Ok = this._Ok!;
        return this.Case == ResultEnum.Ok;
    }


    public bool TryGetError(out TError Error) {
        Error = this._Error!;
        return this.Case == ResultEnum.Error;
    }


    public TOk GetOk() =>
        this.IsOk
        ? this._Ok
        : throw new InvalidOperationException($"Cannot get Ok from {this.Case}");


    public TError GetError() =>
        this.IsError
        ? this._Error
        : throw new InvalidOperationException($"Cannot get Error from {this.Case}");


    public bool IsOk => this.Case == ResultEnum.Ok;


    public bool IsError => this.Case == ResultEnum.Error;


    public override string ToString() {
        return this.Switch(
            Ok: value => $"Ok({value})",
            Error: value => $"Error({value})"
        );
    }


    public T Switch<T>(
        Func<Result<TOk, TError>, T> Default,
        Func<TOk, T>? Ok = null,
        Func<TError, T>? Error = null
    ) {
        switch (this.Case) {
            case ResultEnum.Ok: return Ok != null ? Ok(this._Ok!) : Default(this);
            case ResultEnum.Error: return Error != null ? Error(this._Error!) : Default(this);
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }


    public void Do(
        Action<Result<TOk, TError>> Default,
        Action<TOk>? Ok = null,
        Action<TError>? Error = null
    ) {
        switch (this.Case) {
            case ResultEnum.Ok: if (Ok != null) Ok(this._Ok!); else Default(this); return;
            case ResultEnum.Error: if (Error != null) Error(this._Error!); else Default(this); return;
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }


    public T Switch<T>(
        Func<TOk, T> Ok,
        Func<TError, T> Error
    ) {
        switch (this.Case) {
            case ResultEnum.Ok: return Ok(this._Ok!);
            case ResultEnum.Error: return Error(this._Error!);
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }


    public void Do(
        Action<TOk> Ok,
        Action<TError> Error
    ) {
        switch (this.Case) {
            case ResultEnum.Ok: Ok(this._Ok!); return;
            case ResultEnum.Error: Error(this._Error!); return;
            default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
        }
    }

}
