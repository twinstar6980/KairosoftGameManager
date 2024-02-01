#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace KairosoftGameManager.Utility {

	public static class StorageHelper {

		#region path

		public static String Regularize (
			String path
		) {
			return StorageHelper.ToPosixStyle(path);
		}

		public static String ToPosixStyle (
			String path
		) {
			return path.Replace('\\', '/');
		}

		public static String ToWindowsStyle (
			String path
		) {
			return path.Replace('/', '\\');
		}

		// ----------------

		public static String? Parent (
			String path
		) {
			var parent = Path.GetDirectoryName(path);
			return parent is null ? null : StorageHelper.Regularize(parent);
		}

		public static String Name (
			String path
		) {
			var name = Path.GetFileName(path);
			return StorageHelper.Regularize(name);
		}

		// ----------------

		public static String Temporary (
		) {
			var parent = StorageHelper.Regularize(Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path);
			var name = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
			var result = $"{parent}/{name}";
			var suffix = 0;
			while (StorageHelper.Exist(result)) {
				suffix += 1;
				result = $"{parent}/{name}.{suffix}";
			}
			return result;
		}

		// ----------------

		private static readonly Character[] InvalidPathNameCharacter = Path.GetInvalidFileNameChars();

		public static Boolean CheckName (
			String name
		) {
			if (name.Length == 0) {
				return false;
			}
			if (name[0] == ' ' || name[^1] == ' ' || name[^1] == '.') {
				return false;
			}
			return name.All((value) => (!StorageHelper.InvalidPathNameCharacter.Contains(value)));
		}

		#endregion

		#region general

		public static Boolean Exist (
			String target
		) {
			return File.Exists(target) || Directory.Exists(target);
		}

		public static Boolean ExistFile (
			String target
		) {
			return File.Exists(target);
		}

		public static Boolean ExistDirectory (
			String target
		) {
			return Directory.Exists(target);
		}

		// ----------------

		public static void CopyFile (
			String source,
			String destination
		) {
			File.Copy(source, destination, true);
			return;
		}

		public static void CopyDirectory (
			String source,
			String destination
		) {
			if (!Directory.Exists(destination)) {
				Directory.CreateDirectory(destination);
			}
			foreach (var item in Directory.GetFiles(source)) {
				StorageHelper.CopyFile(item, $"{destination}/{StorageHelper.Name(item)}");
			}
			foreach (var item in Directory.GetDirectories(source)) {
				StorageHelper.CopyDirectory(item, $"{destination}/{StorageHelper.Name(item)}");
			}
			return;
		}

		// ----------------

		public static void RenameFile (
			String source,
			String destination
		) {
			File.Move(source, destination);
			return;
		}

		public static void RenameDirectory (
			String source,
			String destination
		) {
			Directory.Move(source, destination);
			return;
		}

		// ----------------

		public static void RemoveFile (
			String source
		) {
			File.Delete(source);
			return;
		}

		public static void RemoveDirectory (
			String source
		) {
			Directory.Delete(source, true);
			return;
		}

		// ----------------

		public static void CreateFile (
			String target
		) {
			File.Create(target).Close();
			return;
		}

		public static void CreateDirectory (
			String target
		) {
			Directory.CreateDirectory(target);
			return;
		}

		// ----------------

		public static List<String> ListFile (
			String target,
			Size   depth,
			String pattern = "*"
		) {
			var targetFullPath = new DirectoryInfo(target).FullName;
			return Directory.EnumerateFiles(target, pattern, new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = depth }).Select((value) => (StorageHelper.Regularize(value[(targetFullPath.Length + 1)..]))).ToList();
		}

		public static List<String> ListDirectory (
			String target,
			Size   depth,
			String pattern = "*"
		) {
			var targetFullPath = new DirectoryInfo(target).FullName;
			return Directory.EnumerateDirectories(target, pattern, new EnumerationOptions() { RecurseSubdirectories = true, MaxRecursionDepth = depth }).Select((value) => (StorageHelper.Regularize(value[(targetFullPath.Length + 1)..]))).ToList();
		}

		// ----------------

		public static void TrashFile (
			String target
		) {
			Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			return;
		}

		public static void TrashDirectory (
			String target
		) {
			Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			return;
		}

		#endregion

		#region data

		public static async Task WriteFile (
			String target,
			Byte[] data
		) {
			await File.WriteAllBytesAsync(target, data);
			return;
		}

		public static async Task<Byte[]> ReadFile (
			String target
		) {
			return await File.ReadAllBytesAsync(target);
		}

		public static async Task<Byte[]> ReadFileLimited (
			String target,
			Size   limit
		) {
			await using var stream = File.OpenRead(target);
			var size = Math.Min((Size)stream.Length, limit);
			var data = new Byte[size];
			var sizeActual = await stream.ReadAsync(data, 0, size);
			GF.AssertTest(sizeActual == size);
			return data;
		}

		#endregion

		#region text

		public static async Task WriteFileText (
			String target,
			String text
		) {
			await File.WriteAllTextAsync(target, text);
			return;
		}

		public static async Task<String> ReadFileText (
			String target
		) {
			return await File.ReadAllTextAsync(target);
		}

		#endregion

		#region shell

		public static async Task<Boolean> RevealFile (
			String target
		) {
			return await Windows.System.Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(StorageHelper.ToWindowsStyle(target)));
		}

		public static async Task<Boolean> RevealDirectory (
			String target
		) {
			return await Windows.System.Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(StorageHelper.ToWindowsStyle(target)));
		}

		// ----------------

		public static async Task<String?> PickFile (
			Window  host,
			String? filter = null
		) {
			var picker = new FileOpenPicker() {
				FileTypeFilter = { filter is null ? "*" : "." + filter },
			};
			WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHelper.Handle(host));
			var target = await picker.PickSingleFileAsync();
			return target is null ? null : StorageHelper.Regularize(target.Path);
		}

		public static async Task<String?> PickDirectory (
			Window host
		) {
			var picker = new FolderPicker() {
				FileTypeFilter = { "*" },
			};
			WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHelper.Handle(host));
			var target = await picker.PickSingleFolderAsync();
			return target is null ? null : StorageHelper.Regularize(target.Path);
		}

		// ----------------

		public static async Task<String?> SaveFile (
			Window                host,
			Tuple<String, String> filter,
			String?               defaultName = null
		) {
			var picker = new FileSavePicker() {
				FileTypeChoices = { new (filter.Item1, ["." + filter.Item2]) },
				SuggestedFileName = defaultName,
			};
			WinRT.Interop.InitializeWithWindow.Initialize(picker, WindowHelper.Handle(host));
			var target = await picker.PickSaveFileAsync();
			if (target is not null) {
				await target.DeleteAsync();
			}
			return target is null ? null : StorageHelper.Regularize(target.Path);
		}

		#endregion

	}

}
