#pragma warning disable 0,
// ReSharper disable

using KairosoftGameManager;

namespace KairosoftGameManager.Utility {

	public enum StoragePathType {
		Nothing,
		Relative,
		Absolute,
	}

	public enum StoragePathStyle {
		Generic,
		Native,
		Posix,
		Windows,
	}

	// ----------------

	public class StoragePath {

		#region constructor

		private StoragePathType mType;

		private String? mRoot;

		private List<String> mPart;

		// ----------------

		public StoragePath(
		) {
			this.mType = StoragePathType.Nothing;
			this.mRoot = null;
			this.mPart = [];
			return;
		}

		public StoragePath(
			StoragePath other
		) {
			this.mType = other.mType;
			this.mRoot = other.mRoot;
			this.mPart = other.mPart.ToList();
			return;
		}

		public StoragePath(
			StoragePathType type
		) {
			this.mType = type;
			this.mRoot = null;
			this.mPart = [];
			return;
		}

		public StoragePath(
			String text
		) {
			this.mType = StoragePathType.Nothing;
			this.mRoot = null;
			this.mPart = [];
			this.Parse(text);
			return;
		}

		#endregion

		#region equality

		public override Boolean Equals(
			Object? other
		) {
			return other is StoragePath that && this.mType == that.mType && this.mRoot == that.mRoot && this.mPart.SequenceEqual(that.mPart);
		}

		public override Size GetHashCode(
		) {
			return new List<Object?>([this.mType, this.mRoot, ..this.mPart]).GetMixedHashCode();
		}

		// ----------------

		public static Boolean operator ==(
			StoragePath? self,
			Object?      other
		) => Object.Equals(self, other);

		public static Boolean operator !=(
			StoragePath? self,
			Object?      other
		) => !Object.Equals(self, other);

		#endregion

		#region query

		public StoragePathType Type(
		) {
			return this.mType;
		}

		public String? Root(
		) {
			return this.mRoot;
		}

		public List<String> Part(
		) {
			return this.mPart;
		}

		#endregion

		#region segment

		public StoragePath? Parent(
		) {
			var result = null as StoragePath;
			if (this.mType != StoragePathType.Nothing && !this.mPart.IsEmpty()) {
				result = new ();
				result.mType = this.mType;
				result.mRoot = this.mRoot;
				result.mPart.AddRange(this.mPart.Slice(0, this.mPart.Count - 1));
			}
			return result;
		}

		public String? Name(
		) {
			var result = null as String;
			if (this.mType != StoragePathType.Nothing && !this.mPart.IsEmpty()) {
				result = this.mPart[^1];
			}
			return result;
		}

		public String? Stem(
		) {
			var result = null as String;
			if (this.mType != StoragePathType.Nothing && !this.mPart.IsEmpty()) {
				var position = this.mPart[^1].LastIndexOf('.');
				if (position != -1) {
					result = this.mPart[^1].Substring(0, position);
				}
				else {
					result = this.mPart[^1];
				}
			}
			return result;
		}

		public String? Extension(
		) {
			var result = null as String;
			if (this.mType != StoragePathType.Nothing && !this.mPart.IsEmpty()) {
				var position = this.mPart[^1].LastIndexOf('.');
				if (position != -1) {
					result = this.mPart[^1].Substring(position + 1);
				}
			}
			return result;
		}

		#endregion

		#region compose

		public StoragePath Join(
			String other
		) {
			AssertTest(this.mType != StoragePathType.Nothing);
			var result = new StoragePath();
			result.mType = this.mType;
			result.mRoot = this.mRoot;
			result.mPart.AddRange(this.mPart);
			result.mPart.Add(other);
			return result;
		}

		public StoragePath Push(
			StoragePath other
		) {
			AssertTest(this.mType != StoragePathType.Nothing);
			AssertTest(other.mType == StoragePathType.Relative);
			var result = new StoragePath();
			result.mType = this.mType;
			result.mRoot = this.mRoot;
			result.mPart.AddRange(this.mPart);
			result.mPart.AddRange(other.mPart);
			return result;
		}

		#endregion

		#region convert

		public void Parse(
			String text
		) {
			this.mType = StoragePathType.Nothing;
			this.mRoot = null;
			this.mPart = [];
			if (!text.IsEmpty()) {
				var position = 0;
				if (text.Length >= 2 && text[1] == ':' && ConvertHelper.IsLetter(text[0])) {
					this.mRoot = text.Substring(0, 2);
					position += 2;
				}
				if (text.Length > position && ConvertHelper.IsPathSeparator(text[position])) {
					this.mType = StoragePathType.Absolute;
					position += 1;
				}
				else {
					this.mType = StoragePathType.Relative;
				}
				var location = position;
				while (true) {
					if (position == text.Length || ConvertHelper.IsPathSeparator(text[position])) {
						var segment = text.Substring(location, position - location);
						if (segment == "" || segment == ".") {
						}
						else if (segment == ".." && !this.mPart.IsEmpty() && this.mPart.Last() != "..") {
							this.mPart.RemoveAt(this.mPart.Count - 1);
						}
						else {
							this.mPart.Add(segment);
						}
						if (position == text.Length) {
							break;
						}
						position += 1;
						location = position;
						continue;
					}
					position += 1;
				}
			}
			return;
		}

		public String Emit(
			StoragePathStyle style = StoragePathStyle.Generic
		) {
			var text = "";
			if (this.mType != StoragePathType.Nothing) {
				var mappedStyle = style;
				if (style == StoragePathStyle.Generic) {
					mappedStyle = StoragePathStyle.Posix;
				}
				if (style == StoragePathStyle.Native) {
					mappedStyle = StoragePathStyle.Windows;
				}
				var separator = mappedStyle == StoragePathStyle.Posix ? '/' : '\\';
				if (this.mRoot != null) {
					text += this.mRoot!;
				}
				if (this.mType == StoragePathType.Relative) {
					text += '.';
				}
				if (this.mPart.IsEmpty()) {
					text += separator;
				}
				foreach (var segment in this.mPart) {
					text += separator;
					if (mappedStyle == StoragePathStyle.Posix) {
						text += segment;
					}
					if (mappedStyle == StoragePathStyle.Windows) {
						var segmentSize = segment.Length;
						while (segmentSize != 0) {
							if (segment[segmentSize - 1] == ' ') {
								segmentSize -= 1;
								continue;
							}
							if (segment[segmentSize - 1] == '.' && !((segmentSize == 1) || (segmentSize == 2 && segment[0] == '.'))) {
								segmentSize -= 1;
								continue;
							}
							break;
						}
						text += segment.Substring(0, segmentSize);
					}
				}
			}
			return text;
		}

		// ----------------

		public String EmitGeneric(
		) {
			return this.Emit(StoragePathStyle.Generic);
		}

		public String EmitNative(
		) {
			return this.Emit(StoragePathStyle.Native);
		}

		public String EmitPosix(
		) {
			return this.Emit(StoragePathStyle.Posix);
		}

		public String EmitWindows(
		) {
			return this.Emit(StoragePathStyle.Windows);
		}

		#endregion

	}

}
