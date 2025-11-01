#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class ProcessHelper {

		#region environment

		public static String? QueryEnvironment (
			String name
		) {
			return Environment.GetEnvironmentVariable(name);
		}

		// ----------------

		public static Dictionary<String, String> ListEnvironment (
		) {
			return Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(
				(item) => item.Key.As<String>(),
				(item) => item.Value.AsNotNull().As<String>()
			);
		}

		#endregion

		#region process

		public static async Task<Tuple<Size, String, String>?> RunProcess (
			String                      program,
			List<String>                argument,
			Dictionary<String, String>? environment,
			Boolean                     waitForExit
		) {
			if (environment == null) {
				environment = ProcessHelper.ListEnvironment();
			}
			using var process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = program;
			foreach (var argumentItem in argument) {
				process.StartInfo.ArgumentList.Add(argumentItem);
			}
			process.StartInfo.Environment.Clear();
			foreach (var environmentItem in environment) {
				process.StartInfo.Environment.Add(environmentItem.Key, environmentItem.Value);
			}
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			var state = process.Start();
			AssertTest(state);
			if (!waitForExit) {
				return null;
			}
			await process.WaitForExitAsync();
			return new (process.ExitCode, await process.StandardOutput.ReadToEndAsync(), await process.StandardError.ReadToEndAsync());
		}

		#endregion

		#region program

		public static String? SearchProgram (
			String name
		) {
			var result = default(String?);
			var pathList = ProcessHelper.QueryEnvironment("PATH").AsNotNull().Split(";").Select(StorageHelper.Regularize).ToList();
			var pathExtensionList = ProcessHelper.QueryEnvironment("PATHEXT").AsNotNull().Split(";").ToList();
			pathExtensionList.Insert(0, "");
			foreach (var path in pathList) {
				var pathBase = $"{path}/{name}";
				var pathExtension = pathExtensionList.FirstOrDefault((value) => (StorageHelper.ExistFile($"{pathBase}{value}")));
				if (pathExtension != null) {
					result = $"{pathBase}{pathExtension}";
					break;
				}
			}
			return result;
		}

		#endregion

		#region command

		private static void EncodeCommandProgramString (
			String        source,
			StringBuilder destination
		) {
			destination.Append('"');
			foreach (var element in source) {
				if (element == '/') {
					destination.Append('\\');
				}
				else {
					destination.Append(element);
				}
			}
			destination.Append('"');
			return;
		}

		private static void EncodeCommandArgumentString (
			String        source,
			StringBuilder destination
		) {
			var currentBackslashCount = 0;
			destination.Append('"');
			foreach (var element in source) {
				if (element == '"') {
					for (var index = 0; index < currentBackslashCount; index++) {
						destination.Append('\\');
					}
					destination.Append('\\');
				}
				destination.Append(element);
				if (element == '\\') {
					currentBackslashCount += 1;
				}
				else {
					currentBackslashCount = 0;
				}
			}
			for (var index = 0; index < currentBackslashCount; index++) {
				destination.Append('\\');
			}
			destination.Append('"');
			return;
		}

		public static String EncodeCommandString (
			String?      program,
			List<String> argument
		) {
			var destination = new StringBuilder();
			if (program != null) {
				ProcessHelper.EncodeCommandProgramString(program, destination);
			}
			var first = true;
			foreach (var element in argument) {
				if (program != null || !first) {
					destination.Append(' ');
				}
				ProcessHelper.EncodeCommandArgumentString(element, destination);
				first = false;
			}
			return destination.ToString();
		}

		#endregion

	}

}
