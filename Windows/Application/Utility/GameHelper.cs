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
		WindowsIntel32,
		AndroidArm32,
		AndroidArm64,
	}

	public enum GameProgramState {
		None,
		Original,
		Modified,
	}

	public enum GameRecordState {
		None,
		Invalid,
		Original,
		Decrypted,
	}

	public enum GamePackageType {
		Flat,
		Zip,
		Apk,
		Apks,
	}

	// ----------------

	public record GameConfiguration {
		public String           Path       = "";
		public String?          Library    = null;
		public String?          Identifier = null;
		public String?          Version    = null;
		public String           Name       = "";
		public ImageSource?     Icon       = null;
		public String           User       = "";
		public GameProgramState Program    = GameProgramState.None;
		public GameRecordState  Record     = GameRecordState.None;
	}

	public record GameRecordArchiveConfiguration {
		public String Platform   = "";
		public String Identifier = "";
		public String Version    = "";
	}

	// ----------------

	public enum GameFunctionType {
		ModifyProgram,
		EncryptRecord,
		ExportRecord,
		ImportRecord,
	}

	#endregion

	public static class GameHelper {

		#region common

		public const String ExecutableFile = "KairoGames.exe";

		public const String ProgramFile = "GameAssembly.dll";

		public const String RecordBundleDirectory = "saves";

		public const String RecordArchiveFileExtension = "kgra";

		// ----------------

		public static String GetPlatformSystemName (
			GamePlatform value
		) {
			return value switch {
				GamePlatform.WindowsIntel32 => "windows",
				GamePlatform.AndroidArm32   => "android",
				GamePlatform.AndroidArm64   => "android",
				_                           => throw new UnreachableException(),
			};
		}

		#endregion

		#region program

		private static String GetProgramFilePath (
			GamePlatform platform
		) {
			return platform switch {
				GamePlatform.WindowsIntel32 => "GameAssembly.dll",
				GamePlatform.AndroidArm32   => "lib/armeabi-v7a/libil2cpp.so",
				GamePlatform.AndroidArm64   => "lib/arm64-v8a/libil2cpp.so",
				_                           => throw new UnreachableException(),
			};
		}

		private static String GetMetadataFilePath (
			GamePlatform platform
		) {
			return platform switch {
				GamePlatform.WindowsIntel32 => "KairoGames_Data/il2cpp_data/Metadata/global-metadata.dat",
				GamePlatform.AndroidArm32   => "assets/bin/Data/Managed/Metadata/global-metadata.dat",
				GamePlatform.AndroidArm64   => "assets/bin/Data/Managed/Metadata/global-metadata.dat",
				_                           => throw new UnreachableException(),
			};
		}

		private static List<GamePlatform> DetectPlatform (
			String gameDirectory
		) {
			var result = new List<GamePlatform>();
			foreach (var platform in Enum.GetValues<GamePlatform>()) {
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.GetProgramFilePath(platform)}")) {
					continue;
				}
				if (!StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.GetMetadataFilePath(platform)}")) {
					continue;
				}
				result.Add(platform);
			}
			return result;
		}

		// ----------------

		private const IntegerU8 InstructionCodeNopIntel32 = 0x90;

		private const IntegerU32 InstructionCodeNopArm32 = 0xE320F000;

		private const IntegerU32 InstructionCodeNopArm64 = 0xD503201F;

		private static Boolean FindCallInstruction (
			Span<Byte>   data,
			ref Size     position,
			Size         limit,
			List<Size>   address,
			Boolean      overwrite,
			GamePlatform platform
		) {
			var state = false;
			var dataEnd = Math.Min(data.Length, position + limit);
			if (platform == GamePlatform.WindowsIntel32) {
				while (position < dataEnd) {
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
						data[position++] = GameHelper.InstructionCodeNopIntel32;
						data[position++] = GameHelper.InstructionCodeNopIntel32;
						data[position++] = GameHelper.InstructionCodeNopIntel32;
						data[position++] = GameHelper.InstructionCodeNopIntel32;
						data[position++] = GameHelper.InstructionCodeNopIntel32;
					}
					state = true;
					break;
				}
			}
			if (platform == GamePlatform.AndroidArm32) {
				while (position < dataEnd) {
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
						BinaryPrimitives.TryWriteUInt32LittleEndian(data.Slice(position - 4, 4), GameHelper.InstructionCodeNopArm32);
					}
					state = true;
					break;
				}
			}
			if (platform == GamePlatform.AndroidArm64) {
				while (position < dataEnd) {
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
						BinaryPrimitives.TryWriteUInt32LittleEndian(data.Slice(position - 4, 4), GameHelper.InstructionCodeNopArm64);
					}
					state = true;
					break;
				}
			}
			return state;
		}

		// ----------------

		private static async Task ModifyProgramFlat (
			GamePlatform        platform,
			String              programFile,
			String              metadataFile,
			Boolean             disableRecordEncryption,
			Boolean             enableDebugMode,
			ExternalToolSetting externalToolSetting,
			Action<String>      onNotify
		) {
			onNotify($"Phase: dump program information.");
			var dumpData = await ExternalToolHelper.RunIl2cppdumper(externalToolSetting, programFile, metadataFile);
			onNotify($"Phase: parse symbol address.");
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
			{
				symbolAddress.CRC64.GetValue.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "CRC64", "GetValue")
					.SelfAlso((it) => AssertTest(it.Count() == 1))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'CRC64.GetValue' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.Encrypter.Encode.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "Encrypter", "Encode")
					.SelfAlso((it) => AssertTest(it.Count() == 3))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'Encrypter.Encode' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.Encrypter.Decode.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "Encrypter", "Decode")
					.SelfAlso((it) => AssertTest(it.Count() == 3))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'Encrypter.Decode' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.RecordStore.ReadRecord.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "RecordStore", "ReadRecord")
					.Where((value) => (!value.Item3 && value.Item5 == "int rcId"))
					.SelfAlso((it) => AssertTest(it.Count() == 1))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'RecordStore.ReadRecord' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.RecordStore.WriteRecord.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "RecordStore", "WriteRecord")
					.Where((value) => (!value.Item3 && value.Item5 == "int rcId, byte[][] data"))
					.SelfAlso((it) => AssertTest(it.Count() == 1))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'RecordStore.WriteRecord' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.MyConfig._cctor.AddRange(ExternalToolHelper.DoIl2cppdumperSearchMethodFromDumpData(dumpData, "MyConfig", ".cctor")
					.SelfAlso((it) => AssertTest(it.Count() == 1))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'MyConfig..cctor' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
				symbolAddress.MyConfig.DEBUG.AddRange(ExternalToolHelper.DoIl2cppdumperSearchFieldFromDumpData(dumpData, "MyConfig", "DEBUG")
					.SelfAlso((it) => AssertTest(it.Count() == 1))
					.Select((it) => it.Item1)
					.SelfAlso((it) => onNotify($"Tip: the symbol 'MyConfig.DEBUG' at {String.Join(',', it.Select((value) => $"{value:x8}"))}.")));
			}
			onNotify($"Phase: load original program.");
			var programData = await StorageHelper.ReadFile(programFile);
			var programPosition = 0;
			if (disableRecordEncryption) {
				onNotify($"Phase: modify method 'RecordStore.ReadRecord'.");
				programPosition = symbolAddress.RecordStore.ReadRecord.First();
				AssertTest(GameHelper.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform));
				AssertTest(GameHelper.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Decode, true, platform));
				AssertTest(GameHelper.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.CRC64.GetValue, false, platform));
				if (platform == GamePlatform.WindowsIntel32) {
					// add esp, .. = 83 C4 XX
					AssertTest(programData[programPosition] == 0x83);
					programPosition++;
					AssertTest(programData[programPosition] == 0xC4);
					programPosition++;
					programPosition++;
					// cmp eax, .. = 3B XX XX
					AssertTest(programData[programPosition] == 0x3B);
					programData[programPosition++] = GameHelper.InstructionCodeNopIntel32;
					programData[programPosition++] = GameHelper.InstructionCodeNopIntel32;
					programData[programPosition++] = GameHelper.InstructionCodeNopIntel32;
					// jnz .. = 75 XX
					AssertTest(programData[programPosition] == 0x75);
					programData[programPosition++] = GameHelper.InstructionCodeNopIntel32;
					programData[programPosition++] = GameHelper.InstructionCodeNopIntel32;
				}
				if (platform == GamePlatform.AndroidArm32) {
					programPosition += 12;
					// bne #X = 1A XX XX XX
					AssertTest((BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4)) & 0xFF000000) == 0x1A000000);
					BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4), GameHelper.InstructionCodeNopArm32);
					programPosition += 4;
				}
				if (platform == GamePlatform.AndroidArm64) {
					programPosition += 8;
					// bne #X = 54 XX XX XX
					AssertTest((BinaryPrimitives.ReadUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4)) & 0xFF000000) == 0x54000000);
					BinaryPrimitives.WriteUInt32LittleEndian(programData.AsSpan().Slice(programPosition, 4), GameHelper.InstructionCodeNopArm64);
					programPosition += 4;
				}
			}
			if (disableRecordEncryption) {
				onNotify($"Phase: modify method 'RecordStore.WriteRecord'.");
				programPosition = symbolAddress.RecordStore.WriteRecord.First();
				AssertTest(GameHelper.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform));
				AssertTest(GameHelper.FindCallInstruction(programData, ref programPosition, 0x1000, symbolAddress.Encrypter.Encode, true, platform));
			}
			if (enableDebugMode) {
				onNotify($"Phase: modify method 'MyConfig..cctor'.");
				programPosition = symbolAddress.MyConfig._cctor.First();
				var programEnd = programPosition + 0x200;
				if (platform == GamePlatform.WindowsIntel32) {
					while (programPosition < programEnd) {
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
						if (programData[programPosition] != symbolAddress.MyConfig.DEBUG.First()) {
							continue;
						}
						programPosition++;
						if (programData[programPosition] != 0x00) {
							continue;
						}
						programData[programPosition++] = 0x01;
						break;
					}
				}
				if (platform == GamePlatform.AndroidArm32) {
					while (programPosition < programEnd) {
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
						break;
					}
				}
				if (platform == GamePlatform.AndroidArm64) {
					while (programPosition < programEnd) {
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
						break;
					}
				}
				AssertTest(programPosition != programEnd);
				onNotify($"Warning: the STR instruction for 'MyConfig.DEBUG'+4 was found at {(programPosition - 4):x8}, but this modification may cause error.");
			}
			onNotify($"Phase: save modified program.");
			await StorageHelper.WriteFile(programFile, programData);
			return;
		}

		public static async Task ModifyProgram (
			String              target,
			Boolean             disableRecordEncryption,
			Boolean             enableDebugMode,
			ExternalToolSetting externalToolSetting,
			Action<String>      onNotify
		) {
			var temporaryDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(temporaryDirectory);
			await using var temporaryDirectoryFinalizer = new Finalizer(async () => {
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
			AssertTest(packageType != null);
			onNotify($"Tip: the package type is '{packageType}'.");
			var packageBundle = null as ZipArchive;
			var packagePartList = null as List<Tuple<String, ZipArchive>>;
			await using var packageFinalizer = new Finalizer(async () => {
				if (packageBundle != null) {
					await packageBundle.DisposeAsync();
				}
				if (packagePartList != null) {
					foreach (var packagePart in packagePartList) {
						await packagePart.Item2.DisposeAsync();
					}
				}
			});
			if (packageType != GamePackageType.Flat) {
				onNotify($"Phase: load package file'.");
				var packageMainPath = $"{temporaryDirectory}/package/main";
				StorageHelper.Copy(target, packageMainPath);
				var packageMain = await ZipFile.OpenAsync(packageMainPath, ZipArchiveMode.Update);
				packagePartList = [];
				if (packageType == GamePackageType.Zip) {
					packagePartList.Add(new ("main", packageMain));
				}
				if (packageType == GamePackageType.Apk) {
					packagePartList.Add(new ("main", packageMain));
				}
				if (packageType == GamePackageType.Apks) {
					packageBundle = packageMain;
					StorageHelper.CreateDirectory(StorageHelper.Parent($"{temporaryDirectory}/package").AsNotNull());
					foreach (var packagePartFile in packageBundle.Entries) {
						if (!packagePartFile.Name.ToLower().EndsWith(".apk")) {
							continue;
						}
						var packagePartPath = $"{temporaryDirectory}/package/{packagePartFile.Name}";
						await packagePartFile.ExtractToFileAsync(packagePartPath);
						packagePartList.Add(new (packagePartFile.Name, await ZipFile.OpenAsync(packagePartPath, ZipArchiveMode.Update)));
					}
				}
			}
			onNotify($"Phase: extract necessary file.");
			var targetDirectory = $"{temporaryDirectory}/flat";
			var necessaryFileNameList = Enum.GetValues<GamePlatform>().SelectMany((it) => new[] { GameHelper.GetProgramFilePath(it), GameHelper.GetMetadataFilePath(it) }).Distinct();
			if (packageType == GamePackageType.Flat) {
				foreach (var necessaryFileName in necessaryFileNameList) {
					if (StorageHelper.ExistFile($"{target}/{necessaryFileName}")) {
						StorageHelper.Copy($"{target}/{necessaryFileName}", $"{targetDirectory}/{necessaryFileName}");
					}
				}
			}
			else {
				AssertTest(packagePartList != null);
				foreach (var packagePart in packagePartList) {
					foreach (var necessaryFileName in necessaryFileNameList) {
						var necessaryFile = packagePart.Item2.GetEntry(necessaryFileName);
						if (necessaryFile == null) {
							continue;
						}
						StorageHelper.CreateDirectory(StorageHelper.Parent($"{targetDirectory}/{necessaryFileName}").AsNotNull());
						await necessaryFile.ExtractToFileAsync($"{targetDirectory}/{necessaryFileName}");
					}
				}
			}
			onNotify($"Phase: detect platform.");
			var platformList = GameHelper.DetectPlatform(targetDirectory);
			AssertTest(platformList.Count != 0);
			onNotify($"Tip: the platform is '{String.Join('|', platformList)}'.");
			foreach (var platform in platformList) {
				onNotify($"Phase: modify program of '{platform}'.");
				await GameHelper.ModifyProgramFlat(
					platform,
					$"{targetDirectory}/{GameHelper.GetProgramFilePath(platform)}",
					$"{targetDirectory}/{GameHelper.GetMetadataFilePath(platform)}",
					disableRecordEncryption,
					enableDebugMode,
					externalToolSetting,
					onNotify
				);
			}
			if (packageType != GamePackageType.Flat) {
				onNotify($"Phase: repack package file.");
				AssertTest(packagePartList != null);
				var replaceTaskList = new List<Tuple<ZipArchive, GamePlatform>>();
				var replaceProgramFile = async (
					ZipArchive   package,
					GamePlatform platform
				) => {
					var fileName = GameHelper.GetProgramFilePath(platform);
					package.GetEntry(fileName).AsNotNull().Delete();
					await package.CreateEntryFromFileAsync($"{targetDirectory}/{fileName}", fileName);
					return;
				};
				foreach (var platform in platformList) {
					if (packageType == GamePackageType.Zip || packageType == GamePackageType.Apk) {
						replaceTaskList.Add(new (packagePartList.First().Item2, platform));
					}
					if (packageType == GamePackageType.Apks) {
						var architectureName = platform switch {
							GamePlatform.AndroidArm32 => "armeabi_v7a",
							GamePlatform.AndroidArm64 => "arm64_v8a",
							_                         => throw new UnreachableException(),
						};
						var packagePartName = $"split_config.{architectureName}.apk";
						replaceTaskList.Add(new (packagePartList.First((it) => it.Item1 == packagePartName).Item2, platform));
					}
				}
				foreach (var replaceTask in replaceTaskList) {
					var packagePart = replaceTask.Item1;
					var platform = replaceTask.Item2;
					var fileName = GameHelper.GetProgramFilePath(platform);
					packagePart.GetEntry(fileName).AsNotNull().Delete();
					await packagePart.CreateEntryFromFileAsync($"{targetDirectory}/{fileName}", fileName);
				}
				foreach (var packagePart in packagePartList) {
					await packagePart.Item2.DisposeAsync();
				}
			}
			if (packageType == GamePackageType.Apk || packageType == GamePackageType.Apks) {
				onNotify($"Phase: post-processing apk file.");
				AssertTest(packagePartList != null);
				var enableAlign = true;
				var enableSign = true;
				if (!StorageHelper.ExistFile(externalToolSetting.ZipalignPath)) {
					onNotify($"Tip: skipping apk align, the external tool 'zipalign' not found.");
					enableAlign = false;
				}
				if (!StorageHelper.ExistFile(externalToolSetting.ApksignerPath)) {
					onNotify($"Tip: skipping apk sign, the external tool 'apksigner' not found.");
					enableSign = false;
				}
				if (!StorageHelper.ExistFile(externalToolSetting.ApkCertificateFile)) {
					onNotify($"Tip: skipping apk sign, the custom apk certificate file not found.");
					enableSign = false;
				}
				if (externalToolSetting.ApkCertificatePassword == "") {
					onNotify($"Tip: skipping apk sign, the custom apk certificate password not set.");
					enableSign = false;
				}
				foreach (var packagePart in packagePartList) {
					var packagePartFile = $"{temporaryDirectory}/package/{packagePart.Item1}";
					if (enableAlign) {
						await ExternalToolHelper.RunZipalign(externalToolSetting, packagePartFile);
					}
					if (enableSign) {
						await ExternalToolHelper.RunApksigner(externalToolSetting, packagePartFile);
					}
				}
			}
			onNotify($"Phase: generate result.");
			if (packageType == GamePackageType.Flat) {
				foreach (var platform in platformList) {
					StorageHelper.Trash($"{target}/{GameHelper.GetProgramFilePath(platform)}");
					StorageHelper.Copy($"{targetDirectory}/{GameHelper.GetProgramFilePath(platform)}", $"{target}/{GameHelper.GetProgramFilePath(platform)}");
				}
			}
			if (packageType == GamePackageType.Zip || packageType == GamePackageType.Apk) {
				AssertTest(packagePartList != null);
				StorageHelper.Trash(target);
				StorageHelper.Copy($"{temporaryDirectory}/package/{packagePartList.First().Item1}", target);
			}
			if (packageType == GamePackageType.Apks) {
				AssertTest(packageBundle != null);
				AssertTest(packagePartList != null);
				foreach (var packagePart in packagePartList) {
					packageBundle.GetEntry(packagePart.Item1).AsNotNull().Delete();
					await packageBundle.CreateEntryFromFileAsync($"{temporaryDirectory}/package/{packagePart.Item1}", packagePart.Item1);
				}
				await packageBundle.DisposeAsync();
				StorageHelper.Trash(target);
				StorageHelper.Copy($"{temporaryDirectory}/package/main", target);
			}
			onNotify($"Phase: done.");
			return;
		}

		#endregion

		#region record

		private static List<String> ListRecordFile (
			String recordDirectory
		) {
			return StorageHelper.ListDirectory(recordDirectory, 1, true, false)
				.Where((value) => new Regex(@"^\d{4,4}(_backup)?$").IsMatch(value))
				.ToList();
		}

		private static async Task<GameRecordState> DetectRecordState (
			String      recordDirectory,
			List<Byte>? key
		) {
			var state = GameRecordState.Invalid;
			var itemNameList = GameHelper.ListRecordFile(recordDirectory);
			if (itemNameList.Count == 0) {
				state = GameRecordState.None;
			}
			else if (itemNameList.Contains("0000")) {
				var itemFile = $"{recordDirectory}/0000";
				var itemData = await StorageHelper.ReadFileLimited(itemFile, 8);
				if (itemData.Length == 8) {
					var firstNumber = BinaryPrimitives.ReadUInt32LittleEndian(itemData);
					if (firstNumber == 0x00000000) {
						state = GameRecordState.Decrypted;
					}
					else if (key != null) {
						GameHelper.EncryptRecordData(itemData, key);
						firstNumber = BinaryPrimitives.ReadUInt32LittleEndian(itemData);
						if (firstNumber == 0x00000000) {
							state = GameRecordState.Original;
						}
					}
				}
			}
			return state;
		}

		// ----------------

		private static void EncryptRecordData (
			Byte[]      data,
			List<Byte>? key
		) {
			if (key != null) {
				AssertTest(key.Count != 0);
				for (var index = 0; index < data.Length; index++) {
					data[index] ^= key[index % key.Count];
				}
			}
			return;
		}

		private static async Task EncryptRecordFile (
			String      sourceFile,
			String      destinationFile,
			List<Byte>? key
		) {
			var data = await StorageHelper.ReadFile(sourceFile);
			GameHelper.EncryptRecordData(data, key);
			await StorageHelper.WriteFile(destinationFile, data);
			return;
		}

		public static async Task EncryptRecord (
			String         targetDirectory,
			List<Byte>     key,
			Action<String> onNotify
		) {
			var itemNameList = GameHelper.ListRecordFile(targetDirectory);
			if (itemNameList.Count == 0) {
				onNotify($"The target directory does not contain a record file.");
			}
			foreach (var itemName in itemNameList) {
				onNotify($"Phase: {itemName}.");
				await GameHelper.EncryptRecordFile($"{targetDirectory}/{itemName}", $"{targetDirectory}/{itemName}", key);
			}
			return;
		}

		// ----------------

		public static String MakeRecordArchiveConfigurationText (
			GameRecordArchiveConfiguration value
		) {
			return String.Join(':', [value.Platform, value.Identifier, value.Version]);
		}

		public static GameRecordArchiveConfiguration ParseRecordArchiveConfigurationText (
			String text
		) {
			var list = text.Split(':');
			return new () {
				Platform = list[0],
				Identifier = list[1],
				Version = list[2],
			};
		}

		// ----------------

		public static async Task ExportRecordArchive (
			String                                              targetDirectory,
			String                                              archiveFile,
			List<Byte>?                                         key,
			Func<GameRecordArchiveConfiguration, Task<Boolean>> doArchiveConfiguration
		) {
			var archiveDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(archiveDirectory);
			await using var archiveDirectoryFinalizer = new Finalizer(async () => {
				StorageHelper.Remove(archiveDirectory);
			});
			// configuration
			var archiveConfiguration = new GameRecordArchiveConfiguration();
			if (!await doArchiveConfiguration(archiveConfiguration)) {
				return;
			}
			await StorageHelper.WriteFileText($"{archiveDirectory}/configuration.txt", GameHelper.MakeRecordArchiveConfigurationText(archiveConfiguration));
			// data
			StorageHelper.CreateDirectory($"{archiveDirectory}/data");
			foreach (var dataFile in GameHelper.ListRecordFile(targetDirectory)) {
				await GameHelper.EncryptRecordFile($"{targetDirectory}/{dataFile}", $"{archiveDirectory}/data/{dataFile}", key);
			}
			// save
			if (StorageHelper.ExistFile(archiveFile)) {
				StorageHelper.Trash(archiveFile);
			}
			await ZipFile.CreateFromDirectoryAsync(archiveDirectory, archiveFile, CompressionLevel.SmallestSize, false, Encoding.UTF8);
			return;
		}

		public static async Task ImportRecordArchive (
			String                                              targetDirectory,
			String                                              archiveFile,
			List<Byte>?                                         key,
			Func<GameRecordArchiveConfiguration, Task<Boolean>> doArchiveConfiguration
		) {
			var archiveDirectory = StorageHelper.Temporary();
			StorageHelper.CreateDirectory(archiveDirectory);
			await using var archiveDirectoryFinalizer = new Finalizer(async () => {
				StorageHelper.Remove(archiveDirectory);
			});
			// load
			await ZipFile.ExtractToDirectoryAsync(archiveFile, archiveDirectory, Encoding.UTF8);
			// configuration
			var archiveConfiguration = GameHelper.ParseRecordArchiveConfigurationText(await StorageHelper.ReadFileText($"{archiveDirectory}/configuration.txt"));
			if (!await doArchiveConfiguration(archiveConfiguration)) {
				return;
			}
			// data
			if (StorageHelper.ExistDirectory(targetDirectory)) {
				StorageHelper.Trash(targetDirectory);
			}
			StorageHelper.CreateDirectory(targetDirectory);
			foreach (var dataFile in GameHelper.ListRecordFile($"{archiveDirectory}/data")) {
				await GameHelper.EncryptRecordFile($"{archiveDirectory}/data/{dataFile}", $"{targetDirectory}/{dataFile}", key);
			}
			return;
		}

		#endregion

		#region repository

		public static async Task<Tuple<GameProgramState, GameRecordState>> CheckGameState (
			String  gameDirectory,
			String? version,
			String  user
		) {
			var programState = GameProgramState.None;
			var recordState = GameRecordState.None;
			if (StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.ProgramFile}")) {
				programState = !StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.ProgramFile}.{version ?? "0"}.bak")
					? GameProgramState.Original
					: GameProgramState.Modified;
			}
			if (StorageHelper.ExistDirectory($"{gameDirectory}/{GameHelper.RecordBundleDirectory}/{user}")) {
				recordState = await GameHelper.DetectRecordState($"{gameDirectory}/{GameHelper.RecordBundleDirectory}/{user}", GameHelper.MakeKeyFromSteamUser(user));
			}
			return new (programState, recordState);
		}

		// ----------------

		public static async Task<GameConfiguration?> LoadCustomGame (
			String gameDirectory
		) {
			if (!StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.ExecutableFile}")) {
				return null;
			}
			var configuration = new GameConfiguration();
			configuration.Path = gameDirectory;
			configuration.Library = null;
			configuration.Identifier = null;
			configuration.Version = null;
			configuration.Name = StorageHelper.Name(gameDirectory);
			configuration.Icon = await ConvertHelper.ParseBitmapFromGdiBitmap(System.Drawing.Icon.ExtractIcon($"{gameDirectory}/{GameHelper.ExecutableFile}", 0, 48).AsNotNull().ToBitmap());
			configuration.User = "0";
			if (StorageHelper.ExistDirectory($"{gameDirectory}/{GameHelper.RecordBundleDirectory}")) {
				configuration.User = StorageHelper.ListDirectory($"{gameDirectory}/{GameHelper.RecordBundleDirectory}", 1, false, true).FirstOrDefault((it) => new Regex(@"^\d+$").IsMatch(it)) ?? "0";
			}
			var gameState = await GameHelper.CheckGameState(gameDirectory, configuration.Version, configuration.User);
			configuration.Program = gameState.Item1;
			configuration.Record = gameState.Item2;
			return configuration;
		}

		public static async Task<Boolean> CheckCustomRepository (
			String repositoryDirectory
		) {
			return StorageHelper.ExistDirectory($"{repositoryDirectory}")
				&& !StorageHelper.ExistFile($"{repositoryDirectory}/steam.exe");
		}

		public static async Task<List<GameConfiguration>> LoadCustomRepository (
			String repositoryDirectory
		) {
			var libraryList = StorageHelper.ListDirectory(repositoryDirectory, 1, false, true);
			var result = new List<GameConfiguration>();
			foreach (var library in libraryList) {
				var libraryDirectory = $"{repositoryDirectory}/{library}";
				var gameConfiguration = await GameHelper.LoadCustomGame(libraryDirectory);
				if (gameConfiguration != null) {
					result.Add(gameConfiguration);
				}
			}
			return result;
		}

		// ----------------

		public static List<Byte> MakeKeyFromSteamUser (
			String user
		) {
			var keyValue = IntegerU64.Parse(user);
			var key = new Byte[8];
			BinaryPrimitives.TryWriteUInt64LittleEndian(key, keyValue);
			return key.ToList();
		}

		public static async Task<GameConfiguration?> LoadSteamGame (
			String libraryDirectory,
			String gameIdentifier
		) {
			var gameManifestFile = $"{libraryDirectory}/steamapps/appmanifest_{gameIdentifier}.acf";
			if (!StorageHelper.ExistFile(gameManifestFile)) {
				return null;
			}
			var gameManifest = VdfConvert.Deserialize(await StorageHelper.ReadFileText(gameManifestFile));
			AssertTest(gameManifest.Key == "AppState");
			AssertTest(gameManifest.Value["appid"].AsNotNull().Value<String>() == gameIdentifier);
			var gameDirectory = $"{libraryDirectory}/steamapps/common/{gameManifest.Value["installdir"].AsNotNull().Value<String>()}";
			if (!StorageHelper.ExistFile($"{gameDirectory}/{GameHelper.ExecutableFile}")) {
				return null;
			}
			var configuration = new GameConfiguration();
			configuration.Path = gameDirectory;
			configuration.Library = libraryDirectory;
			configuration.Identifier = gameIdentifier;
			configuration.Version = gameManifest.Value["buildid"].AsNotNull().Value<String>();
			configuration.Name = gameManifest.Value["name"].AsNotNull().Value<String>();
			configuration.Icon = await ConvertHelper.ParseBitmapFromGdiBitmap(System.Drawing.Icon.ExtractIcon($"{gameDirectory}/{GameHelper.ExecutableFile}", 0, 48).AsNotNull().ToBitmap());
			configuration.User = gameManifest.Value["LastOwner"].AsNotNull().Value<String>();
			var gameState = await GameHelper.CheckGameState(gameDirectory, configuration.Version, configuration.User);
			configuration.Program = gameState.Item1;
			configuration.Record = gameState.Item2;
			return configuration;
		}

		public static async Task<Boolean> CheckSteamRepository (
			String repositoryDirectory
		) {
			return StorageHelper.ExistDirectory($"{repositoryDirectory}")
				&& StorageHelper.ExistFile($"{repositoryDirectory}/steam.exe");
		}

		public static async Task<List<GameConfiguration>> LoadSteamRepository (
			String repositoryDirectory
		) {
			var libraryList = VdfConvert.Deserialize(await StorageHelper.ReadFileText($"{repositoryDirectory}/steamapps/libraryfolders.vdf"));
			AssertTest(libraryList.Key == "libraryfolders");
			var result = new List<GameConfiguration>();
			foreach (var library in libraryList.Value.Children<VProperty>()) {
				var libraryDirectory = StorageHelper.Regularize(library.Value["path"].AsNotNull().Value<String>());
				foreach (var game in library.Value["apps"].AsNotNull().Children<VProperty>()) {
					var gameIdentifier = game.Key;
					var gameConfiguration = await GameHelper.LoadSteamGame(libraryDirectory, gameIdentifier);
					if (gameConfiguration != null) {
						result.Add(gameConfiguration);
					}
				}
			}
			return result;
		}

		#endregion

	}

}
