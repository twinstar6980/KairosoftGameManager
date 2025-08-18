#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Runtime.InteropServices.WindowsRuntime;

namespace KairosoftGameManager.Utility {

	public static class ConvertHelper {

		#region type

		public static Boolean IsTypeOfTuple (
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

		public static Boolean IsTypeOfValueTuple (
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

		#region boolean

		public static String MakeBooleanToStringOfConfirmation (
			Boolean value
		) {
			return value switch {
				false => "No",
				true  => "Yes",
			};
		}

		#endregion

		#region theme

		public static String MakeThemeToString (
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

		// source by https://stackoverflow.com/a/76641464
		public static async Task<Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource> ParseBitmapFromGdiBitmap (
			System.Drawing.Bitmap bitmap
		) {
			// get pixels as an array of bytes
			var data = bitmap.LockBits(new (0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
			var bytes = new Byte[data.Stride * data.Height];
			Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
			bitmap.UnlockBits(data);
			// get WinRT SoftwareBitmap
			var softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
			softwareBitmap.CopyFromBuffer(bytes.AsBuffer());
			// build WinUI3 SoftwareBitmapSource
			var source = new Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource();
			await source.SetBitmapAsync(softwareBitmap);
			return source;
		}

		#endregion

	}

}
