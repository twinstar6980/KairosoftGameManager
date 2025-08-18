#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

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
			return parent == null ? null : StorageHelper.Regularize(parent);
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
			var parent = App.CacheDirectory;
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

		// ----------------

		public static String GetLongPath (
			String source
		) {
			var destinationLength = Win32.PInvoke.GetLongPathName(source, []);
			var destination = new Character[destinationLength];
			var destinationLengthCheck = Win32.PInvoke.GetLongPathName(source, destination.AsSpan());
			GF.AssertTest(destinationLengthCheck == destinationLength - 1);
			return StorageHelper.Regularize(new (destination.AsSpan(0, destinationLength.CastPrimitive<Size>() - 1)));
		}

		#endregion

		#region exist

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

		#endregion

		#region basic

		public static void Copy (
			String source,
			String destination
		) {
			GF.AssertTest(StorageHelper.Exist(source));
			if (StorageHelper.ExistFile(source)) {
				StorageHelper.CreateFile(destination);
				File.Copy(source, destination, true);
			}
			if (StorageHelper.ExistDirectory(source)) {
				StorageHelper.CreateDirectory(destination);
				foreach (var item in Directory.GetFiles(source)) {
					StorageHelper.Copy(item, $"{destination}/{StorageHelper.Name(item)}");
				}
				foreach (var item in Directory.GetDirectories(source)) {
					StorageHelper.Copy(item, $"{destination}/{StorageHelper.Name(item)}");
				}
			}
			return;
		}

		public static void Rename (
			String source,
			String destination
		) {
			GF.AssertTest(StorageHelper.Exist(source));
			StorageHelper.CreateDirectory(StorageHelper.Parent(destination).AsNotNull());
			if (StorageHelper.ExistFile(source)) {
				File.Move(source, destination);
			}
			if (StorageHelper.ExistDirectory(source)) {
				Directory.Move(source, destination);
			}
			return;
		}

		public static void Remove (
			String source
		) {
			GF.AssertTest(StorageHelper.Exist(source));
			if (StorageHelper.ExistFile(source)) {
				File.Delete(source);
			}
			if (StorageHelper.ExistDirectory(source)) {
				Directory.Delete(source, true);
			}
			return;
		}

		#endregion

		#region file

		public static void CreateFile (
			String target
		) {
			var parent = StorageHelper.Parent(target);
			if (parent != null) {
				Directory.CreateDirectory(parent);
			}
			File.Create(target).Close();
			return;
		}

		// ----------------

		public static async Task<Byte[]> ReadFile (
			String target
		) {
			return await File.ReadAllBytesAsync(target);
		}

		public static async Task WriteFile (
			String target,
			Byte[] data
		) {
			StorageHelper.CreateFile(target);
			await File.WriteAllBytesAsync(target, data);
			return;
		}

		// ----------------

		public static async Task<Byte[]> ReadFileLimited (
			String target,
			Size   limit
		) {
			await using var stream = File.OpenRead(target);
			var size = Math.Min(stream.Length.CastPrimitive<Size>(), limit);
			var data = new Byte[size];
			var sizeActual = await stream.ReadAsync(data, 0, size);
			GF.AssertTest(sizeActual == size);
			return data;
		}

		#endregion

		#region file - text

		public static async Task<String> ReadFileText (
			String target
		) {
			return await File.ReadAllTextAsync(target);
		}

		public static async Task WriteFileText (
			String target,
			String text
		) {
			StorageHelper.CreateFile(target);
			await File.WriteAllTextAsync(target, text);
			return;
		}

		// ----------------

		public static String ReadFileTextSync (
			String target
		) {
			return File.ReadAllText(target);
		}

		#endregion

		#region directory

		public static void CreateDirectory (
			String target
		) {
			Directory.CreateDirectory(target);
			return;
		}

		#endregion

		#region iterate

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

		#endregion

		#region shell

		public static async Task Reveal (
			String target
		) {
			var result = Win32.PInvoke.ShellExecute(new (IntPtr.Zero), "open", $"file://{target}", null, null, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNORMAL);
			GF.AssertTest(result >= 32);
			return;
		}

		// ----------------

		public static async Task<String?> Pick (
			String  type,
			Window  host,
			String? location,
			String? name
		) {
			GF.AssertTest(type == "LoadFile" || type == "LoadDirectory" || type == "SaveFile");
			var target = null as String;
			var locationTag = location == null ? null : !location.StartsWith('@') ? null : location[1..];
			var locationPath = location;
			if (locationTag != null) {
				locationPath = App.Setting.Data.StoragePickerHistoryLocation.GetValueOrDefault(locationTag);
			}
			if (locationPath != null && !StorageHelper.ExistDirectory(locationPath)) {
				locationPath = null;
			}
			locationPath ??= "C:/";
			name ??= "";
			await Task.Run(() => {
				unsafe {
					Win32.PInvoke.CoInitialize(null);
					using var comFinalizer = new DisposableWrapper(() => {
						Win32.PInvoke.CoUninitialize();
					});
					Win32.PInvoke.CoCreateInstance<Win32.UI.Shell.IFileDialog>(
						type switch {
							"LoadFile"      => typeof(Win32.UI.Shell.FileOpenDialog).GUID,
							"LoadDirectory" => typeof(Win32.UI.Shell.FileOpenDialog).GUID,
							"SaveFile"      => typeof(Win32.UI.Shell.FileSaveDialog).GUID,
							_               => throw new UnreachableException(),
						},
						null,
						Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
						out var dialog
					).ThrowOnFailure();
					using var dialogFinalizer = new DisposableWrapper(() => {
						dialog->Release();
					});
					dialog->GetOptions(out var option).ThrowOnFailure();
					option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM;
					option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE;
					if (type == "LoadDirectory") {
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS;
					}
					dialog->SetOptions(option).ThrowOnFailure();
					Win32.PInvoke.SHCreateItemFromParsingName(
						StorageHelper.ToWindowsStyle(locationPath),
						null,
						typeof(Win32.UI.Shell.IShellItem).GUID,
						out var locationItem
					).ThrowOnFailure();
					using var locationItemFinalizer = new DisposableWrapper(() => {
						((Win32.UI.Shell.IShellItem*)locationItem)->Release();
					});
					dialog->SetFolder((Win32.UI.Shell.IShellItem*)locationItem).ThrowOnFailure();
					if (type == "SaveFile") {
						dialog->SetFileName(name).ThrowOnFailure();
					}
					var dialogResult = dialog->Show(new (WindowHelper.Handle(host)));
					if (dialogResult.Failed && dialogResult != unchecked((IntegerS32)0x800704C7)) {
						dialogResult.ThrowOnFailure();
					}
					if (dialogResult.Succeeded) {
						var targetItem = default(Win32.UI.Shell.IShellItem*);
						dialog->GetResult(&targetItem).ThrowOnFailure();
						var targetItemPointer = &targetItem;
						using var targetItemFinalizer = new DisposableWrapper(() => {
							(*targetItemPointer)->Release();
						});
						targetItem->GetDisplayName(Win32.UI.Shell.SIGDN.SIGDN_FILESYSPATH, out var targetPath).ThrowOnFailure();
						using var targetPathFinalizer = new DisposableWrapper(() => {
							Win32.PInvoke.CoTaskMemFree(targetPath.Value);
						});
						target = targetPath == null ? null : StorageHelper.Regularize(targetPath.ToString());
					}
				}
			});
			if (locationTag != null && target != null) {
				App.Setting.Data.StoragePickerHistoryLocation[locationTag] = type switch {
					"LoadFile"      => StorageHelper.Parent(target).AsNotNull(),
					"LoadDirectory" => target,
					"SaveFile"      => StorageHelper.Parent(target).AsNotNull(),
					_               => throw new UnreachableException(),
				};
				await App.Setting.Save(apply: false);
			}
			return target;
		}

		public static async Task<String?> PickLoadFile (
			Window  host,
			String? location
		) {
			return await StorageHelper.Pick("LoadFile", host, location, null);
		}

		public static async Task<String?> PickLoadDirectory (
			Window  host,
			String? location
		) {
			return await StorageHelper.Pick("LoadDirectory", host, location, null);
		}

		public static async Task<String?> PickSaveFile (
			Window  host,
			String? location,
			String? name
		) {
			return await StorageHelper.Pick("SaveFile", host, location, name);
		}

		// ----------------

		public static void Trash (
			String target
		) {
			GF.AssertTest(StorageHelper.Exist(target));
			if (StorageHelper.ExistFile(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			if (StorageHelper.ExistDirectory(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			return;
		}

		#endregion

	}

}
