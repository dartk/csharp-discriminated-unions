#nullable enable
using System;
using CSharpDiscriminatedUnions;


namespace CSharpDiscriminatedUnions.Tests {



public enum ShapeEnum {
	Circle = 0,
	Square = 1,
	Rectangle = 2
}



public partial record Shape {

	public ShapeEnum Case { get; }


	private readonly double _Circle;


	private readonly double _Square;


	private readonly (double Side0, double Side1) _Rectangle;


	private Shape(
		ShapeEnum @case,
		double Circle,
		double Square,
		(double Side0, double Side1) Rectangle
	) {
		this.Case = @case;
		this._Circle = Circle;
		this._Square = Square;
		this._Rectangle = Rectangle;
	}


	public static Shape Circle(double Circle) {
		return new Shape(
			ShapeEnum.Circle,
			Circle,
			default!,
			default!);
	}


	public static Shape Square(double Square) {
		return new Shape(
			ShapeEnum.Square,
			default!,
			Square,
			default!);
	}


	public static Shape Rectangle((double Side0, double Side1) Rectangle) {
		return new Shape(
			ShapeEnum.Rectangle,
			default!,
			default!,
			Rectangle);
	}


	public bool TryGetCircle(out double Circle) {
		Circle = this._Circle!;
		return this.Case == ShapeEnum.Circle;
	}


	public bool TryGetSquare(out double Square) {
		Square = this._Square!;
		return this.Case == ShapeEnum.Square;
	}


	public bool TryGetRectangle(out (double Side0, double Side1) Rectangle) {
		Rectangle = this._Rectangle!;
		return this.Case == ShapeEnum.Rectangle;
	}


	public double GetCircle() =>
		this.IsCircle
		? this._Circle
		: throw new InvalidOperationException($"Cannot get Circle from {this.Case}");


	public double GetSquare() =>
		this.IsSquare
		? this._Square
		: throw new InvalidOperationException($"Cannot get Square from {this.Case}");


	public (double Side0, double Side1) GetRectangle() =>
		this.IsRectangle
		? this._Rectangle
		: throw new InvalidOperationException($"Cannot get Rectangle from {this.Case}");


	public bool IsCircle => this.Case == ShapeEnum.Circle;


	public bool IsSquare => this.Case == ShapeEnum.Square;


	public bool IsRectangle => this.Case == ShapeEnum.Rectangle;


	public override string ToString() {
		return this.Switch(
			Circle: value => $"Circle({value})",
			Square: value => $"Square({value})",
			Rectangle: value => $"Rectangle({value})"
		);
	}


	public T Switch<T>(
		Func<Shape, T> Default,
		Func<double, T>? Circle = null,
		Func<double, T>? Square = null,
		Func<(double Side0, double Side1), T>? Rectangle = null
	) {
		switch (this.Case) {
			case ShapeEnum.Circle: return Circle != null ? Circle(this._Circle!) : Default(this);
			case ShapeEnum.Square: return Square != null ? Square(this._Square!) : Default(this);
			case ShapeEnum.Rectangle: return Rectangle != null ? Rectangle(this._Rectangle!) : Default(this);
			default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
		}
	}

	public void Do(
		Action<Shape> Default,
		Action<double>? Circle = null,
		Action<double>? Square = null,
		Action<(double Side0, double Side1)>? Rectangle = null
	) {
		switch (this.Case) {
			case ShapeEnum.Circle: if (Circle != null) Circle(this._Circle!); else Default(this); return;
			case ShapeEnum.Square: if (Square != null) Square(this._Square!); else Default(this); return;
			case ShapeEnum.Rectangle: if (Rectangle != null) Rectangle(this._Rectangle!); else Default(this); return;
			default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
		}
	}

	public T Switch<T>(
		Func<double, T> Circle,
		Func<double, T> Square,
		Func<(double Side0, double Side1), T> Rectangle
	) {
		switch (this.Case) {
			case ShapeEnum.Circle: return Circle(this._Circle!);
			case ShapeEnum.Square: return Square(this._Square!);
			case ShapeEnum.Rectangle: return Rectangle(this._Rectangle!);
			default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
		}
	}

	public void Do(
		Action<double> Circle,
		Action<double> Square,
		Action<(double Side0, double Side1)> Rectangle
	) {
		switch (this.Case) {
			case ShapeEnum.Circle: Circle(this._Circle!); return;
			case ShapeEnum.Square: Square(this._Square!); return;
			case ShapeEnum.Rectangle: Rectangle(this._Rectangle!); return;
			default: throw new ArgumentOutOfRangeException($"Invalid union case '{this.Case}'");
		}
	}
}


}
