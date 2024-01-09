#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public static class ProcessHelper {

		#region command line

		private static void EncodeCommandLineProgramString (
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

		private static void EncodeCommandLineArgumentString (
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

		public static String EncodeCommandLineString (
			String?      program,
			List<String> argument
		) {
			var destination = new StringBuilder();
			if (program is not null) {
				ProcessHelper.EncodeCommandLineProgramString(program, destination);
			}
			var first = true;
			foreach (var element in argument) {
				if (program is not null || !first) {
					destination.Append(' ');
				}
				ProcessHelper.EncodeCommandLineArgumentString(element, destination);
				first = false;
			}
			return destination.ToString();
		}

		#endregion

		#region create

		public static async Task<Tuple<Size, String, String>?> CreateProcess (
			String       program,
			List<String> argument,
			Boolean      waitForExit
		) {
			var process = new Process();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = program;
			process.StartInfo.Arguments = ProcessHelper.EncodeCommandLineString(null, argument);
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			var state = process.Start();
			GF.AssertTest(state);
			if (!waitForExit) {
				return null;
			}
			await process.WaitForExitAsync();
			return new (process.ExitCode, await process.StandardOutput.ReadToEndAsync(), await process.StandardError.ReadToEndAsync());
		}

		#endregion

	}

}
