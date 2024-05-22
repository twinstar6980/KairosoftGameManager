#pragma warning disable 0,
// ReSharper disable

global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Threading;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.Runtime.InteropServices;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Controls.Primitives;
global using CommunityToolkit.WinUI;
global using Boolean = System.Boolean;
global using Integer = System.Int64;
global using Floater = System.Double;
global using Character = System.Char;
global using Byte = System.Byte;
global using Size = System.Int32;
global using USize = System.UInt32;
global using String = System.String;
global using Boolean8 = System.Boolean;
global using IntegerU8 = System.Byte;
global using IntegerU16 = System.UInt16;
global using IntegerU32 = System.UInt32;
global using IntegerU64 = System.UInt64;
global using IntegerS8 = System.SByte;
global using IntegerS16 = System.Int16;
global using IntegerS32 = System.Int32;
global using IntegerS64 = System.Int64;
global using FloaterS32 = System.Single;
global using FloaterS64 = System.Double;

public static class GF {

	public static void AssertTest (
		[DoesNotReturnIf(false)] Boolean condition,
		String?                          message = null
	) {
		if (!condition) {
			throw new ("Assertion failed" + (message is null ? "." : $" : {message}."));
		}
		return;
	}

	// ----------------

	public static T ReturnSelf<T> (
		this T target
	) {
		return target;
	}

	public static T ApplySelf<T> (
		this T    target,
		Action<T> action
	)
		where T : notnull {
		action(target);
		return target;
	}

	// ----------------

	public static String ToString<T> (
		this T target
	)
		where T : notnull {
		return target.ToString().AsNotNull();
	}

	// ----------------

	public static Boolean IsNull<T> (
		[NotNullWhen(false)] this T? target
	) {
		return target is null;
	}

	public static Boolean NotNull<T> (
		[NotNullWhen(true)] this T? target
	) {
		return target is not null;
	}

	// ----------------

	public static T AsNotNullX<T> (
		[NotNull] this T? target
	) {
		return target ?? throw new NullReferenceException();
	}

	public static T AsNotNull<T> (
		[NotNull] this T? target
	)
		where T : struct {
		return target ?? throw new NullReferenceException();
	}

	public static T AsNotNull<T> (
		[NotNull] this T? target
	)
		where T : class {
		return target ?? throw new NullReferenceException();
	}

	// ----------------

	public static T AsStruct<T> (
		this Object? target
	)
		where T : struct {
		return target as T? ?? throw new NullReferenceException();
	}

	public static T AsClass<T> (
		this Object? target
	)
		where T : class {
		return target as T ?? throw new NullReferenceException();
	}

	// ----------------

	public static T? AsStructOrNull<T> (
		this Object? target
	)
		where T : struct {
		return target as T?;
	}

	public static T? AsClassOrNull<T> (
		this Object? target
	)
		where T : class {
		return target as T;
	}

	// ----------------

	public static ObservableCollection<TSource> ToObservableCollection<TSource> (
		this IEnumerable<TSource> source
	) {
		return new (source);
	}

}
