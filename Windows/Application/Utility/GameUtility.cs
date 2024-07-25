#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;
using System.Buffers.Binary;
using System.IO.Compression;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.UI.Xaml.Media;

namespace KairosoftGameManager.Utility {

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

	public static class GameUtility {

		public static readonly SortedDictionary<String, List<String>> TestedGame = new () {
			// Hot Springs Story
			{ "1823710", ["9883792", "13332097"] },
			// Game Dev Story
			{ "1847240", ["12865314", "13584515"] },
			// Dungeon Village
			{ "1859360", ["9882689", "13493523"] },
			// Dream House Days DX
			{ "1859370", ["10513663", "13652821"] },
			// Nanja Village
			{ "1918520", ["13560678"] },
			// Pocket Academy
			{ "1921760", ["11746770", "13652705"] },
			// Mega Mall Story
			{ "1923680", ["9259357", "13642430"] },
			// Pool Slide Story
			{ "1933980", ["9889731", "13698186"] },
			// Burger Bistro Story
			{ "1933990", ["10314100", "13321147", "13698407"] },
			// Grand Prix Story
			{ "1978100", ["9835394", "13641093"] },
			// Forest Camp Story
			{ "1983690", ["10825670", "13343525"] },
			// Dungeon Village 2
			{ "1983710", ["12756185", "13450272"] },
			// Magazine Mogul
			{ "2054800", ["13708967"] },
			// Tennis Club Story
			{ "2054810", ["9784452", "13735556"] },
			// Pocket Arcade Story
			{ "2072130", ["13735337"] },
			// Shiny Ski Resort
			{ "2072170", ["9735034", "13515402"] },
			// 8-Bit Farm
			{ "2074810", ["13769596"] },
			// Home Run High
			{ "2084720", ["9720692", "13652157"] },
			// Biz Builder Delux
			{ "2102480", ["13721698"] },
			// Basketball Club Story
			{ "2102490", ["10915393", "13628976", "13667306"] },
			// Dream Park Story
			{ "2119660", ["12769873", "13494212"] },
			// Forest Golf Planner
			{ "2119670", ["11577947", "13664559"] },
			// Jumbo Airport Story
			{ "2191480", ["12579631", "13561083"] },
			// Zoo Park Story
			{ "2437690", ["12409789", "13333913"] },
		};

		public static Boolean IsTestedGame (
			SortedDictionary<String, List<String>> table,
			String                                 identity,
			String?                                version
		) {
			return table.TryGetValue(identity, out var versionList) && (version == null || versionList.Contains(version));
		}

		// ----------------

		public const String ExecutableFile = "KairoGames.exe";

		public const String ProgramFile = "GameAssembly.dll";

		public const String RecordBundleDirectory = "saves";

		public const String RecordArchiveFileExtension = "kgra";

		public const String BackupDirectory = ".backup";

		public const String BackupProgramFile = "program";

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

		private static GamePlatform? DetectGamePlatform (
			String gameDirectory
		) {
			var type = null as GamePlatform?;
			if (StorageHelper.ExistFile($"{gameDirectory}/KairoGames.exe")) {
				if (StorageHelper.ExistFile($"{gameDirectory}/GameAssembly.dll")) {
					type = GamePlatform.WindowsX32;
				}
			}
			if (StorageHelper.ExistFile($"{gameDirectory}/AndroidManifest.xml")) {
				if (StorageHelper.ExistFile($"{gameDirectory}/lib/armeabi-v7a/libil2cpp.so")) {
					type = GamePlatform.AndroidA32;
				}
				if (StorageHelper.ExistFile($"{gameDirectory}/lib/arm64-v8a/libil2cpp.so")) {
					type = GamePlatform.AndroidA64;
				}
			}
			return type;
		}

