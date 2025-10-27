#pragma warning disable 0, CA1050,
// ReSharper disable CheckNamespace InconsistentNaming RedundantUsingDirective.Global UnusedMember.Global OperatorIsCanBeUsed

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
global using System.Runtime.CompilerServices;
global using System.ComponentModel;
global using System.Runtime.InteropServices;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Controls.Primitives;
global using CommunityToolkit.WinUI;
global using Win32 = Windows.Win32;
global using Boolean = System.Boolean;
global using Integer = System.Int64;
global using Floater = System.Double;
global using Character = System.Char;
global using Byte = System.Byte;
global using Size = System.Int32;
global using SizeU = System.UInt32;
global using String = System.String;
global using Boolean8 = System.Boolean;
global using IntegerU8 = System.Byte;
global using IntegerU16 = System.UInt16;
global using IntegerU32 = System.UInt32;
global using IntegerU64 = System.UInt64;
global using IntegerUN = System.UIntPtr;
global using IntegerS8 = System.SByte;
global using IntegerS16 = System.Int16;
global using IntegerS32 = System.Int32;
global using IntegerS64 = System.Int64;
global using IntegerSN = System.IntPtr;
global using FloaterS32 = System.Single;
global using FloaterS64 = System.Double;
global using static CommonUtility;
using Windows.ApplicationModel;

// ----------------

public static class ApplicationInformation {

	public static readonly String Identifier = Package.Current.Id.Name;

	public static readonly String Version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}";

	public static readonly String VersionMainly = $"{Package.Current.Id.Version.Major}";

	public static readonly String Name = Package.Current.DisplayName;

	public static readonly String Developer = Package.Current.PublisherDisplayName;

	public static readonly String Year = "2024-2025";

}

public static class CommonUtility {

	public static void AssertTest (
		[DoesNotReturnIf(false)]                      Boolean condition,
		[CallerArgumentExpression(nameof(condition))] String  expression = ""
	) {
		if (!condition) {
			throw new ($"AssertionException: '{expression}'.");
		}
		return;
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

	public static TTarget CastPrimitive<TTarget> (
		this Object self
	) {
		var selfValue = self;
		if (self.GetType() == typeof(IntegerUN)) {
			selfValue = (IntegerU64)self.As<IntegerUN>();
		}
		if (self.GetType() == typeof(IntegerSN)) {
			selfValue = (IntegerS64)self.As<IntegerSN>();
		}
		if (self.GetType() == typeof(Character)) {
			selfValue = (IntegerU16)self.As<Character>();
		}
		var intermediateType = typeof(TTarget);
		if (typeof(TTarget) == typeof(IntegerUN)) {
			intermediateType = typeof(IntegerU64);
		}
		if (typeof(TTarget) == typeof(IntegerSN)) {
			intermediateType = typeof(IntegerS64);
		}
		if (typeof(TTarget) == typeof(Character)) {
			intermediateType = typeof(IntegerU16);
		}
		if (typeof(TTarget).IsEnum) {
			intermediateType = typeof(Size);
		}
		var intermediateValue = Convert.ChangeType(selfValue, intermediateType);
		if (typeof(TTarget) == typeof(IntegerUN)) {
			intermediateValue = (IntegerUN)intermediateValue.As<IntegerU64>();
		}
		if (typeof(TTarget) == typeof(IntegerSN)) {
			intermediateValue = (IntegerSN)intermediateValue.As<IntegerS64>();
		}
		if (typeof(TTarget) == typeof(Character)) {
			intermediateValue = (Character)intermediateValue.As<IntegerU16>();
		}
		return (TTarget)intermediateValue;
	}

	// ----------------

	public static ObservableCollection<TSource> ToObservableCollection<TSource> (
		this IEnumerable<TSource> self
	) {
		return new (self);
	}

}
