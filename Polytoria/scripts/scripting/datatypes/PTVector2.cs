// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using System;

namespace Polytoria.Scripting.Datatypes;

public class PTVector2 : IScriptGDObject
{
	Vector2 vector;

	[ScriptProperty] public float X { get => vector.X; set => vector.X = value; }
	[ScriptProperty] public float Y { get => vector.Y; set => vector.Y = value; }


	[ScriptProperty] public static PTVector2 Down { get; private set; } = new() { X = 0, Y = -1 };
	[ScriptProperty] public static PTVector2 Left { get; private set; } = new() { X = -1, Y = 0 };
	[ScriptProperty] public static PTVector2 One { get; private set; } = new() { X = 1, Y = 1 };
	[ScriptProperty] public static PTVector2 Zero { get; private set; } = new() { X = 0, Y = 0 };
	[ScriptProperty] public static PTVector2 Right { get; private set; } = new() { X = 1, Y = 0 };
	[ScriptProperty] public static PTVector2 Up { get; private set; } = new() { X = 0, Y = 1 };

	[ScriptProperty] public float Magnitude => vector.Length();
	[ScriptProperty] public PTVector2 Normalized => (PTVector2)FromGDClass(-vector.Normalized());
	[ScriptProperty] public float SqrMagnitude => vector.LengthSquared();

	public static object FromGDClass(object vec)
	{
		return new PTVector2()
		{
			vector = (Vector2)vec
		};
	}

	public object ToGDClass()
	{
		return vector;
	}

	[ScriptMethod]
	public static PTVector2 New()
	{
		return new()
		{
			X = 0,
			Y = 0,
		};
	}

	[ScriptMethod]
	public static PTVector2 New(float d)
	{
		return new()
		{
			X = d,
			Y = d,
		};
	}

	[ScriptMethod]
	public static PTVector2 New(float x, float y)
	{
		return new()
		{
			X = x,
			Y = y,
		};
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Add)]
	public static PTVector2 Add(PTVector2 a, PTVector2 b)
	{
		return (PTVector2)FromGDClass(a.vector + b.vector);
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Sub)]
	public static PTVector2 Sub(PTVector2 a, PTVector2 b)
	{
		return (PTVector2)FromGDClass(a.vector - b.vector);
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Mul)]
	public static object Mul(PTVector2 a, object b)
	{
		if (b is PTVector2 vb)
			return (PTVector2)FromGDClass(a.vector * vb.vector);
		if (b is double d)
			return (PTVector2)FromGDClass(a.vector * (float)d);
		return null!;
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Div)]
	public static PTVector2 Div(PTVector2 a, double b)
	{
		return (PTVector2)FromGDClass(a.vector / (float)b);
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Mod)]
	public static PTVector2 Mod(PTVector2 a, PTVector2 b)
	{
		return (PTVector2)FromGDClass(new Vector2(
			a.vector.X % b.vector.X,
			a.vector.Y % b.vector.Y
		));
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Unm)]
	public static PTVector2 Unm(PTVector2 a)
	{
		return (PTVector2)FromGDClass(-a.vector);
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Pow)]
	public static PTVector2 Pow(PTVector2 a, PTVector2 b)
	{
		return (PTVector2)FromGDClass(new Vector2(
			(float)Math.Pow(a.vector.X, b.vector.X),
			(float)Math.Pow(a.vector.Y, b.vector.Y)
		));
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Eq)]
	public static bool Eq(PTVector2 a, PTVector2 b)
	{
		return a.vector == b.vector;
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Lt)]
	public static bool Lt(PTVector2 a, PTVector2 b)
	{
		return a.vector.LengthSquared() < b.vector.LengthSquared();
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Le)]
	public static bool Le(PTVector2 a, PTVector2 b)
	{
		return a.vector.LengthSquared() <= b.vector.LengthSquared();
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Len)]
	public static double Len(PTVector2 a)
	{
		return a.vector.Length();
	}

	[ScriptMetamethod(ScriptObjectMetamethod.ToString)]
	public static string ToString(PTVector2? v)
	{
		if (v == null) return "<Vector2>";
		return $"<Vector2:({v.vector.X}, {v.vector.Y})>";
	}

	[ScriptMethod(ConvertParamsToGD = false)] public static float Angle(PTVector2 from, PTVector2 to) => from.vector.AngleTo(to.vector);
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Cross(PTVector2 lhs, PTVector2 rhs) => (PTVector2)FromGDClass(lhs.vector.Cross(rhs.vector));
	[ScriptMethod(ConvertParamsToGD = false)] public static float Distance(PTVector2 a, PTVector2 b) => a.vector.DistanceTo(b.vector);
	[ScriptMethod(ConvertParamsToGD = false)] public static float Dot(PTVector2 lhs, PTVector2 rhs) => lhs.vector.Dot(rhs.vector);
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Lerp(PTVector2 a, PTVector2 b, float t) => (PTVector2)FromGDClass(a.vector.Lerp(b.vector, t));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Max(PTVector2 lhs, PTVector2 rhs) => (PTVector2)FromGDClass(lhs.vector.Max(rhs.vector));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Min(PTVector2 lhs, PTVector2 rhs) => (PTVector2)FromGDClass(lhs.vector.Min(rhs.vector));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 MoveTowards(PTVector2 current, PTVector2 target, float maxDistanceDelta) => (PTVector2)FromGDClass(current.vector.MoveToward(target.vector, maxDistanceDelta));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Normalize(PTVector2 value) => (PTVector2)FromGDClass(value.vector.Normalized());
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Project(PTVector2 vector, PTVector2 onNormal) => (PTVector2)FromGDClass(vector.vector.Project(onNormal.vector));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Reflect(PTVector2 inDirection, PTVector2 inNormal) => (PTVector2)FromGDClass(inDirection.vector.Reflect(inNormal.vector));
	[ScriptMethod(ConvertParamsToGD = false)] public static PTVector2 Slerp(PTVector2 a, PTVector2 b, float t) => (PTVector2)FromGDClass(a.vector.Slerp(b.vector, t));
}