		private static List<String> ListRecordFile (
			String recordDirectory
		) {
			return StorageHelper.ListFile(recordDirectory, 0).Where((value) => new Regex(@"^\d{4,4}(_backup)?$").IsMatch(value)).ToList();
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
								fieldMatch.Groups[2].Value == "static",
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
								methodMatch.Groups[2].Value == "static",
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
					var jumpOffset = (Size)(instructionCode & 0x00FFFFFFu);
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
					var jumpOffset = (Size)(instructionCode & 0x00FFFFFFu);
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

		public static async Task ModifyProgram (
			String         targetDirectory,
			Boolean        disableRecordEncryption,
			Boolean        enableDebugMode,
			String         programFileOfIl2CppDumper,
			String?        backupToken,
			Action<String> onNotify
		) {
			onNotify($"Phase: detect game platform.");
			var platform = GameUtility.DetectGamePlatform(targetDirectory);
			GF.AssertTest(platform != null);
			onNotify($"The game platform is '{platform}'.");
			var programFile = platform switch {
				GamePlatform.WindowsX32 => $"{targetDirectory}/GameAssembly.dll",
				GamePlatform.AndroidA32 => $"{targetDirectory}/lib/armeabi-v7a/libil2cpp.so",
				GamePlatform.AndroidA64 => $"{targetDirectory}/lib/arm64-v8a/libil2cpp.so",
				_                       => throw new (),
			};
			var metadataFile = platform switch {
				GamePlatform.WindowsX32 => $"{targetDirectory}/KairoGames_Data/il2cpp_data/Metadata/global-metadata.dat",
				GamePlatform.AndroidA32 => $"{targetDirectory}/assets/bin/Data/Managed/Metadata/global-metadata.dat",
				GamePlatform.AndroidA64 => $"{targetDirectory}/assets/bin/Data/Managed/Metadata/global-metadata.dat",
				_                       => throw new (),
			};
			var programBackupFile = $"{targetDirectory}/{GameUtility.BackupDirectory}{(backupToken == null ? "" : $"_{backupToken}")}/{GameUtility.BackupProgramFile}";
			onNotify($"Phase: check game file.");
			GF.AssertTest(StorageHelper.ExistFile(programFile) || StorageHelper.ExistFile(programBackupFile));
			GF.AssertTest(StorageHelper.ExistFile(metadataFile));
			if (!StorageHelper.ExistFile(programBackupFile)) {
				onNotify($"Phase: backup original program.");
				StorageHelper.CopyFile(programFile, programBackupFile);
			}
			var dumpData = new List<String>();
			onNotify($"Phase: dump program information via Il2CppDumper.");
			{
				onNotify($"The Il2CppDumper path is '{programFileOfIl2CppDumper}'.");
				GF.AssertTest(StorageHelper.ExistFile(programFileOfIl2CppDumper));
				var dumpResult = await ProcessHelper.SpawnChild(programFileOfIl2CppDumper, [programBackupFile, metadataFile], true);
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
			onNotify($"Phase: parse symbol address from program information.");
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
			var programData = await StorageHelper.ReadFile(programBackupFile);
			var programPosition = 0;
			if (disableRecordEncryption) {
				onNotify($"Phase: modify method 'RecordStore.ReadRecord'.");
				programPosition = symbolAddress.RecordStore.ReadRecord[0];
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform.AsNotNull()));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform.AsNotNull()));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.CRC64.GetValue, false, platform.AsNotNull()));
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
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform.AsNotNull()));
				GF.AssertTest(GameUtility.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform.AsNotNull()));
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
						onNotify($"Warning : the STR instruction for 'MyConfig.DEBUG' was found at {(programPosition - 4):x}, but this modification may cause error.");
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
						BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition - 4, 4), (instructionCode & 0b111111111111_1111_0000_000000000000u) | (14u << 12) | ((USize)symbolAddress.MyConfig.DEBUG.First() << 0));
						onNotify($"Warning : the STR instruction for 'MyConfig.DEBUG'+4 was found at {(programPosition - 4):x}, but this modification may cause error.");
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
						BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition - 4, 4), (instructionCode & 0b1111111111_000000000000_11111_00000u) | ((USize)symbolAddress.MyConfig.DEBUG.First() << 10) | (30u << 0));
						onNotify($"Warning : the STR instruction for 'MyConfig.DEBUG'+4 was found at {(programPosition - 4):x}, but this modification may cause error.");
						break;
					}
					GF.AssertTest(programPosition != symbolAddress.MyConfig._cctor[0] + searchLimit);
				}
			}
			onNotify($"Phase: save modified program.");
			await StorageHelper.WriteFile(programFile, programData);
			return;
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
			try {
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
			}
			catch (Exception) {
				StorageHelper.RemoveDirectory(archiveDirectory);
				throw;
			}
			StorageHelper.RemoveDirectory(archiveDirectory);
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
			try {
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
			}
			catch (Exception) {
				StorageHelper.RemoveDirectory(archiveDirectory);
				throw;
			}
			StorageHelper.RemoveDirectory(archiveDirectory);
			return;
		}

		// ----------------

		public static Byte[] ConvertKeyFromUser (
			String user
		) {
			var keyValue = IntegerU64.Parse(user);
			var key = new Byte[8];
			BinaryPrimitives.TryWriteUInt64LittleEndian(key, keyValue);
			return key;
		}

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
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameUtility.BackupDirectory}_{configuration.Version}/{GameUtility.BackupProgramFile}")) {
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

	}

}
