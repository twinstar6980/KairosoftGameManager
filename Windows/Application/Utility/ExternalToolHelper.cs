#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	#region type

	public record ExternalToolSetting {
		public String Il2cppdumperPath       = default!;
		public String ZipalignPath           = default!;
		public String ApksignerPath          = default!;
		public String ApkCertificateFile     = default!;
		public String ApkCertificatePassword = default!;
	}

	#endregion

	public static class ExternalToolHelper {

		#region basic

		public static ExternalToolSetting ParseSetting (
			ExternalToolSetting original
		) {
			return new () {
				Il2cppdumperPath = original.Il2cppdumperPath.SelfLet((it) => StorageHelper.ExistFile(it) ? it : ProcessHelper.SearchProgram(it) ?? ""),
				ZipalignPath = original.ZipalignPath.SelfLet((it) => StorageHelper.ExistFile(it) ? it : ProcessHelper.SearchProgram(it) ?? ""),
				ApksignerPath = original.ApksignerPath.SelfLet((it) => StorageHelper.ExistFile(it) ? it : ProcessHelper.SearchProgram(it) ?? ""),
				ApkCertificateFile = original.ApkCertificateFile.SelfLet((it) => StorageHelper.ExistFile(it) ? it : ""),
				ApkCertificatePassword = original.ApkCertificatePassword,
			};
		}

		#endregion

		#region special

		public static async Task<List<String>> RunIl2cppdumper (
			ExternalToolSetting setting,
			String              programFile,
			String              metadataFile
		) {
			var processResult = (await ProcessHelper.RunProcess(
				setting.Il2cppdumperPath,
				[
					programFile,
					metadataFile,
				],
				null,
				true
			)).AsNotNull();
			AssertTest(processResult.Item2.ReplaceLineEndings("\n").EndsWith("Done!\nPress any key to exit...\n"));
			var result = (await StorageHelper.ReadFileText($"{StorageHelper.Parent(setting.Il2cppdumperPath)}/dump.cs")).Split('\n').ToList();
			return result;
		}

		// ----------------

		public static async Task RunZipalign (
			ExternalToolSetting setting,
			String              file
		) {
			var alignedFile = StorageHelper.Temporary();
			StorageHelper.CreateFile(alignedFile);
			using var alignedFileFinalizer = new Finalizer(() => {
				StorageHelper.Remove(alignedFile);
			});
			var processResult = (await ProcessHelper.RunProcess(
				setting.ZipalignPath,
				[
					"-P", "16",
					"-f",
					"4",
					$"{file}",
					$"{alignedFile}",
				],
				null,
				true
			)).AsNotNull();
			if (processResult.Item1 != 0) {
				throw new ($"External tool 'zipalign' error.\n{processResult.Item3}\n");
			}
			StorageHelper.Copy(alignedFile, file);
			return;
		}

		// ----------------

		public static async Task RunApksigner (
			ExternalToolSetting setting,
			String              file
		) {
			var processResult = (await ProcessHelper.RunProcess(
				setting.ApksignerPath,
				[
					"sign",
					"--v1-signing-enabled", "true",
					"--v2-signing-enabled", "true",
					"--v3-signing-enabled", "true",
					"--v4-signing-enabled", "false",
					"--ks", $"{setting.ApkCertificateFile}",
					"--ks-pass", $"pass:{setting.ApkCertificatePassword}",
					$"{file}",
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
