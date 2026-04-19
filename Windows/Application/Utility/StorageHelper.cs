#pragma warning disable 0,

using KairosoftGameManager;
using Windows.ApplicationModel;

namespace KairosoftGameManager.Utility {

	public enum StorageQueryType {
		UserHome,
		ApplicationShared,
		ApplicationCache,
		ApplicationPackage,
		ApplicationPackagedShared,
		ApplicationPackagedCache,
	}

	public enum StoragePickType {
		LoadFile,
		LoadDirectory,
		SaveFile,
	}

	// ----------------

	public static class StorageHelper {

		#region basic

		public static async Task<Boolean> Exist(
			StoragePath target
		) {
			if (target.Type() != StoragePathType.Absolute) {
				return false;
			}
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			return type != ~FileAttributes.None;
		}

		public static async Task Copy(
			StoragePath target,
			StoragePath placement,
			Boolean     followLink
		) {
			AssertTest(await StorageHelper.Exist(target));
			AssertTest(!await StorageHelper.Exist(placement));
			var placementParent = placement.Parent();
			if (placementParent != null && !await StorageHelper.ExistDirectory(placementParent)) {
				await StorageHelper.CreateDirectory(placementParent);
			}
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			if (followLink && type.HasFlag(FileAttributes.ReparsePoint)) {
				var referentType = new FileInfo(targetString).ResolveLinkTarget(true).AsNotNull().Attributes;
				if (referentType != ~FileAttributes.None) {
					type = referentType;
				}
			}
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				var referent = await StorageHelper.ResolveLink(target);
				var isDirectory = new FileInfo(targetString).Attributes.HasFlag(FileAttributes.Directory);
				await StorageHelper.CreateLink(placement, referent, isDirectory);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				var data = await StorageHelper.ReadFile(target);
				await StorageHelper.CreateFile(placement);
				await StorageHelper.WriteFile(placement, data);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				await StorageHelper.CreateDirectory(placement);
				foreach (var item in new DirectoryInfo(targetString).EnumerateFileSystemInfos("*", new EnumerationOptions() {
						AttributesToSkip = FileAttributes.None,
						BufferSize = 0,
						IgnoreInaccessible = false,
						MatchCasing = MatchCasing.PlatformDefault,
						MatchType = MatchType.Simple,
						MaxRecursionDepth = 0,
						RecurseSubdirectories = false,
						ReturnSpecialDirectories = false,
					})) {
					var itemName = item.Name;
					await StorageHelper.Copy(target.Join(itemName), placement.Join(itemName), followLink);
				}
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		public static async Task Rename(
			StoragePath target,
			StoragePath placement
		) {
			AssertTest(await StorageHelper.Exist(target));
			AssertTest(!await StorageHelper.Exist(placement));
			var placementParent = placement.Parent();
			if (placementParent != null && !await StorageHelper.ExistDirectory(placementParent)) {
				await StorageHelper.CreateDirectory(placementParent);
			}
			var targetString = target.EmitNative();
			var placementString = placement.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				if (!type.HasFlag(FileAttributes.Directory)) {
					File.Move(targetString, placementString);
				}
				else {
					Directory.Move(targetString, placementString);
				}
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				File.Move(targetString, placementString);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				Directory.Move(targetString, placementString);
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		public static async Task Remove(
			StoragePath target
		) {
			AssertTest(await StorageHelper.Exist(target));
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			if (type.HasFlag(FileAttributes.ReparsePoint)) {
				if (!type.HasFlag(FileAttributes.Directory)) {
					File.Delete(targetString);
				}
				else {
					Directory.Delete(targetString);
				}
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && !type.HasFlag(FileAttributes.Directory)) {
				File.Delete(targetString);
			}
			else if (!type.HasFlag(FileAttributes.ReparsePoint) && type.HasFlag(FileAttributes.Directory)) {
				Directory.Delete(targetString, true);
			}
			else {
				throw new NotSupportedException();
			}
			return;
		}

		#endregion

		#region link

		public static async Task<Boolean> ExistLink(
			StoragePath target
		) {
			if (target.Type() != StoragePathType.Absolute) {
				return false;
			}
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			return type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint);
		}

		public static async Task CreateLink(
			StoragePath target,
			StoragePath referent,
			Boolean     isDirectory
		) {
			AssertTest(!await StorageHelper.Exist(target));
			var targetParent = target.Parent();
			if (targetParent != null && !await StorageHelper.ExistDirectory(targetParent)) {
				await StorageHelper.CreateDirectory(targetParent);
			}
			var targetString = target.EmitNative();
			var referentString = referent.EmitNative();
			if (!isDirectory) {
				File.CreateSymbolicLink(targetString, referentString);
			}
			else {
				Directory.CreateSymbolicLink(targetString, referentString);
			}
			return;
		}

		// ----------------

		public static async Task<StoragePath> ResolveLink(
			StoragePath target
		) {
			AssertTest(await StorageHelper.ExistLink(target));
			var targetString = target.EmitNative();
			return new (new FileInfo(targetString).LinkTarget.AsNotNull());
		}

		#endregion

		#region file

		public static async Task<Boolean> ExistFile(
			StoragePath target
		) {
			if (target.Type() != StoragePathType.Absolute) {
				return false;
			}
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			if (type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint)) {
				type = new FileInfo(targetString).ResolveLinkTarget(true).AsNotNull().Attributes;
			}
			return type != ~FileAttributes.None && !type.HasFlag(FileAttributes.Directory);
		}

