#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KairosoftGameManager.Utility {

	public static class ConvertHelper {

		#region type

		public static Boolean IsTypeOfTuple(
			Type type
		) {
			if (!type.IsGenericType) {
				return false;
			}
			var genericType = type.GetGenericTypeDefinition();
			return genericType == typeof(Tuple<>)
				|| genericType == typeof(Tuple<,>)
				|| genericType == typeof(Tuple<,,>)
				|| genericType == typeof(Tuple<,,,>)
				|| genericType == typeof(Tuple<,,,,>)
				|| genericType == typeof(Tuple<,,,,,>)
				|| genericType == typeof(Tuple<,,,,,,>)
				|| genericType == typeof(Tuple<,,,,,,,>);
		}

		public static Boolean IsTypeOfValueTuple(
			Type type
		) {
			if (!type.IsGenericType) {
				return false;
			}
			var genericType = type.GetGenericTypeDefinition();
			return genericType == typeof(ValueTuple<>)
				|| genericType == typeof(ValueTuple<,>)
				|| genericType == typeof(ValueTuple<,,>)
				|| genericType == typeof(ValueTuple<,,,>)
				|| genericType == typeof(ValueTuple<,,,,>)
				|| genericType == typeof(ValueTuple<,,,,,>)
				|| genericType == typeof(ValueTuple<,,,,,,>)
				|| genericType == typeof(ValueTuple<,,,,,,,>);
		}

		#endregion

		#region exception

		public static String GenerateExceptionMessage(
			Exception exception
		) {
			var message = $"{exception.Message}";
			if (exception.StackTrace != null) {
				foreach (var frame in exception.StackTrace.Split(Environment.NewLine)) {
					if (!frame.StartsWith("   at ")) {
						continue;
					}
					message += $"\n@ {frame.Substring("   at ".Length)}";
				}
			}
			return message;
		}

		#endregion

		#region enumeration

		public static String MakeEnumerationToStringOfSnakeCase<TValue>(
			TValue value
		)
			where TValue : struct, Enum {
			return ConvertHelper.ChangeStringFromCamelCaseToSnakeCase(Enum.GetName(value).AsNotNull());
		}

		public static TValue ParseEnumerationFromStringOfSnakeCase<TValue>(
			String text
		)
			where TValue : struct, Enum {
			return Enum.GetValues<TValue>().First((it) => ConvertHelper.MakeEnumerationToStringOfSnakeCase(it) == text);
		}

		#endregion

		#region boolean

		public static String MakeBooleanToString(
			Boolean value
		) {
			return value switch {
				false => "false",
				true  => "true",
			};
		}

		public static String MakeBooleanToStringOfConfirmation(
			Boolean value
		) {
			return value switch {
				false => "No",
				true  => "Yes",
			};
		}

		public static String MakeBooleanToStringOfConfirmationCharacter(
			Boolean value
		) {
			return value switch {
				false => "n",
				true  => "y",
			};
		}

		public static Floater MakeBooleanToFloaterOfOpacityVisibility(
			Boolean value
		) {
			return value switch {
				false => 0.0,
				true  => 1.0,
			};
		}

		public static Floater MakeBooleanToFloaterOfOpacityEnabled(
			Boolean value
		) {
			return value switch {
				false => 0.5,
				true  => 1.0,
			};
		}

		#endregion

		#region integer

		public static String MakeIntegerToString(
			Integer value
		) {
			return $"{value}";
		}

		#endregion

		#region floater

		public static String MakeFloaterToString(
			Floater value
		) {
			var valueDecimal = new Decimal(value);
			return $"{valueDecimal}{(!Decimal.IsInteger(valueDecimal) ? "" : ".0")}";
		}

		#endregion

		#region character

		public static Boolean IsPathSeparator(
			Character value
		) {
			return value == '/' || value == '\\';
		}

		public static Boolean IsLetter(
			Character value
		) {
			var code = (value | 0x20).CastPrimitive<Character>();
			return 'a' <= code && code <= 'z';
		}

		#endregion

		#region string

		public static List<String> SplitStringOfCamelCase(
			String source
		) {
			return new Regex(@"([A-Z][a-z]*)|([a-z]+)|([0-9]+)", RegexOptions.Compiled)
				.Matches(source)
				.Select((it) => it.Value)
				.ToList();
		}

		// ----------------

		public static String ChangeStringFromCamelCaseToSnakeCase(
			String source
		) {
			return String.Join('_', ConvertHelper.SplitStringOfCamelCase(source)).ToLower();
		}

		public static String ChangeStringFromCamelCaseThenInsertSpace(
			String source
		) {
			return String.Join(' ', ConvertHelper.SplitStringOfCamelCase(source));
		}

		#endregion

		#region string list

		public static String MakeStringListToStringWithLine(
			List<String> value
		) {
			return String.Join('\r', value) + (!value.IsEmpty() && value[^1].IsEmpty() ? "\r" : "");
		}

		public static List<String> ParseStringListFromStringWithLine(
			String text
		) {
			var value = text.Split('\r').ToList();
			if (!value.IsEmpty() && value[^1].IsEmpty()) {
				value.RemoveAt(value.Count - 1);
			}
			return value;
		}

		#endregion

		#region color

		public static Integer MakeColorToInteger(
			Color value
		) {
			return (value.A.CastPrimitive<Integer>() << 24)
				| (value.R.CastPrimitive<Integer>() << 16)
				| (value.G.CastPrimitive<Integer>() << 8)
				| (value.B.CastPrimitive<Integer>() << 0);
		}

		public static Color ParseColorFromInteger(
			Integer integer
		) {
			return Color.FromArgb(
				((integer & 0xFF000000L) >> 24).CastPrimitive<Byte>(),
				((integer & 0x00FF0000L) >> 16).CastPrimitive<Byte>(),
				((integer & 0x0000FF00L) >> 8).CastPrimitive<Byte>(),
				((integer & 0x000000FFL) >> 0).CastPrimitive<Byte>()
			);
		}

		#endregion

		#region theme

		public static String MakeThemeToString(
			ElementTheme value
		) {
			return value switch {
				ElementTheme.Default => "System",
				ElementTheme.Light   => "Light",
				ElementTheme.Dark    => "Dark",
				_                    => throw new UnreachableException(),
			};
		}

		#endregion

		#region bitmap

		public static async Task<WriteableBitmap> ParseBitmapFromBinary(
			Byte[] data
		) {
			using var stream = data.AsBuffer().AsStream().AsRandomAccessStream();
			var decoder = await BitmapDecoder.CreateAsync(stream);
			var image = new WriteableBitmap(decoder.PixelWidth.CastPrimitive<Size>(), decoder.PixelHeight.CastPrimitive<Size>());
			stream.Seek(0);
			await image.SetSourceAsync(stream);
			return image;
		}

		// source by https://stackoverflow.com/a/76641464
		public static async Task<Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource> ParseBitmapFromGdiBitmap(
			System.Drawing.Bitmap bitmap
		) {
			var data = bitmap.LockBits(new (0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
			var bytes = new Byte[data.Stride * data.Height];
			Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
			bitmap.UnlockBits(data);
			var softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
			softwareBitmap.CopyFromBuffer(bytes.AsBuffer());
			var source = new Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource();
			await source.SetBitmapAsync(softwareBitmap);
			return source;
		}

		#endregion

		#region data view

		public static async Task<List<StoragePath>> DataViewGetStoragePath(
			DataPackageView view
		) {
			return (await view.GetStorageItemsAsync())
				.Select((it) => StorageHelper.GetLongPath(it.Path))
				.ToList();
		}

		#endregion

	}

}
