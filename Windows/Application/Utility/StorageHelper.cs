#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class StorageHelper {

		#region path

		public static String Regularize(
			String path
		) {
			return path.Replace('\\', '/');
		}

		public static String Nativize(
			String path
		) {
			return path.Replace('/', '\\');
		}

		// ----------------

		public static String? Parent(
			String path
		) {
			var parent = Path.GetDirectoryName(path);
			return parent == null ? null : StorageHelper.Regularize(parent);
		}

		public static String Name(
			String path
		) {
			var name = Path.GetFileName(path);
			return StorageHelper.Regularize(name);
		}

		// ----------------

		public static async Task<String> Temporary(
		) {
			var parent = App.Instance.CacheDirectory;
			var name = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
			var result = $"{parent}/{name}";
			var suffix = 0;
			while (await StorageHelper.Exist(result)) {
				suffix += 1;
				result = $"{parent}/{name}.{suffix}";
			}
			return result;
		}

		// ----------------

		private static readonly Character[] InvalidPathNameCharacter = Path.GetInvalidFileNameChars();

		public static Boolean CheckName(
			String name
		) {
			if (name.Length == 0) {
				return false;
			}
			if (name[0] == ' ' || name[^1] == ' ' || name[^1] == '.') {
				return false;
			}
			return name.All((value) => !StorageHelper.InvalidPathNameCharacter.Contains(value));
		}

		// ----------------

		public static String GetLongPath(
			String source
		) {
			var destinationLength = Win32.PInvoke.GetLongPathName(source, []);
			var destination = new Character[destinationLength];
			var destinationLengthCheck = Win32.PInvoke.GetLongPathName(source, destination.AsSpan());
			AssertTest(destinationLengthCheck == destinationLength - 1);
			return StorageHelper.Regularize(new (destination.AsSpan(0, destinationLength.CastPrimitive<Size>() - 1)));
		}

		#endregion

		#region basic

		public static async Task<Boolean> Exist(
			String target
		) {
			if (target.Length == 0) {
				return false;
			}
			var type = new FileInfo(target).Attributes;
			return type != ~FileAttributes.None;
		}

		public static async Task Copy(
			String  target,
			String  placement,
			Boolean followLink
		) {
			AssertTest(await StorageHelper.Exist(target));
			AssertTest(!await StorageHelper.Exist(placement));
			if (StorageHelper.Parent(placement) != null && !await StorageHelper.ExistDirectory(StorageHelper.Parent(placement).AsNotNull())) {
				await StorageHelper.CreateDirectory(StorageHelper.Parent(placement).AsNotNull());
			}
			var type = new FileInfo(target).Attributes;
			if (followLink && type.HasFlag(FileAttributes.ReparsePoint)) {
				var referentType = new FileInfo(target).ResolveLinkTarget(true).AsNotNull().Attributes;
				if (referentType != ~FileAttributes.None) {
					type = referentType;
				}
			}
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				var referent = await StorageHelper.ResolveLink(target);
				var isDirectory = new FileInfo(target).Attributes.HasFlag(FileAttributes.Directory);
				await StorageHelper.CreateLink(placement, referent, isDirectory);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				var data = await StorageHelper.ReadFile(target);
				await StorageHelper.CreateFile(placement);
				await StorageHelper.WriteFile(placement, data);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				await StorageHelper.CreateDirectory(placement);
				foreach (var item in new DirectoryInfo(target).EnumerateFileSystemInfos("*", new EnumerationOptions() {
						AttributesToSkip = FileAttributes.None,
						BufferSize = 0,
						IgnoreInaccessible = false,
						MatchCasing = MatchCasing.PlatformDefault,
						MatchType = MatchType.Simple,
						MaxRecursionDepth = 0,
						RecurseSubdirectories = false,
						ReturnSpecialDirectories = false,
					})) {
					var itemName = StorageHelper.Name(item.Name);
					await StorageHelper.Copy(item.FullName, $"{placement}/{itemName}", followLink);
				}
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		public static async Task Rename(
			String target,
			String placement
		) {
			AssertTest(await StorageHelper.Exist(target));
			AssertTest(!await StorageHelper.Exist(placement));
			if (StorageHelper.Parent(placement) != null && !await StorageHelper.ExistDirectory(StorageHelper.Parent(placement).AsNotNull())) {
				await StorageHelper.CreateDirectory(StorageHelper.Parent(placement).AsNotNull());
			}
			var type = new FileInfo(target).Attributes;
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				if (!type.HasFlag(FileAttributes.Directory)) {
					File.Move(target, placement);
				}
				else {
					Directory.Move(target, placement);
				}
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				File.Move(target, placement);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				Directory.Move(target, placement);
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		public static async Task Remove(
			String target
		) {
			AssertTest(await StorageHelper.Exist(target));
			var type = new FileInfo(target).Attributes;
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				if (!type.HasFlag(FileAttributes.Directory)) {
					File.Delete(target);
				}
				else {
					Directory.Delete(target);
				}
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				File.Delete(target);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				Directory.Delete(target, true);
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		#endregion

		#region link

		public static async Task<Boolean> ExistLink(
			String target
		) {
			if (target.Length == 0) {
				return false;
			}
			var type = new FileInfo(target).Attributes;
			return type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint);
		}

		public static async Task CreateLink(
			String  target,
			String  referent,
			Boolean isDirectory
		) {
			AssertTest(!await StorageHelper.Exist(target));
			if (StorageHelper.Parent(target) != null && !await StorageHelper.ExistDirectory(StorageHelper.Parent(target).AsNotNull())) {
				await StorageHelper.CreateDirectory(StorageHelper.Parent(target).AsNotNull());
			}
			if (!isDirectory) {
				File.CreateSymbolicLink(target, StorageHelper.Nativize(referent));
			}
			else {
				Directory.CreateSymbolicLink(target, StorageHelper.Nativize(referent));
			}
			return;
		}

		// ----------------

		public static async Task<String> ResolveLink(
			String target
		) {
			AssertTest(await StorageHelper.ExistLink(target));
			return StorageHelper.Regularize(new FileInfo(target).LinkTarget.AsNotNull());
		}

		#endregion

		#region file

		public static async Task<Boolean> ExistFile(
			String target
		) {
			if (target.Length == 0) {
				return false;
			}
			var type = new FileInfo(target).Attributes;
			if (type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint)) {
				type = new FileInfo(target).ResolveLinkTarget(true).AsNotNull().Attributes;
			}
			return type != ~FileAttributes.None && !type.HasFlag(FileAttributes.Directory);
		}

		public static async Task CreateFile(
			String target
		) {
			AssertTest(!await StorageHelper.Exist(target));
			if (StorageHelper.Parent(target) != null && !await StorageHelper.ExistDirectory(StorageHelper.Parent(target).AsNotNull())) {
				await StorageHelper.CreateDirectory(StorageHelper.Parent(target).AsNotNull());
			}
			File.Create(target).Close();
			return;
		}

		// ----------------

		public static async Task<Integer> SizeFile(
			String target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			return new FileInfo(target).Length;
		}

		// ----------------

		public static async Task<Byte[]> ReadFile(
			String target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			return await File.ReadAllBytesAsync(target);
		}

		public static async Task WriteFile(
			String target,
			Byte[] data
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			await File.WriteAllBytesAsync(target, data);
			return;
		}

		// ----------------

		public static async Task<String> ReadFileText(
			String target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			return await File.ReadAllTextAsync(target);
		}

		public static async Task WriteFileText(
			String target,
			String text
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			await File.WriteAllTextAsync(target, text);
			return;
		}

		// ----------------

		public static async Task<Byte[]> ReadFileLimited(
			String target,
			Size   limit
		) {
			await using var stream = File.OpenRead(target);
			var size = Math.Min(stream.Length.CastPrimitive<Size>(), limit);
			var data = new Byte[size];
			var sizeActual = await stream.ReadAsync(data, 0, size);
			AssertTest(sizeActual == size);
			return data;
		}

		#endregion

		#region directory

		public static async Task<Boolean> ExistDirectory(
			String target
		) {
			if (target.Length == 0) {
				return false;
			}
			var type = new FileInfo(target).Attributes;
			if (type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint)) {
				type = new FileInfo(target).ResolveLinkTarget(true).AsNotNull().Attributes;
			}
			return type != ~FileAttributes.None && type.HasFlag(FileAttributes.Directory);
		}

		public static async Task CreateDirectory(
			String target
		) {
			AssertTest(!await StorageHelper.Exist(target));
			Directory.CreateDirectory(target);
			return;
		}

		// ----------------

		public static async Task<List<String>> ListDirectory(
			String  target,
			Size?   depth,
			Boolean followLink,
			Boolean allowLink,
			Boolean allowFile,
			Boolean allowDirectory
		) {
			AssertTest(await StorageHelper.ExistDirectory(target));
			var result = new List<String>();
			async Task iterate(
				String  currentTarget,
				String  currentItem,
				Integer currentDepth
			) {
				if (depth == null || currentDepth < depth) {
					foreach (var item in new DirectoryInfo(currentTarget).EnumerateFileSystemInfos("*", new EnumerationOptions() {
							AttributesToSkip = FileAttributes.None,
							BufferSize = 0,
							IgnoreInaccessible = false,
							MatchCasing = MatchCasing.PlatformDefault,
							MatchType = MatchType.Simple,
							MaxRecursionDepth = 0,
							RecurseSubdirectories = false,
							ReturnSpecialDirectories = false,
						})) {
						var itemType = item.Attributes;
						var itemName = StorageHelper.Name(item.Name);
						var itemPath = $"{(currentItem.Length == 0 ? "" : $"{currentItem}/")}{itemName}";
						if (followLink && itemType.HasFlag(FileAttributes.ReparsePoint)) {
							var referentType = item.ResolveLinkTarget(true).AsNotNull().Attributes;
							if (referentType != ~FileAttributes.None) {
								itemType = referentType;
							}
						}
						if (allowLink && itemType.HasFlag(FileAttributes.ReparsePoint)) {
							result.Add(itemPath);
						}
						if (allowFile && !itemType.HasFlag(FileAttributes.ReparsePoint) && !itemType.HasFlag(FileAttributes.Directory)) {
							result.Add(itemPath);
						}
						if (allowDirectory && !itemType.HasFlag(FileAttributes.ReparsePoint) && itemType.HasFlag(FileAttributes.Directory)) {
							result.Add(itemPath);
						}
						if (!itemType.HasFlag(FileAttributes.ReparsePoint) && itemType.HasFlag(FileAttributes.Directory)) {
							await iterate($"{currentTarget}/{itemName}", itemPath, currentDepth + 1);
						}
					}
				}
				return;
			}
			await iterate(target, "", 0);
			return result;
		}

		#endregion

		#region shell

		public static async Task Reveal(
			String target
		) {
			AssertTest(await StorageHelper.Exist(target));
			var result = Win32.PInvoke.ShellExecute(new (IntegerSN.Zero), "open", $"file://{target}", null, null, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNORMAL);
			AssertTest(result >= 32);
			return;
		}

		// ----------------

		public static async Task<String?> Pick(
			String  type,
			Window  host,
			String? location,
			String? name
		) {
			AssertTest(type == "LoadFile" || type == "LoadDirectory" || type == "SaveFile");
			var target = null as String;
			var locationTag = location == null ? null : !location.StartsWith('@') ? null : location[1..];
			var locationPath = location;
			if (locationTag != null) {
				locationPath = App.Instance.Setting.Data.StoragePickerHistoryLocation.GetValueOrDefault(locationTag);
			}
			if (locationPath != null && !await StorageHelper.ExistDirectory(locationPath)) {
				locationPath = null;
			}
			locationPath ??= "C:/";
			name ??= "";
			await Task.Run(() => {
				unsafe {
					Win32.PInvoke.CoInitialize(null);
					using var comFinalizer = new Finalizer(async () => {
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
					using var dialogFinalizer = new Finalizer(async () => {
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
						StorageHelper.Nativize(locationPath),
						null,
						typeof(Win32.UI.Shell.IShellItem).GUID,
						out var locationItem
					).ThrowOnFailure();
					using var locationItemFinalizer = new Finalizer(async () => {
						((Win32.UI.Shell.IShellItem*)locationItem)->Release();
					});
					dialog->SetFolder((Win32.UI.Shell.IShellItem*)locationItem).ThrowOnFailure();
					if (type == "SaveFile") {
						dialog->SetFileName(name).ThrowOnFailure();
					}
					var dialogResult = dialog->Show(new (WindowHelper.GetHandle(host)));
					if (dialogResult.Failed && dialogResult != unchecked((IntegerS32)0x800704C7)) {
						dialogResult.ThrowOnFailure();
					}
					if (dialogResult.Succeeded) {
						var targetItem = default(Win32.UI.Shell.IShellItem*);
						dialog->GetResult(&targetItem).ThrowOnFailure();
						var targetItemPointer = &targetItem;
						using var targetItemFinalizer = new Finalizer(async () => {
							(*targetItemPointer)->Release();
						});
						targetItem->GetDisplayName(Win32.UI.Shell.SIGDN.SIGDN_FILESYSPATH, out var targetPath).ThrowOnFailure();
						using var targetPathFinalizer = new Finalizer(async () => {
							Win32.PInvoke.CoTaskMemFree(targetPath.Value);
						});
						target = targetPath == null ? null : StorageHelper.Regularize(targetPath.ToString());
					}
				}
			});
			if (locationTag != null && target != null) {
				App.Instance.Setting.Data.StoragePickerHistoryLocation[locationTag] = type switch {
					"LoadFile"      => StorageHelper.Parent(target).AsNotNull(),
					"LoadDirectory" => target,
					"SaveFile"      => StorageHelper.Parent(target).AsNotNull(),
					_               => throw new UnreachableException(),
				};
				await App.Instance.Setting.Save(apply: false);
			}
			return target;
		}

		public static async Task<String?> PickLoadFile(
			Window  host,
			String? location
		) {
			return await StorageHelper.Pick("LoadFile", host, location, null);
		}

		public static async Task<String?> PickLoadDirectory(
			Window  host,
			String? location
		) {
			return await StorageHelper.Pick("LoadDirectory", host, location, null);
		}

		public static async Task<String?> PickSaveFile(
			Window  host,
			String? location,
			String? name
		) {
			return await StorageHelper.Pick("SaveFile", host, location, name);
		}

		// ----------------

		public static async Task Trash(
			String target
		) {
			AssertTest(await StorageHelper.Exist(target));
			if (await StorageHelper.ExistFile(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			if (await StorageHelper.ExistDirectory(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(target, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			return;
		}

		#endregion

	}

}
