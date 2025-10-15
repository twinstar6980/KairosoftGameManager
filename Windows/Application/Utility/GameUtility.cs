#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Buffers.Binary;
using System.IO.Compression;
using Microsoft.UI.Xaml.Media;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

namespace KairosoftGameManager.Utility {

	#region type

	public enum GamePlatform {
		WindowsX32,
		AndroidA32,
		AndroidA64,
	}

	public enum GameRecordState {
		None,
		Invalid,
		Original,
		Decrypted,
	}

	public enum GameProgramState {
		None,
		Original,
		Modified,
	}

	public enum GamePackageType {
		Flat,
		Zip,
		Apk,
		Apks,
	}

	// ----------------

	public record class GameConfiguration {
		public String           Path     = "";
		public String           Identity = "";
		public String           Version  = "";
		public String           Name     = "";
		public ImageSource?     Icon     = null;
		public String           User     = "";
		public GameRecordState  Record   = GameRecordState.None;
		public GameProgramState Program  = GameProgramState.None;
	}

	public record class GameRecordArchiveConfiguration {
		public String Platform = "";
		public String Identity = "";
		public String Version  = "";
	}

	// ----------------

	public enum GameFunctionType {
		EncryptRecord,
		ModifyProgram,
	}

	#endregion

	public static class GameUtility {

		#region common

		public const String ExecutableFile = "KairoGames.exe";

		public const String ProgramFile = "GameAssembly.dll";

		public const String RecordBundleDirectory = "saves";

		public const String RecordArchiveFileExtension = "kgra";

		#endregion

		#region record

		private static List<String> ListRecordFile (
			String recordDirectory
		) {
			return StorageHelper.ListDirectory(recordDirectory, 1, true, false).Where((value) => new Regex(@"^\d{4,4}(_backup)?$").IsMatch(value)).ToList();
		}

		private static async Task<GameRecordState> DetectRecordState (
			String  recordDirectory,
			Byte[]? key
		) {
			var state = default(GameRecordState);
			var itemNameList = GameUtility.ListRecordFile(recordDirectory);
			if (itemNameList.Count == 0) {
				state = GameRecordState.None;
			}
			else if (!itemNameList.Contains("0000")) {
				state = GameRecordState.Invalid;
			}
			else {
				var itemFile = $"{recordDirectory}/0000";
				var itemData = await StorageHelper.ReadFileLimited(itemFile, 8);
				if (itemData.Length != 8) {
					state = GameRecordState.Invalid;
				}
				else {
					var firstNumber = BinaryPrimitives.ReadUInt32LittleEndian(itemData);
					if (firstNumber == 0x00000000) {
						state = GameRecordState.Decrypted;
					}
					else {
						GameUtility.EncryptRecordData(itemData, key);
						firstNumber = BinaryPrimitives.ReadUInt32LittleEndian(itemData);
						if (firstNumber == 0x00000000) {
							state = GameRecordState.Original;
						}
						else {
							state = GameRecordState.Invalid;
						}
					}
				}
			}
			return state;
		}

		// ----------------

		private static void EncryptRecordData (
			Byte[]  data,
			Byte[]? key
		) {
			if (key != null && key.Length != 0) {
				for (var index = 0; index < data.Length; index++) {
					data[index] ^= key[index % key.Length];
				}
			}
			return;
		}

		private static async Task EncryptRecordFile (
			String  sourceFile,
			String  destinationFile,
			Byte[]? key
		) {
			var data = await StorageHelper.ReadFile(sourceFile);
			GameUtility.EncryptRecordData(data, key);
			await StorageHelper.WriteFile(destinationFile, data);
			return;
		}

		public static async Task EncryptRecord (
			String         targetDirectory,
			Byte[]         key,
			Action<String> onNotify
		) {
			var itemNameList = GameUtility.ListRecordFile(targetDirectory);
			if (itemNameList.Count == 0) {
				onNotify($"The target directory does not contain a record file.");
			}
			foreach (var itemName in itemNameList) {
				onNotify($"Phase: {itemName}.");
				await GameUtility.EncryptRecordFile($"{targetDirectory}/{itemName}", $"{targetDirectory}/{itemName}", key);
			}
			return;
		}

