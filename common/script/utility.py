import os
import sys
import re
import pathlib
import glob
import shutil
import tempfile
import subprocess

# ----------------

def fs_resolve(
	pattern: str,
) -> list[str]:
	return glob.glob(pattern)

def fs_copy(
	source: str,
	destination: str,
) -> None:
	if not pathlib.Path(source).exists():
		raise RuntimeError(f'invalid source \'{source}\'')
	if not pathlib.Path(destination).parent.exists():
		fs_create_directory(f'{pathlib.Path(destination).parent}')
	if pathlib.Path(source).is_file():
		shutil.copy(source, destination, follow_symlinks=True)
	if pathlib.Path(source).is_dir():
		shutil.copytree(source, destination, symlinks=True)
	return

def fs_remove(
	source: str,
) -> None:
	if pathlib.Path(source).is_symlink():
		os.remove(source)
	if pathlib.Path(source).is_file():
		os.remove(source)
	if pathlib.Path(source).is_dir():
		shutil.rmtree(source)
	return

def fs_create_link(
	target: str,
	object: str,
	is_directory: bool,
) -> None:
	if pathlib.Path(target).exists():
		fs_remove(target)
	os.symlink(object, target, is_directory)
	return

def fs_read_file(
	target: str,
) -> str:
	return pathlib.Path(target).read_text('utf-8')

def fs_write_file(
	target: str,
	data: str,
) -> None:
	pathlib.Path(target).write_text(data, encoding='utf-8')
	return

def fs_create_directory(
	target: str,
) -> None:
	os.makedirs(target, exist_ok=True)
	return

# ----------------

def execute_command(
	location: str,
	command: list[str],
	environment: dict[str, str] = {},
	ensure_ok: bool = True,
	want_output: bool = False,
) -> tuple[int, str, str]:
	actual_environment = os.environ.copy()
	for environment_name, environment_value in environment.items():
		actual_environment[environment_name] = environment_value
	result = subprocess.run(
		command,
		env=actual_environment,
		cwd=location,
		shell=sys.platform == 'win32',
		check=ensure_ok,
		capture_output=want_output,
		text=True,
		encoding='utf-8',
	)
	return (result.returncode, result.stdout, result.stderr)

# ----------------

def get_project(
) -> str:
	return f'{pathlib.Path(__file__).absolute().parent.parent.parent.as_posix()}'

def get_project_module(
	path: str,
) -> tuple[str, str]:
	name = pathlib.Path(path).parent.name
	name_snake = '_'.join([item.lower() for item in re.split(r'(?=[A-Z])', name)[1:]])
	return (f'{get_project()}/{name}', name_snake)

def get_project_local(
	name: str | None = None,
) -> str:
	return f'{get_project()}/.local{'' if name is None else f'/{name}'}'

def get_project_distribution(
	name: str | None = None,
) -> str:
	return f'{get_project_local('distribution')}{'' if name is None else f'/{name}'}'

def get_project_keystore(
) -> tuple[str | None, str]:
	file = f'{get_project_local('keystore')}/file.p12'
	if not pathlib.Path(file).is_file():
		return (None, '')
	password = fs_read_file(f'{get_project_local('keystore')}/password.txt')
	return (file, password)

# ----------------

def check_platform(
	value: str,
	expect: list[str],
) -> bool:
	return value in expect

def ensure_platform(
	value: str,
	expect: list[str],
) -> None:
	if not check_platform(value, expect):
		raise RuntimeError(f'unsupported platform \'{value}\'')
	return

# ----------------

def pack_zip(
	name: str,
	source: str,
	destination: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/{name}',
		)
		shutil.make_archive(
			f'{temporary}/{name}',
			'zip',
			f'{temporary}',
			f'{name}',
		)
		fs_copy(
			f'{temporary}/{name}.zip',
			f'{destination}',
		)
	return

