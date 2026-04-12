#pragma warning disable 0,

// ReSharper disable RedundantUsingDirective.Global

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
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

// ReSharper restore RedundantUsingDirective.Global

// ----------------

// ReSharper disable once CheckNamespace
public static class ApplicationInformation {

	public static readonly String Identifier = Package.Current.Id.Name;

	public static readonly String Version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}";

	public static readonly String VersionMainly = $"{Package.Current.Id.Version.Major}";

	public static readonly String Name = Package.Current.DisplayName;

	public static readonly String Developer = "TwinStar";

	public static readonly String Year = "2024-2026";

}

public static class CommonUtility {

	public static void AssertTest(
		[DoesNotReturnIf(false)]                      Boolean condition,
		[CallerArgumentExpression(nameof(condition))] String  expression = ""
	) {
		if (!condition) {
			throw new ($"AssertionException: '{expression}'.");
		}
		return;
	}

	// ----------------

	public static TResult SelfLet<TSelf, TResult>(
		this TSelf           self,
		Func<TSelf, TResult> action
	)
		where TSelf : notnull {
		return action(self);
	}

	public static TSelf SelfAlso<TSelf>(
		this TSelf    self,
		Action<TSelf> action
	)
		where TSelf : notnull {
		action(self);
		return self;
	}

	// ----------------

	public static TTarget As<TTarget>(
		this Object self
	)
		where TTarget : notnull {
		if (self is TTarget target) {
			return target;
		}
		throw new NullReferenceException();
	}

	// ----------------

	public static TSelf AsNotNull<TSelf>(
		[NotNull] this TSelf? self
	)
		where TSelf : struct {
		return self ?? throw new NullReferenceException();
	}

	public static TSelf AsNotNull<TSelf>(
		[NotNull] this TSelf? self
	)
		where TSelf : class {
		return self ?? throw new NullReferenceException();
	}

	// ----------------

	public static TTarget CastPrimitive<TTarget>(
		this Object self
	) {
		var selfValue = self;
		if (self is IntegerUN) {
			selfValue = (IntegerU64)self.As<IntegerUN>();
		}
		if (self is IntegerSN) {
			selfValue = (IntegerS64)self.As<IntegerSN>();
		}
		if (self is Character) {
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

	public static void Move<TValue>(
		this List<TValue> self,
		Size              oldIndex,
		Size              newIndex
	) {
		var item = self[oldIndex];
		self.RemoveAt(oldIndex);
		self.Insert(newIndex, item);
		return;
	}

	public static Boolean IsEmpty<TValue>(
		this IEnumerable<TValue> self
	) {
		return !self.Any();
	}

	public static ObservableCollection<TValue> ToObservableCollection<TValue>(
		this IEnumerable<TValue> self
	) {
		return new (self);
	}

	public static Size GetMixedHashCode<TValue>(
		this IEnumerable<TValue> self
	) {
		var result = new HashCode();
		foreach (var item in self) {
			result.Add(item);
		}
		return result.ToHashCode();
	}

}