		public static async Task CreateFile(
			StoragePath target
		) {
			AssertTest(!await StorageHelper.Exist(target));
			var targetParent = target.Parent();
			if (targetParent != null && !await StorageHelper.ExistDirectory(targetParent)) {
				await StorageHelper.CreateDirectory(targetParent);
			}
			var targetString = target.EmitNative();
			File.Create(targetString).Close();
			return;
		}

		// ----------------

		public static async Task<Integer> SizeFile(
			StoragePath target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			return new FileInfo(targetString).Length;
		}

		// ----------------

		public static async Task<Byte[]> ReadFile(
			StoragePath target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			return await File.ReadAllBytesAsync(targetString);
		}

		public static async Task WriteFile(
			StoragePath target,
			Byte[]      data
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			await File.WriteAllBytesAsync(targetString, data);
			return;
		}

		// ----------------

		public static async Task<String> ReadFileText(
			StoragePath target
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			return await File.ReadAllTextAsync(targetString);
		}

		public static async Task WriteFileText(
			StoragePath target,
			String      text
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			await File.WriteAllTextAsync(targetString, text);
			return;
		}

		// ----------------

		public static async Task<Byte[]> ReadFileLimited(
			StoragePath target,
			Size        limit
		) {
			AssertTest(await StorageHelper.ExistFile(target));
			var targetString = target.EmitNative();
			await using var stream = File.OpenRead(targetString);
			var size = Math.Min(stream.Length.CastPrimitive<Size>(), limit);
			var data = new Byte[size];
			var sizeActual = await stream.ReadAsync(data, 0, size);
			AssertTest(sizeActual == size);
			return data;
		}

		#endregion

		#region directory

		public static async Task<Boolean> ExistDirectory(
			StoragePath target
		) {
			if (target.Type() != StoragePathType.Absolute) {
				return false;
			}
			var targetString = target.EmitNative();
			var type = new FileInfo(targetString).Attributes;
			if (type != ~FileAttributes.None && type.HasFlag(FileAttributes.ReparsePoint)) {
				type = new FileInfo(targetString).ResolveLinkTarget(true).AsNotNull().Attributes;
			}
			return type != ~FileAttributes.None && type.HasFlag(FileAttributes.Directory);
		}

		public static async Task CreateDirectory(
			StoragePath target
		) {
			AssertTest(!await StorageHelper.Exist(target));
			var targetString = target.EmitNative();
			Directory.CreateDirectory(targetString);
			return;
		}

		// ----------------