def unpack_zip(
	source: str,
	destination: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		shutil.unpack_archive(
			f'{source}',
			f'{destination}',
			'zip',
		)
	return

# ----------------

def generate_keystore(
	target: str,
	password: str,
	subject: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		execute_command(temporary, [
			'openssl',
			'genrsa',
			'-out', f'{temporary}/file.key',
			f'2048',
		])
		execute_command(temporary, [
			'openssl',
			'req',
			'-new',
			'-x509',
			'-key', f'{temporary}/file.key',
			'-out', f'{temporary}/file.crt',
			'-utf8',
			'-days', f'365',
			'-subj', f'{subject}',
			'-addext', f'basicConstraints=CA:FALSE',
		])
		execute_command(temporary, [
			'openssl',
			'pkcs12',
			'-export',
			'-out', f'{temporary}/file.p12',
			'-passout', f'pass:{password}',
			'-inkey', f'{temporary}/file.key',
			'-in', f'{temporary}/file.crt',
		])
		fs_copy(
			f'{temporary}/file.p12',
			f'{target}',
		)
	return

# ----------------

def strip_windows_binary(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		execute_command(temporary, [
			'llvm-strip',
			'--strip-all',
			f'{target}',
		])
	return

def apply_windows_manifest(
	target: str,
	manifest: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		execute_command(temporary, [
			'mt',
			'-manifest', f'{manifest}',
			f'-outputresource:{target};#1',
		])
	return

def sign_windows_executable(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		keystore_file, keystore_password = get_project_keystore()
		if keystore_file == None:
			return
		execute_command(temporary, [
			'signtool',
			'sign',
			'/q',
			'/fd', f'SHA256',
			'/f', f'{keystore_file}',
			'/p', f'{keystore_password}',
			f'{target}',
		])
	return

def pack_windows_msix(
	name: str,
	source: str,
	destination: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/{name}',
		)
		execute_command(temporary, [
			'makepri',
			'createconfig',
			'/cf', f'{temporary}/{name}/priconfig.xml',
			'/dq', f'en-US',
			'/o',
		])
		execute_command(temporary, [
			'makepri',
			'new',
			'/cf', f'{temporary}/{name}/priconfig.xml',
			'/pr', f'{temporary}/{name}',
			'/of', f'{temporary}/{name}/resources.pri',
			'/o',
		])
		execute_command(temporary, [
			'makeappx',
			'pack',
			'/o',
			'/h', f'SHA256',
			'/d', f'{temporary}/{name}',
			'/p', f'{temporary}/{name}.msix',
		])
		fs_copy(
			f'{temporary}/{name}.msix',
			f'{destination}',
		)
	return

# ----------------

def sign_macintosh_executable(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		keystore_file, keystore_password = get_project_keystore()
		keystore_name = '-'
		keychain_file = f'{temporary}/temporary.keychain'
		keychain_password = 'temporary'
		if keystore_file != None:
			execute_command(temporary, [
				'security',
				'create-keychain',
				'-p', f'{keychain_password}',
				f'{keychain_file}',
			])
			execute_command(temporary, [
				'security',
				'import',
				f'{keystore_file}',
				'-k', f'{keychain_file}',
				'-P', f'{keystore_password}',
				'-T', f'/usr/bin/codesign',
			])
			execute_command(temporary, [
				'security',
				'unlock-keychain',
				'-p', f'{keychain_password}',
				f'{keychain_file}',
			])
			execute_command(temporary, [
				'security',
				'set-key-partition-list',
				'-S', f'apple-tool:,apple:',
				'-s',
				'-k', f'{keychain_password}',
				f'{keychain_file}',
			])
			_, list_keychains_output, _ = execute_command(temporary, [
				'security',
				'list-keychains',
			], want_output=True)
			execute_command(temporary, [
				'security',
				'list-keychains',
				'-s', *map(lambda it: it.strip(' "'), list_keychains_output.splitlines()), keychain_file,
			])
			_, list_keychains_output, _ = execute_command(temporary, [
				'security',
				'list-keychains',
			], want_output=True)
			_, find_identity_output, _ = execute_command(temporary, [
				'security',
				'find-identity',
				f'{keychain_file}',
			], want_output=True)
			find_identity_match = re.search(r" ([0-9A-F]{40}) ", find_identity_output, flags=re.RegexFlag.MULTILINE)
			if find_identity_match == None:
				raise RuntimeError('could not import keystore')
			keystore_name = find_identity_match.group(1)
		default_entitlements = f'{temporary}/default.entitlements'
		fs_write_file(
			default_entitlements,
			'<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"><plist version="1.0"><dict></dict></plist>',
		)
		package_list: list[str] = []
		package_list += fs_resolve(f'{target}/Contents/Frameworks/*.framework')
		package_list += fs_resolve(f'{target}/Contents/PlugIns/*.appex')
		package_list += fs_resolve(f'{target}/Frameworks/*.framework')
		package_list += fs_resolve(f'{target}/PlugIns/*.appex')
		package_list += [target]
		for package in package_list:
			package_entitlements = f'{temporary}/original.{str(pathlib.Path(package).relative_to(target)).replace('/', '_')}.entitlements'
			execute_command(temporary, [
				'codesign',
				'-d',
				'--entitlements', f':{package_entitlements}',
				f'{package}',
			], ensure_ok=False)
			if not pathlib.Path(package_entitlements).exists():
				package_entitlements = default_entitlements
			execute_command(temporary, [
				'codesign',
				'-s', f'{keystore_name}',
				'--entitlements', f'{package_entitlements}',
				'--force',
				f'{package}',
			])
		if keystore_file != None:
			execute_command(temporary, [
				'security',
				'delete-keychain',
				f'{keychain_file}',
			])
	return

def pack_macintosh_dmg(
	name: str,
	source: str,
	destination: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/{name}.app',
		)
		execute_command(temporary, [
			'create-dmg',
			f'{temporary}/{name}.dmg',
			f'{temporary}/{name}.app',
		])
		fs_copy(
			f'{temporary}/{name}.dmg',
			f'{destination}',
		)
	return

# ----------------

def sign_android_apk(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		keystore_file, keystore_password = get_project_keystore()
		if keystore_file == None:
			return
		execute_command(temporary, [
			'apksigner',
			'sign',
			'--v1-signing-enabled', f'false',
			'--v2-signing-enabled', f'false',
			'--v3-signing-enabled', f'true',
			'--v4-signing-enabled', f'false',
			'--ks', f'{keystore_file}',
			'--ks-pass', f'pass:{keystore_password}',
			f'{target}',
		])
	return

# ----------------

def sign_iphone_executable(
	target: str,
) -> None:
	return sign_macintosh_executable(target)

def pack_iphone_ipa(
	name: str,
	source: str,
	destination: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/Payload/{name}.app',
		)
		shutil.make_archive(
			f'{temporary}/Payload',
			'zip',
			f'{temporary}',
			f'Payload',
		)
		fs_copy(
			f'{temporary}/Payload.zip',
			f'{destination}',
		)
	return

# ----------------

def setup_common_cpp_library(
	platform: str,
) -> None:
	library_directory = None
	library_file_list = None
	if check_platform(platform, ['windows.amd64']):
		clang_file = shutil.which('clang')
		if clang_file == None:
			raise RuntimeError('could not found clang path')
		library_directory_list = fs_resolve(str(pathlib.Path(clang_file).parent.parent / 'x86_64-w64-mingw32/bin'))
		if len(library_directory_list) == 0:
			raise RuntimeError('could not found library directory')
		library_directory = library_directory_list[0]
		library_file_list = ['libc++.dll', 'libunwind.dll']
	if check_platform(platform, ['android.arm64']):
		ndk_home = os.environ.get('ANDROID_NDK_HOME')
		if ndk_home == None:
			raise RuntimeError('could not found ndk path')
		library_directory_list = fs_resolve(str(pathlib.Path(ndk_home) / 'toolchains/llvm/prebuilt/*/sysroot/usr/lib/aarch64-linux-android'))
		if len(library_directory_list) == 0:
			raise RuntimeError('could not found library directory')
		library_directory = library_directory_list[0]
		library_file_list = ['libc++_shared.so']
	destination = f'{get_project_local('library')}/{platform}'
	fs_create_directory(
		destination,
	)
	if library_directory != None and library_file_list != None:
		for library_file in library_file_list:
			fs_copy(
				f'{library_directory}/{library_file}',
				f'{destination}/{library_file}',
			)
	return
