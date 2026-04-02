#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	#region type

	public record ExternalToolSetting {
		public StoragePath Il2cppdumperPath    { get; set; } = default!;
		public StoragePath ZipalignPath        { get; set; } = default!;
		public StoragePath ApksignerPath       { get; set; } = default!;
		public StoragePath ApkKeystoreFile     { get; set; } = default!;
		public String      ApkKeystorePassword { get; set; } = default!;
	}

	#endregion

	public static class ExternalToolHelper {

		#region basic

		public static async Task<ExternalToolSetting> ParseSetting(
			ExternalToolSetting original
		) {
			return new () {
				// TODO: convert
				Il2cppdumperPath = await original.Il2cppdumperPath.SelfLet(async (it) => await StorageHelper.ExistFile(it) ? it : await ProcessHelper.SearchProgram(it.Name() ?? "", true) ?? new ()),
				ZipalignPath = await original.ZipalignPath.SelfLet(async (it) => await StorageHelper.ExistFile(it) ? it : await ProcessHelper.SearchProgram(it.Name() ?? "", true) ?? new ()),
				ApksignerPath = await original.ApksignerPath.SelfLet(async (it) => await StorageHelper.ExistFile(it) ? it : await ProcessHelper.SearchProgram(it.Name() ?? "", true) ?? new ()),
				ApkKeystoreFile = await original.ApkKeystoreFile.SelfLet(async (it) => await StorageHelper.ExistFile(it) ? it : new ()),
				ApkKeystorePassword = original.ApkKeystorePassword,
			};
		}

		#endregion

		#region special

		public static async Task<List<String>> RunIl2cppdumper(
			ExternalToolSetting setting,
			StoragePath         programFile,
			StoragePath         metadataFile
		) {
			var dumpDirectory = await StorageHelper.Temporary();
			await StorageHelper.CreateDirectory(dumpDirectory);
			await using var dumpDirectoryFinalizer = new Finalizer(async () => {
				await StorageHelper.Remove(dumpDirectory);
			});
			var processResult = (await ProcessHelper.RunProcess(
				setting.Il2cppdumperPath,
				[
					programFile.EmitGeneric(),
					metadataFile.EmitGeneric(),
					dumpDirectory.EmitGeneric(),
				],
				null,
				true
			)).AsNotNull();
			AssertTest(processResult.Item2.ReplaceLineEndings("\n").EndsWith("Done!\nPress any key to exit...\n"));
			var result = (await StorageHelper.ReadFileText(dumpDirectory.Join("dump.cs"))).Split('\n').ToList();
			return result;
		}

		public static List<Tuple<Size, String, Boolean, String>> DoIl2cppdumperSearchFieldFromDumpData(
			List<String> source,
			String       className,
			String       fieldName
		) {
			var result = new List<Tuple<Size, String, Boolean, String>>();
			var classRule = new Regex(@"^(private|protected|public) class ([^ ]+)");
			var fieldRule = new Regex(@"^\t(private|protected|public)( static)? (.+) (.+); \/\/ 0x(.+)$");
			for (var index = 0; index < source.Count; index++) {
				var classMatch = classRule.Match(source[index]);
				if (!classMatch.Success || classMatch.Groups[2].Value != className) {
					continue;
				}
				for (; index < source.Count; index++) {
					if (source[index] == "}") {
						break;
					}
					var fieldMatch = fieldRule.Match(source[index]);
					if (!fieldMatch.Success || fieldMatch.Groups[4].Value != fieldName) {
						continue;
					}
					result.Add(new (
						Size.Parse(fieldMatch.Groups[5].Value, NumberStyles.HexNumber),
						fieldMatch.Groups[1].Value,
						fieldMatch.Groups[2].Value.Length != 0,
						fieldMatch.Groups[3].Value
					));
				}
				break;
			}
			return result;
		}

		public static List<Tuple<Size, String, Boolean, String, String>> DoIl2cppdumperSearchMethodFromDumpData(
			List<String> source,
			String       className,
			String       methodName
		) {
			var result = new List<Tuple<Size, String, Boolean, String, String>>();
			var classRule = new Regex(@"^(private|protected|public) class ([^ ]+)");
			var methodRule = new Regex(@"^\t(private|protected|public)( static)? (.+) (.+)\((.*)\) \{ \}$");
			var commentRule = new Regex(@"^\t\/\/ RVA: 0x(.+) Offset: 0x(.+) VA: 0x(.+)$");
			for (var index = 0; index < source.Count; index++) {
				var classMatch = classRule.Match(source[index]);
				if (!classMatch.Success || classMatch.Groups[2].Value != className) {
					continue;
				}
				for (; index < source.Count; index++) {
					if (source[index] == "}") {
						break;
					}
					var methodMatch = methodRule.Match(source[index]);
					if (!methodMatch.Success || methodMatch.Groups[4].Value != methodName) {
						continue;
					}
					var commentMatch = commentRule.Match(source[index - 1]);
					AssertTest(commentMatch.Success);
					result.Add(new (
						Size.Parse(commentMatch.Groups[2].Value, NumberStyles.HexNumber),
						methodMatch.Groups[1].Value,
						methodMatch.Groups[2].Value.Length != 0,
						methodMatch.Groups[3].Value,
						methodMatch.Groups[5].Value
					));
				}
				break;
			}
			return result;
		}

		// ----------------

		public static async Task RunZipalign(
			ExternalToolSetting setting,
			StoragePath         zipFile
		) {
			var alignedFile = await StorageHelper.Temporary();
			await StorageHelper.CreateFile(alignedFile);
			await using var alignedFileFinalizer = new Finalizer(async () => {
				await StorageHelper.Remove(alignedFile);
			});
			var processResult = (await ProcessHelper.RunProcess(
				setting.ZipalignPath,
				[
					"-P", "16",
					"-f",
					"4",
					$"{zipFile.EmitGeneric()}",
					$"{alignedFile.EmitGeneric()}",
				],
				null,
				true
			)).AsNotNull();
			if (processResult.Item1 != 0) {
				throw new ($"External tool 'zipalign' error.\n{processResult.Item3}\n");
			}
			await StorageHelper.Remove(zipFile);
			await StorageHelper.Copy(alignedFile, zipFile, false);
			return;
		}

		// ----------------

		public static async Task RunApksigner(
			ExternalToolSetting setting,
			StoragePath         apkFile
		) {
			var processResult = (await ProcessHelper.RunProcess(
				setting.ApksignerPath,
				[
					"sign",
					"--v1-signing-enabled", "true",
					"--v2-signing-enabled", "true",
					"--v3-signing-enabled", "true",
					"--v4-signing-enabled", "false",
					"--ks", $"{setting.ApkKeystoreFile.EmitGeneric()}",
					"--ks-pass", $"pass:{setting.ApkKeystorePassword}",
					$"{apkFile.EmitGeneric()}",
				],
				null,
				true
			)).AsNotNull();
			if (processResult.Item1 != 0) {
				throw new ($"External tool 'apksigner' error.\n{processResult.Item3}\n");
			}
			return;
		}

		#endregion

	}

}