		public static async Task<List<StoragePath>> ListDirectory(
			StoragePath target,
			Size?       depth,
			Boolean     followLink,
			Boolean     allowLink,
			Boolean     allowFile,
			Boolean     allowDirectory
		) {
			AssertTest(await StorageHelper.ExistDirectory(target));
			var result = new List<StoragePath>();
			async Task iterate(
				StoragePath currentTarget,
				StoragePath currentItem,
				Integer     currentDepth
			) {
				if (depth == null || currentDepth < depth) {
					var currentTargetString = currentTarget.EmitNative();
					foreach (var item in new DirectoryInfo(currentTargetString).EnumerateFileSystemInfos("*", new EnumerationOptions() {
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
						var itemName = item.Name;
						var itemPath = currentItem.Join(itemName);
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
							await iterate(currentTarget.Join(itemName), itemPath, currentDepth + 1);
						}
					}
				}
				return;
			}
			await iterate(target, new (StoragePathType.Detached), 0);
			return result;
		}

		#endregion

		#region shell

		public static async Task<StoragePath> Query(
			StorageQueryType type
		) {
			var path = null as StoragePath;
			switch (type) {
				case StorageQueryType.UserHome: {
					path = new ($"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
					break;
				}
				case StorageQueryType.ApplicationShared: {
					path = new ($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{ApplicationInformation.Identifier}");
					break;
				}
				case StorageQueryType.ApplicationCache: {
					path = new ($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{ApplicationInformation.Identifier}\\cache");
					break;
				}
				case StorageQueryType.ApplicationPackage: {
					path = new ($"{Package.Current.InstalledPath}");
					break;
				}
				case StorageQueryType.ApplicationPackagedShared: {
					path = new ($"{Windows.Storage.ApplicationData.Current.LocalFolder.Path}");
					break;
				}
				case StorageQueryType.ApplicationPackagedCache: {
					path = new ($"{Windows.Storage.ApplicationData.Current.LocalFolder.Path}\\cache");
					break;
				}
				default: throw new UnreachableException();
			}
			return path;
		}

		public static async Task Reveal(
			StoragePath target
		) {
			AssertTest(await StorageHelper.Exist(target));
			var targetString = target.EmitNative();
			var result = Win32.PInvoke.ShellExecute(new (IntegerSN.Zero), "open", $"file://{targetString}", null, null, Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_SHOWNORMAL);
			AssertTest(result >= 32);
			return;
		}

		public static async Task<StoragePath?> Pick(
			StoragePickType type,
			StoragePath?    location,
			String?         name,
			Window          host
		) {
			if (location == null || !await StorageHelper.ExistDirectory(location)) {
				location = await StorageHelper.Query(StorageQueryType.UserHome);
			}
			if (name == null) {
				name = "";
			}
			var target = null as StoragePath;
			await Task.Run(() => {
				unsafe {
					Win32.PInvoke.CoCreateInstance<Win32.UI.Shell.IFileDialog>(
						type switch {
							StoragePickType.LoadFile      => typeof(Win32.UI.Shell.FileOpenDialog).GUID,
							StoragePickType.LoadDirectory => typeof(Win32.UI.Shell.FileOpenDialog).GUID,
							StoragePickType.SaveFile      => typeof(Win32.UI.Shell.FileSaveDialog).GUID,
							_                             => throw new UnreachableException(),
						},
						null,
						Win32.System.Com.CLSCTX.CLSCTX_INPROC_SERVER,
						out var dialog
					).ThrowOnFailure();
					using var dialogFinalizer = new Finalizer(async () => {
						dialog->Release();
					});
					var option = (Win32.UI.Shell.FILEOPENDIALOGOPTIONS)0;
					option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR;
					option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_NODEREFERENCELINKS;
					option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_FORCESHOWHIDDEN;
					if (type == StoragePickType.LoadFile || type == StoragePickType.LoadDirectory) {
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST;
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST;
					}
					if (type == StoragePickType.LoadDirectory) {
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS;
					}
					if (type == StoragePickType.SaveFile) {
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST;
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT;
						option |= Win32.UI.Shell.FILEOPENDIALOGOPTIONS.FOS_NOREADONLYRETURN;
					}
					dialog->SetOptions(option).ThrowOnFailure();
					Win32.PInvoke.SHCreateItemFromParsingName(
						location.EmitNative(),
						null,
						typeof(Win32.UI.Shell.IShellItem).GUID,
						out var locationItem
					).ThrowOnFailure();
					using var locationItemFinalizer = new Finalizer(async () => {
						((Win32.UI.Shell.IShellItem*)locationItem)->Release();
					});
					dialog->SetFolder((Win32.UI.Shell.IShellItem*)locationItem).ThrowOnFailure();
					if (type == StoragePickType.SaveFile) {
						dialog->SetFileName(name).ThrowOnFailure();
					}
					var dialogResult = dialog->Show(new (WindowHelper.GetHandle(host)));
					if (dialogResult != Win32.PInvoke.HRESULT_FROM_WIN32(Win32.Foundation.WIN32_ERROR.ERROR_CANCELLED)) {
						dialogResult.ThrowOnFailure();
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
						target = new (targetPath.ToString());
					}
				}
			});
			return target;
		}

		// ----------------

		public static async Task<StoragePath> Temporary(
		) {
			var parent = App.Instance.CacheDirectory;
			var name = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
			var result = parent.Join(name);
			var suffix = 0;
			while (await StorageHelper.Exist(result)) {
				suffix += 1;
				result = parent.Join($"{name}.{suffix}");
			}
			return result;
		}

		public static async Task Trash(
			StoragePath target
		) {
			AssertTest(await StorageHelper.Exist(target));
			var targetString = target.EmitNative();
			if (await StorageHelper.ExistFile(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(targetString, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			if (await StorageHelper.ExistDirectory(target)) {
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(targetString, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
			}
			return;
		}

		#endregion

		#region miscellaneous

		private static readonly Character[] InvalidPathNameCharacter = Path.GetInvalidFileNameChars();

		public static Boolean CheckName(
			String name
		) {
			if (name.IsEmpty()) {
				return false;
			}
			if (name[0] == ' ' || name[^1] == ' ' || name[^1] == '.') {
				return false;
			}
			return name.All((value) => !StorageHelper.InvalidPathNameCharacter.Contains(value));
		}

		// ----------------

		public static StoragePath ResolveLongPath(
			String source
		) {
			var destinationLength = Win32.PInvoke.GetLongPathName(source, []);
			var destination = new Character[destinationLength];
			var destinationLengthCheck = Win32.PInvoke.GetLongPathName(source, destination.AsSpan());
			AssertTest(destinationLengthCheck == destinationLength - 1);
			return new (new String(destination.AsSpan(0, destinationLength.CastPrimitive<Size>() - 1)));
		}

		#endregion

	}

}