		// ----------------

		public static String MakeRecordArchiveConfigurationText (
			GameRecordArchiveConfiguration value
		) {
			return String.Join(':', [value.Platform, value.Identity, value.Version]);
		}

		public static GameRecordArchiveConfiguration ParseRecordArchiveConfigurationText (
			String text
		) {
			var list = text.Split(':');
			return new () {
				Platform = list[0],
				Identity = list[1],
				Version = list[2],
			};
		}

		// ----------------

		public static async Task ImportRecordArchive (
			String                                              archiveFile,
			String                                              targetDirectory,
			Byte[]?                                             key,
			Func<GameRecordArchiveConfiguration, Task<Boolean>> doArchiveConfiguration
		) {
			var archiveDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(archiveDirectory);
			using var archiveDirectoryFinalizer = new Finalizer(() => {
				StorageHelper.Remove(archiveDirectory);
			});
			// load
			ZipFile.ExtractToDirectory(archiveFile, archiveDirectory, Encoding.UTF8);
			// configuration
			var archiveConfiguration = GameUtility.ParseRecordArchiveConfigurationText(await StorageHelper.ReadFileText($"{archiveDirectory}/configuration.txt"));
			if (!await doArchiveConfiguration(archiveConfiguration)) {
				return;
			}
			// data
			StorageHelper.CreateDirectory(targetDirectory);
			foreach (var dataFile in GameUtility.ListRecordFile($"{archiveDirectory}/data")) {
				await GameUtility.EncryptRecordFile($"{archiveDirectory}/data/{dataFile}", $"{targetDirectory}/{dataFile}", key);
			}
			return;
		}

		public static async Task ExportRecordArchive (
			String                                              archiveFile,
			String                                              targetDirectory,
			Byte[]?                                             key,
			Func<GameRecordArchiveConfiguration, Task<Boolean>> doArchiveConfiguration
		) {
			var archiveDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(archiveDirectory);
			using var archiveDirectoryFinalizer = new Finalizer(() => {
				StorageHelper.Remove(archiveDirectory);
			});
			// configuration
			var archiveConfiguration = new GameRecordArchiveConfiguration();
			if (!await doArchiveConfiguration(archiveConfiguration)) {
				return;
			}
			await StorageHelper.WriteFileText($"{archiveDirectory}/configuration.txt", GameUtility.MakeRecordArchiveConfigurationText(archiveConfiguration));
			// data
			StorageHelper.CreateDirectory($"{archiveDirectory}/data");
			foreach (var dataFile in GameUtility.ListRecordFile(targetDirectory)) {
				await GameUtility.EncryptRecordFile($"{targetDirectory}/{dataFile}", $"{archiveDirectory}/data/{dataFile}", key);
			}
			// save
			ZipFile.CreateFromDirectory(archiveDirectory, archiveFile, CompressionLevel.SmallestSize, false, Encoding.UTF8);
			return;
		}

		#endregion

		#region program

		private static String GetProgramFilePath (
			GamePlatform platform
		) {
			return platform switch {
				GamePlatform.WindowsX32 => "GameAssembly.dll",
				GamePlatform.AndroidA32 => "lib/armeabi-v7a/libil2cpp.so",
				GamePlatform.AndroidA64 => "lib/arm64-v8a/libil2cpp.so",
				_                       => throw new UnreachableException(),
			};
		}

		private static String GetMetadataFilePath (
			GamePlatform platform
		) {
			return platform switch {
				GamePlatform.WindowsX32 => "KairoGames_Data/il2cpp_data/Metadata/global-metadata.dat",
				GamePlatform.AndroidA32 => "assets/bin/Data/Managed/Metadata/global-metadata.dat",
				GamePlatform.AndroidA64 => "assets/bin/Data/Managed/Metadata/global-metadata.dat",
				_                       => throw new UnreachableException(),
			};
		}

