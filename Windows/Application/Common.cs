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
			throw new ("Assertion failed" + (message == null ? "." : $" : {message}."));
		}
		return;
	}

	// ----------------

	public static TTarget As<TTarget> (
		this Object self
	)
		where TTarget : notnull {
		if (self is TTarget target) {
			return target;
		}
		throw new NullReferenceException();
	}

	// ----------------

	public static TType AsNotNull<TType> (
		[NotNull] this TType? self
	)
		where TType : struct {
		return self ?? throw new NullReferenceException();
	}

	public static TType AsNotNull<TType> (
		[NotNull] this TType? self
	)
		where TType : class {
		return self ?? throw new NullReferenceException();
	}

	// ----------------

	public static TResult SelfLet<TType, TResult> (
		this TType           self,
		Func<TType, TResult> action
	)
		where TType : notnull {
		return action(self);
	}

	public static TType SelfAlso<TType> (
		this TType    self,
		Action<TType> action
	)
		where TType : notnull {
		action(self);
		return self;
	}

	// ----------------

	public static ObservableCollection<TSource> ToObservableCollection<TSource> (
		this IEnumerable<TSource> self
	) {
		return new (self);
	}

}