		private static List<GamePlatform> DetectPlatform (
			String gameDirectory
		) {
			var result = new List<GamePlatform>();
			foreach (var platform in Enum.GetValues<GamePlatform>()) {
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.GetProgramFilePath(platform)}")) {
					continue;
				}
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.GetMetadataFilePath(platform)}")) {
					continue;
				}
				result.Add(platform);
			}
			return result;
		}

		// ----------------

		private static Tuple<Size, String, Boolean, String>? SearchFieldFromDumpData (
			List<String> source,
			String       className,
			String       fieldName
		) {
			var result = null as Tuple<Size, String, Boolean, String>;
			for (var index = 0; index < source.Count; index++) {
				var classMatch = new Regex(@"^(private|protected|public)? class ([^ ]+)?").Match(source[index]);
				if (classMatch.Success && classMatch.Groups[2].Value == className) {
					for (; index < source.Count; index++) {
						if (source[index] == "}") {
							break;
						}
						var fieldMatch = new Regex(@"^\t(private|protected|public)?( static)? (.+) (.+); \/\/ 0x(.+)$").Match(source[index]);
						if (fieldMatch.Success && fieldMatch.Groups[4].Value == fieldName) {
							result = new (
								Size.Parse(fieldMatch.Groups[5].Value, NumberStyles.HexNumber),
								fieldMatch.Groups[1].Value,
								fieldMatch.Groups[2].Value == " static",
								fieldMatch.Groups[3].Value
							);
							break;
						}
					}
					break;
				}
			}
			return result;
		}

		private static List<Tuple<Size, String, Boolean, String, String>> SearchMethodFromDumpData (
			List<String> source,
			String       className,
			String       methodName
		) {
			var result = new List<Tuple<Size, String, Boolean, String, String>>();
			for (var index = 0; index < source.Count; index++) {
				var classMatch = new Regex(@"^(private|protected|public)? class ([^ ]+)?").Match(source[index]);
				if (classMatch.Success && classMatch.Groups[2].Value == className) {
					for (; index < source.Count; index++) {
						if (source[index] == "}") {
							break;
						}
						var methodMatch = new Regex(@"^\t(private|protected|public)?( static)? (.+) (.+)\((.*)\) \{ \}$").Match(source[index]);
						if (methodMatch.Success && methodMatch.Groups[4].Value == methodName) {
							var commentMatch = new Regex(@"^\t\/\/ RVA: 0x(.+) Offset: 0x(.+) VA: 0x(.+)$").Match(source[index - 1]);
							GF.AssertTest(commentMatch.Success);
							result.Add(new (
								Size.Parse(commentMatch.Groups[2].Value, NumberStyles.HexNumber),
								methodMatch.Groups[1].Value,
								methodMatch.Groups[2].Value == " static",
								methodMatch.Groups[3].Value,
								methodMatch.Groups[5].Value
							));
						}
					}
					break;
				}
			}
			return result;
		}

		// ----------------

		private const IntegerU8 InstructionCodeNopX32 = 0x90;

		private const IntegerU32 InstructionCodeNopA32 = 0xE320F000;

		private const IntegerU32 InstructionCodeNopA64 = 0xD503201F;

		private static Boolean FindCallInstruction (
			Span<Byte>   data,
			ref Size     position,
			Size         limit,
			List<Size>   address,
			Boolean      overwrite,
			GamePlatform platform
		) {
			var state = false;
			var end = Math.Min(data.Length, position + limit);
			if (platform == GamePlatform.WindowsX32) {
				while (position < end) {
					var instructionCode = data[position];
					position += 1;
					// call #X = E8 XX XX XX XX
					if (instructionCode != 0xE8u) {
						continue;
					}
					var jumpOffset = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(position, 4));
					position += 4;
					var jumpAddress = position + jumpOffset;
					if (!address.Contains(jumpAddress)) {
						position -= 4;
						continue;
					}
					if (overwrite) {
						position -= 5;
						data[position++] = GameUtility.InstructionCodeNopX32;
						data[position++] = GameUtility.InstructionCodeNopX32;
						data[position++] = GameUtility.InstructionCodeNopX32;
						data[position++] = GameUtility.InstructionCodeNopX32;
						data[position++] = GameUtility.InstructionCodeNopX32;
					}
					state = true;
					break;
				}
			}
			if (platform == GamePlatform.AndroidA32) {
				while (position < end) {
					var instructionCode = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(position, 4));
					position += 4;
					// bl #X = EB XX XX XX
					if ((instructionCode & 0xFF000000u) != 0xEB000000u) {
						continue;
					}
					var jumpOffset = (instructionCode & 0x00FFFFFFu).CastPrimitive<Size>();
					if ((jumpOffset & 0x800000u) == 0x800000u) {
						jumpOffset = -(0x1000000 - jumpOffset);
					}
					var jumpAddress = position - 4 + 8 + jumpOffset * 4;
					if (!address.Contains(jumpAddress)) {
						continue;
					}
					if (overwrite) {
						BinaryPrimitives.TryWriteUInt32LittleEndian(data.Slice(position - 4, 4), GameUtility.InstructionCodeNopA32);
					}
					state = true;
					break;
				}
			}
			if (platform == GamePlatform.AndroidA64) {
				while (position < end) {
					var instructionCode = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(position, 4));
					position += 4;
					// bl #X = 97 XX XX XX
					if ((instructionCode & 0xFF000000u) != 0x97000000u) {
						continue;
					}
					var jumpOffset = (instructionCode & 0x00FFFFFFu).CastPrimitive<Size>();
					if ((jumpOffset & 0x800000u) == 0x800000u) {
						jumpOffset = -(0x1000000 - jumpOffset);
					}
					var jumpAddress = position - 4 + jumpOffset * 4;
					if (!address.Contains(jumpAddress)) {
						continue;
					}
					if (overwrite) {
						BinaryPrimitives.TryWriteUInt32LittleEndian(data.Slice(position - 4, 4), GameUtility.InstructionCodeNopA64);
					}
					state = true;
					break;
				}
			}
			return state;
		}

		// ----------------

		private static async Task ModifyProgramFlat (
			GamePlatform   platform,
			String         programFile,
			String         metadataFile,
			Boolean        disableRecordEncryption,
			Boolean        enableDebugMode,
			String         programFileOfIl2CppDumper,
			Action<String> onNotify
		) {
			var dumpData = new List<String>();
			onNotify($"Phase: dump program information.");
			{
				onNotify($"The Il2CppDumper path is '{programFileOfIl2CppDumper}'.");
				GF.AssertTest(StorageHelper.ExistFile(programFileOfIl2CppDumper));
				var dumpResult = await ProcessHelper.RunProcess(programFileOfIl2CppDumper, [programFile, metadataFile], true);
				GF.AssertTest(dumpResult != null);
				GF.AssertTest(dumpResult.Item2.ReplaceLineEndings("\n").EndsWith("Done!\nPress any key to exit...\n"));
				dumpData = (await StorageHelper.ReadFileText($"{StorageHelper.Parent(programFileOfIl2CppDumper)}/dump.cs")).Split('\n').ToList();
			}
			var symbolAddress = new {
				CRC64 = new {
					GetValue = new List<Size>(),
				},
				Encrypter = new {
					Encode = new List<Size>(),
					Decode = new List<Size>(),
				},
				RecordStore = new {
					ReadRecord = new List<Size>(),
					WriteRecord = new List<Size>(),
				},
				MyConfig = new {
					_cctor = new List<Size>(),
					DEBUG = new List<Size>(),
				},
			};
			onNotify($"Phase: parse symbol address.");
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "CRC64", "GetValue");
				GF.AssertTest(searchResult.Count == 1);
				symbolAddress.CRC64.GetValue.AddRange(searchResult.Select((value) => value.Item1));
				onNotify($"The symbol 'CRC64.GetValue' at {String.Join(',', symbolAddress.CRC64.GetValue.Select((value) => ($"{value:x}")))}.");
			}
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "Encrypter", "Encode");
				GF.AssertTest(searchResult.Count == 3);
				symbolAddress.Encrypter.Encode.AddRange(searchResult.Select((value) => value.Item1));
				onNotify($"The symbol 'Encrypter.Encode' at {String.Join(',', symbolAddress.Encrypter.Encode.Select((value) => ($"{value:x}")))}.");
			}
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "Encrypter", "Decode");
				GF.AssertTest(searchResult.Count == 3);
				symbolAddress.Encrypter.Decode.AddRange(searchResult.Select((value) => value.Item1));
				onNotify($"The symbol 'Encrypter.Decode' at {String.Join(',', symbolAddress.Encrypter.Decode.Select((value) => ($"{value:x}")))}.");
			}
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "RecordStore", "ReadRecord").Where((value) => (!value.Item3 && value.Item5 == "int rcId")).ToList();
				GF.AssertTest(searchResult.Count == 1);
				symbolAddress.RecordStore.ReadRecord.Add(searchResult[0].Item1);
				onNotify($"The symbol 'RecordStore.ReadRecord' at {symbolAddress.RecordStore.ReadRecord[0]:x}.");
			}
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "RecordStore", "WriteRecord").Where((value) => (!value.Item3 && value.Item5 == "int rcId, byte[][] data")).ToList();
				GF.AssertTest(searchResult.Count == 1);
				symbolAddress.RecordStore.WriteRecord.Add(searchResult[0].Item1);
				onNotify($"The symbol 'RecordStore.WriteRecord' at {symbolAddress.RecordStore.WriteRecord[0]:x}.");
			}
			{
				var searchResult = GameUtility.SearchMethodFromDumpData(dumpData, "MyConfig", ".cctor");
				GF.AssertTest(searchResult.Count == 1);
				symbolAddress.MyConfig._cctor.Add(searchResult[0].Item1);
				onNotify($"The symbol 'MyConfig..cctor' at {symbolAddress.MyConfig._cctor[0]:x}.");
			}
			{
				var searchResult = GameUtility.SearchFieldFromDumpData(dumpData, "MyConfig", "DEBUG");
				GF.AssertTest(searchResult != null);
				symbolAddress.MyConfig.DEBUG.Add(searchResult.Item1);
				onNotify($"The symbol 'MyConfig.DEBUG' at +{symbolAddress.MyConfig.DEBUG[0]:x}.");
			}
			onNotify($"Phase: load original program.");
			var programData = await StorageHelper.ReadFile(programFile);
			var programPosition = 0;
			if (disableRecordEncryption) {
				onNotify($"Phase: modify method 'RecordStore.ReadRecord'.");
				programPosition = symbolAddress.RecordStore.ReadRecord[0];
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.CRC64.GetValue, false, platform));
				if (platform == GamePlatform.WindowsX32) {
					// add esp, .. = 83 C4 XX
					GF.AssertTest(programData[programPosition] == 0x83);
					programPosition++;
					GF.AssertTest(programData[programPosition] == 0xC4);
					programPosition++;
					programPosition++;
					// cmp eax, .. = 3B XX XX
					GF.AssertTest(programData[programPosition] == 0x3B);
					programData[programPosition++] = GameUtility.InstructionCodeNopX32;
					programData[programPosition++] = GameUtility.InstructionCodeNopX32;
					programData[programPosition++] = GameUtility.InstructionCodeNopX32;
					// jnz .. = 75 XX
					GF.AssertTest(programData[programPosition] == 0x75);
					programData[programPosition++] = GameUtility.InstructionCodeNopX32;
					programData[programPosition++] = GameUtility.InstructionCodeNopX32;
				}
				if (platform == GamePlatform.AndroidA32) {
					programPosition += 12;
					// bne #X = 1A XX XX XX
					GF.AssertTest((BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4)) & 0xFF000000) == 0x1A000000);
					BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4), GameUtility.InstructionCodeNopA32);
					programPosition += 4;
				}
				if (platform == GamePlatform.AndroidA64) {
					programPosition += 8;
					// bne #X = 54 XX XX XX
					GF.AssertTest((BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4)) & 0xFF000000) == 0x54000000);
					BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4), GameUtility.InstructionCodeNopA64);
					programPosition += 4;
				}
			}
			if (disableRecordEncryption) {
				onNotify($"Phase: modify method 'RecordStore.WriteRecord'.");
				programPosition = symbolAddress.RecordStore.WriteRecord[0];
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform));
			}
			if (enableDebugMode) {
				onNotify($"Phase: modify method 'MyConfig..cctor'.");
				programPosition = symbolAddress.MyConfig._cctor[0];
				var searchLimit = 512;
				if (platform == GamePlatform.WindowsX32) {
					while (programPosition < symbolAddress.MyConfig._cctor[0] + searchLimit) {
						// mov byte ptr [eax+X], 0 = C6 40 XX 00
						if (programData[programPosition] != 0xC6) {
							programPosition++;
							continue;
						}
						programPosition++;
						if (programData[programPosition] != 0x40) {
							continue;
						}
						programPosition++;
						if (programData[programPosition] != symbolAddress.MyConfig.DEBUG[0]) {
							continue;
						}
						programPosition++;
						if (programData[programPosition] != 0x00) {
							continue;
						}
						programData[programPosition++] = 0x01;
						onNotify($"Warning: the STR instruction for 'MyConfig.DEBUG' was found at {(programPosition - 4):x}, but this modification may cause error.");
						break;
					}
					GF.AssertTest(programPosition != symbolAddress.MyConfig._cctor[0] + searchLimit);
				}
				if (platform == GamePlatform.AndroidA32) {
					while (programPosition < symbolAddress.MyConfig._cctor[0] + searchLimit) {
						// strb rX, [rY, #Z] = 111001011100 YYYY XXXX ZZZZZZZZZZZZ
						var instructionCode = BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4));
						programPosition += 4;
						if ((instructionCode & 0b111111111111_0000_0000_000000000000) != 0b111001011100_0000_0000_000000000000) {
							continue;
						}
						if ((instructionCode & 0b000000000000_0000_0000_111111111111) >> 0 != symbolAddress.MyConfig.DEBUG.First() + 4) {
							continue;
						}
						BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition - 4, 4), (instructionCode & 0b111111111111_1111_0000_000000000000u) | (14u << 12) | (symbolAddress.MyConfig.DEBUG.First().CastPrimitive<SizeU>() << 0));
						onNotify($"Warning: the STR instruction for 'MyConfig.DEBUG'+4 was found at {(programPosition - 4):x}, but this modification may cause error.");
						break;
					}
					GF.AssertTest(programPosition != symbolAddress.MyConfig._cctor[0] + searchLimit);
				}
				if (platform == GamePlatform.AndroidA64) {
					while (programPosition < symbolAddress.MyConfig._cctor[0] + searchLimit) {
						// strb wX, [xY, #Z] = 0011100100 ZZZZZZZZZZZZ YYYYY XXXXX
						var instructionCode = BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4));
						programPosition += 4;
						if ((instructionCode & 0b1111111111_000000000000_00000_00000) != 0b0011100100_000000000000_00000_00000) {
							continue;
						}
						if ((instructionCode & 0b0000000000_111111111111_00000_00000) >> 10 != symbolAddress.MyConfig.DEBUG.First() + 4) {
							continue;
						}
						BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition - 4, 4), (instructionCode & 0b1111111111_000000000000_11111_00000u) | (symbolAddress.MyConfig.DEBUG.First().CastPrimitive<SizeU>() << 10) | (30u << 0));
						onNotify($"Warning: the STR instruction for 'MyConfig.DEBUG'+4 was found at {(programPosition - 4):x}, but this modification may cause error.");
						break;
					}
					GF.AssertTest(programPosition != symbolAddress.MyConfig._cctor[0] + searchLimit);
				}
			}
			onNotify($"Phase: save modified program.");
			await StorageHelper.WriteFile(programFile, programData);
			return;
		}

		public static async Task ModifyProgram (
			String         target,
			Boolean        disableRecordEncryption,
			Boolean        enableDebugMode,
			String         programFileOfIl2CppDumper,
			Action<String> onNotify
		) {
			var temporaryDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(temporaryDirectory);
			using var temporaryDirectoryFinalizer = new Finalizer(() => {
				StorageHelper.Remove(temporaryDirectory);
			});
			onNotify($"Phase: detect package type.");
			var packageType = default(GamePackageType?);
			if (StorageHelper.ExistDirectory(target)) {
				packageType = GamePackageType.Flat;
			}
			if (StorageHelper.ExistFile(target)) {
				if (target.ToLower().EndsWith(".zip")) {
					packageType = GamePackageType.Zip;
				}
				if (target.ToLower().EndsWith(".apk")) {
					packageType = GamePackageType.Apk;
				}
				if (target.ToLower().EndsWith(".apks")) {
					packageType = GamePackageType.Apks;
				}
			}
			GF.AssertTest(packageType != null);
			onNotify($"The package type is '{packageType}'.");
			var targetDirectory = default(String?);
			if (packageType == GamePackageType.Flat) {
				targetDirectory = target;
			}
			else {
				onNotify($"Phase: unpack package file.");
				targetDirectory = $"{temporaryDirectory}/flat";
				if (packageType == GamePackageType.Zip || packageType == GamePackageType.Apk) {
					ZipFile.ExtractToDirectory(target, targetDirectory);
				}
				if (packageType == GamePackageType.Apks) {
					ZipFile.ExtractToDirectory(target, $"{temporaryDirectory}/apks");
					foreach (var apk in StorageHelper.ListDirectory($"{temporaryDirectory}/apks", 1, true, false, "*.apk")) {
						ZipFile.ExtractToDirectory($"{temporaryDirectory}/apks/{apk}", targetDirectory, true);
					}
				}
			}
			onNotify($"Phase: detect platform.");
			var platformList = GameUtility.DetectPlatform(targetDirectory);
			GF.AssertTest(platformList.Count != 0);
			onNotify($"The platform is '{String.Join('|', platformList)}'.");
			foreach (var platform in platformList) {
				onNotify($"Phase: modify program of '{platform}'.");
				await GameUtility.ModifyProgramFlat(
					platform,
					$"{targetDirectory}/{GameUtility.GetProgramFilePath(platform)}",
					$"{targetDirectory}/{GameUtility.GetMetadataFilePath(platform)}",
					disableRecordEncryption,
					enableDebugMode,
					programFileOfIl2CppDumper,
					onNotify
				);
			}
			if (packageType != GamePackageType.Flat) {
				onNotify($"Phase: repack package file.");
				using var package = ZipFile.Open(target, ZipArchiveMode.Update);
				foreach (var platform in platformList) {
					var programFile = $"{targetDirectory}/{GameUtility.GetProgramFilePath(platform)}";
					if (packageType == GamePackageType.Zip || packageType == GamePackageType.Apk) {
						package.GetEntry(GameUtility.GetProgramFilePath(platform)).AsNotNull().Delete();
						package.CreateEntryFromFile(programFile, GameUtility.GetProgramFilePath(platform));
					}
					if (packageType == GamePackageType.Apks) {
						var architectureName = platform switch {
							GamePlatform.AndroidA32 => "armeabi_v7a",
							GamePlatform.AndroidA64 => "arm64_v8a",
							_                       => throw new UnreachableException(),
						};
						var subPackageName = $"split_config.{architectureName}.apk";
						var subPackageFile = $"{temporaryDirectory}/apks/{subPackageName}";
						{
							using var subPackage = ZipFile.Open(subPackageFile, ZipArchiveMode.Update);
							subPackage.GetEntry(GameUtility.GetProgramFilePath(platform)).AsNotNull().Delete();
							subPackage.CreateEntryFromFile(programFile, GameUtility.GetProgramFilePath(platform));
						}
						package.GetEntry(subPackageName).AsNotNull().Delete();
						package.CreateEntryFromFile(subPackageFile, subPackageName);
					}
				}
			}
			return;
		}

		#endregion

		#region repository

		public static Byte[] ConvertKeyFromUser (
			String user
		) {
			var keyValue = IntegerU64.Parse(user);
			var key = new Byte[8];
			BinaryPrimitives.TryWriteUInt64LittleEndian(key, keyValue);
			return key;
		}

		// ----------------

		public static async Task<GameConfiguration?> LoadGameConfiguration (
			String repositoryDirectory,
			String gameIdentity
		) {
			var gameManifestFile = $"{repositoryDirectory}/steamapps/appmanifest_{gameIdentity}.acf";
			if (!StorageHelper.ExistFile(gameManifestFile)) {
				return null;
			}
			var gameManifest = VdfConvert.Deserialize(await StorageHelper.ReadFileText(gameManifestFile));
			GF.AssertTest(gameManifest.Key == "AppState");
			GF.AssertTest(gameManifest.Value["appid"].AsNotNull().Value<String>() == gameIdentity);
			var gameDirectory = $"{repositoryDirectory}/steamapps/common/{gameManifest.Value["installdir"].AsNotNull().Value<String>()}";
			if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.ExecutableFile}")) {
				return null;
			}
			var configuration = new GameConfiguration();
			configuration.Path = gameDirectory;
			configuration.Identity = gameIdentity;
			configuration.Version = gameManifest.Value["buildid"].AsNotNull().Value<String>();
			configuration.Name = gameManifest.Value["name"].AsNotNull().Value<String>();
			configuration.Icon = await ConvertHelper.ParseBitmapFromGdiBitmap(System.Drawing.Icon.ExtractIcon($"{gameDirectory}/{GameUtility.ExecutableFile}", 0, 48).AsNotNull().ToBitmap());
			configuration.User = gameManifest.Value["LastOwner"].AsNotNull().Value<String>();
			if (!StorageHelper.ExistDirectory($"{gameDirectory}/{GameUtility.RecordBundleDirectory}/{configuration.User}")) {
				configuration.Record = GameRecordState.None;
			}
			else {
				configuration.Record = await GameUtility.DetectRecordState($"{gameDirectory}/{GameUtility.RecordBundleDirectory}/{configuration.User}", GameUtility.ConvertKeyFromUser(configuration.User));
			}
			if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.ProgramFile}")) {
				configuration.Program = GameProgramState.None;
			}
			else {
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.ProgramFile}.{configuration.Version}.bak")) {
					configuration.Program = GameProgramState.Original;
				}
				else {
					configuration.Program = GameProgramState.Modified;
				}
			}
			return configuration;
		}

		public static async Task<List<String>> ListGameInRepository (
			String repositoryDirectory
		) {
			var steamLibrary = VdfConvert.Deserialize(await StorageHelper.ReadFileText($"{repositoryDirectory}/steamapps/libraryfolders.vdf"));
			GF.AssertTest(steamLibrary.Key == "libraryfolders");
			return steamLibrary.Value["0"].AsNotNull()["apps"].AsNotNull().Children().Select((value) => value.Value<VProperty>().Key).ToList();
		}

		public static async Task<Boolean> CheckRepositoryDirectory (
			String repositoryDirectory
		) {
			return StorageHelper.ExistFile($"{repositoryDirectory}/steam.exe");
		}

		#endregion

	}

}
