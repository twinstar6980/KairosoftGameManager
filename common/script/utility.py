import os
import sys
import re
import pathlib
import glob
import shutil
import tempfile
import subprocess
import typing

# ----------------

def fs_find(
	pattern: str,
) -> list[str]:
	return glob.glob(pattern)

def fs_temporary(
) -> tempfile.TemporaryDirectory[str]:
	return tempfile.TemporaryDirectory()

def fs_copy(
	target: str,
	placement: str,
	follow_link: bool = False,
) -> None:
	if not pathlib.Path(target).exists():
		raise RuntimeError(f'invalid source \'{target}\'')
	if pathlib.Path(placement).exists():
		fs_remove(placement)
	if not pathlib.Path(placement).parent.exists():
		fs_create_directory(f'{pathlib.Path(placement).parent}')
	if pathlib.Path(target).is_file():
		shutil.copy(target, placement, follow_symlinks=follow_link)
	if pathlib.Path(target).is_dir():
		shutil.copytree(target, placement, symlinks=follow_link, dirs_exist_ok=True)
	return

def fs_remove(
	target: str,
) -> None:
	if pathlib.Path(target).is_symlink():
		os.remove(target)
	if pathlib.Path(target).is_file():
		os.remove(target)
	if pathlib.Path(target).is_dir():
		shutil.rmtree(target)
	return

def fs_create_link(
	target: str,
	referent: str,
	is_directory: bool,
) -> None:
	if pathlib.Path(target).exists():
		fs_remove(target)
	os.symlink(referent, target, is_directory)
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

def sh_execute_command(
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
		shell=sys.platform == 'win32',
		cwd=location,
		env=actual_environment,
		check=ensure_ok,
		capture_output=want_output,
		text=True,
		encoding='utf-8',
	)
	return (result.returncode, result.stdout, result.stderr)

# ----------------

def generate_keystore(
	target: str,
	password: str,
	subject: str,
	time: tuple[str, str],
) -> None:
	with fs_temporary() as temporary:
		sh_execute_command(temporary, [
			'openssl',
			'genrsa',
			'-out', f'{temporary}/file.key',
			f'2048',
		])
		sh_execute_command(temporary, [
			'openssl',
			'req',
			'-new',
			'-x509',
			'-key', f'{temporary}/file.key',
			'-out', f'{temporary}/file.crt',
			'-utf8',
			'-not_before', f'{time[0]}',
			'-not_after', f'{time[1]}',
			'-subj', f'{subject}',
			'-addext', f'basicConstraints=CA:FALSE',
			'-addext', f'keyUsage=digitalSignature',
			'-addext', f'extendedKeyUsage=codeSigning',
		])
		sh_execute_command(temporary, [
			'openssl',
			'pkcs12',
			'-export',
			'-inkey', f'{temporary}/file.key',
			'-in', f'{temporary}/file.crt',
			'-out', f'{temporary}/file.p12',
			'-passout', f'pass:{password}',
			'-keypbe', f'PBE-SHA1-3DES',
			'-certpbe', f'PBE-SHA1-3DES',
			'-macalg', f'SHA1',
		])
		fs_copy(
			f'{temporary}/file.p12',
			f'{target}',
		)
	return

def verify_keystore(
	target: str,
	password: str,
) -> bool:
	with fs_temporary() as temporary:
		openssl_result, _, _ = sh_execute_command(temporary, [
			'openssl',
			'pkcs12',
			'-in', f'{target}',
			'-passin', f'pass:{password}',
			'-noout',
		], ensure_ok=False)
	return openssl_result == 0

def query_keystore(
	target: str,
	password: str,
) -> str:
	with fs_temporary() as temporary:
		sh_execute_command(temporary, [
			'openssl',
			'pkcs12',
			'-in', f'{target}',
			'-passin', f'pass:{password}',
			'-out', f'{temporary}/file.crt',
			'-nokeys',
		])
		sh_execute_command(temporary, [
			'openssl',
			'x509',
			'-in', f'{temporary}/file.crt',
			'-out', f'{temporary}/result.txt',
			'-noout',
			'-subject',
		])
		result_content = fs_read_file(
			f'{temporary}/result.txt',
		)
		result_subject = re.search(
			r'^subject=(.*)$',
			result_content,
			re.MULTILINE,
		)
		if result_subject == None:
			raise RuntimeError(f'unable to get subject')
	return result_subject.group(1)

# ----------------

def pack_zip(
	source: str,
	destination: str,
	name: str | None,
	follow_link: bool = False,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package{'' if name == None else f'/{name}'}',
			follow_link=follow_link,
		)
		shutil.make_archive(
			f'{temporary}/package',
			'zip',
			f'{temporary}/package',
			f'{'.' if name == None else f'{name}'}',
		)
		fs_copy(
			f'{temporary}/package.zip',
			f'{destination}',
		)
	return

def unpack_zip(
	source: str,
	destination: str,
	name: str | None,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package.zip',
		)
		shutil.unpack_archive(
			f'{temporary}/package.zip',
			f'{temporary}/package',
			'zip',
		)
		fs_copy(
			f'{temporary}/package{'' if name == None else f'/{name}'}',
			f'{destination}',
			follow_link=True,
		)
	return

# ----------------

def strip_windows_binary(
	source: str,
	destination: str,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/target',
		)
		sh_execute_command(temporary, [
			'llvm-strip',
			'--strip-all',
			f'{temporary}/target',
		])
		fs_copy(
			f'{temporary}/target',
			f'{destination}',
		)
	return

def apply_windows_manifest(
	source: str,
	destination: str,
	manifest: str,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/target',
		)
		fs_copy(
			f'{manifest}',
			f'{temporary}/manifest',
		)
		sh_execute_command(temporary, [
			'mt',
			'-manifest', f'{temporary}/manifest',
			f'-outputresource:{temporary}/target;#1',
			'-verbose',
		])
		fs_copy(
			f'{temporary}/target',
			f'{destination}',
		)
	return

def sign_windows_executable(
	source: str,
	destination: str,
	type: str,
	keystore: tuple[str, str] | None,
) -> None:
	with fs_temporary() as temporary:
		if keystore == None:
			return
		keystore_file, keystore_password = keystore
		fs_copy(
			f'{source}',
			f'{temporary}/target.{type}',
		)
		if type == 'msix':
			keystore_subject = query_keystore(keystore_file, keystore_password)
			unpack_windows_msix(
				f'{temporary}/target.{type}',
				f'{temporary}/package',
			)
			manifest_content = fs_read_file(
				f'{temporary}/package/AppxManifest.xml',
			)
			manifest_content = re.sub(
				r'(<Identity\s.*Publisher\s*=\s*")([^"]*)("\s.*/>)',
				rf'\1{keystore_subject}\3',
				manifest_content,
				re.RegexFlag.MULTILINE,
			)
			fs_write_file(
				f'{temporary}/package/AppxManifest.xml',
				manifest_content,
			)
			pack_windows_msix(
				f'{temporary}/package',
				f'{temporary}/target.{type}',
				False,
			)
		sh_execute_command(temporary, [
			'signtool',
			'sign',
			'/fd', f'SHA256',
			'/f', f'{keystore_file}',
			'/p', f'{keystore_password}',
			f'{temporary}/target.{type}',
		])
		fs_copy(
			f'{temporary}/target.{type}',
			f'{destination}',
		)
	return

def pack_windows_msix(
	source: str,
	destination: str,
	generate_pri: bool,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package',
			follow_link=True,
		)
		if generate_pri:
			sh_execute_command(temporary, [
				'makepri',
				'createconfig',
				'/cf', f'{temporary}/priconfig.xml',
				'/dq', f'en-US',
				'/o',
			])
			sh_execute_command(temporary, [
				'makepri',
				'new',
				'/cf', f'{temporary}/priconfig.xml',
				'/pr', f'{temporary}/package',
				'/of', f'{temporary}/package/resources.pri',
				'/o',
			])
		sh_execute_command(temporary, [
			'makeappx',
			'pack',
			'/o',
			'/h', f'SHA256',
			'/d', f'{temporary}/package',
			'/p', f'{temporary}/package.msix',
		])
		fs_copy(
			f'{temporary}/package.msix',
			f'{destination}',
		)
	return

def unpack_windows_msix(
	source: str,
	destination: str,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package.msix',
		)
		sh_execute_command(temporary, [
			'makeappx',
			'unpack',
			'/o',
			'/p', f'{temporary}/package.msix',
			'/d', f'{temporary}/package',
		])
		fs_copy(
			f'{temporary}/package',
			f'{destination}',
			follow_link=True,
		)
	return

# ----------------

def sign_macintosh_executable(
	source: str,
	destination: str,
	keystore: tuple[str, str] | None,
) -> None:
	with fs_temporary() as temporary:
		keystore_name = '-'
		keychain_file = f'{temporary}/temporary.keychain'
		keychain_password = 'temporary'
		if keystore != None:
			keystore_file, keystore_password = keystore
			sh_execute_command(temporary, [
				'security',
				'create-keychain',
				'-p', f'{keychain_password}',
				f'{keychain_file}',
			])
			sh_execute_command(temporary, [
				'security',
				'import',
				f'{keystore_file}',
				'-k', f'{keychain_file}',
				'-P', f'{keystore_password}',
				'-T', f'/usr/bin/codesign',
			])
			sh_execute_command(temporary, [
				'security',
				'unlock-keychain',
				'-p', f'{keychain_password}',
				f'{keychain_file}',
			])
			sh_execute_command(temporary, [
				'security',
				'set-key-partition-list',
				'-S', f'apple-tool:,apple:',
				'-s',
				'-k', f'{keychain_password}',
				f'{keychain_file}',
			])
			_, list_keychains_output, _ = sh_execute_command(temporary, [
				'security',
				'list-keychains',
			], want_output=True)
			sh_execute_command(temporary, [
				'security',
				'list-keychains',
				'-s', *[item.strip(' "') for item in list_keychains_output.splitlines()], keychain_file,
			])
			_, find_identity_output, _ = sh_execute_command(temporary, [
				'security',
				'find-identity',
				f'{keychain_file}',
			], want_output=True)
			find_identity_match = re.search(r" ([0-9A-F]{40}) ", find_identity_output, flags=re.RegexFlag.MULTILINE)
			if find_identity_match == None:
				raise RuntimeError(f'could not import keystore')
			keystore_name = find_identity_match.group(1)
		default_entitlements = f'{temporary}/default.entitlements'
		fs_write_file(
			default_entitlements,
			'<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"><plist version="1.0"><dict></dict></plist>',
		)
		fs_copy(
			f'{source}',
			f'{temporary}/target',
			follow_link=True,
		)
		target_item_list: list[str] = []
		target_item_list += fs_find(f'{temporary}/target/Contents/Frameworks/*.framework')
		target_item_list += fs_find(f'{temporary}/target/Contents/PlugIns/*.appex')
		target_item_list += fs_find(f'{temporary}/target/Frameworks/*.framework')
		target_item_list += fs_find(f'{temporary}/target/PlugIns/*.appex')
		target_item_list += [f'{temporary}/target']
		for target_item in target_item_list:
			target_item_entitlements = f'{temporary}/original.{str(pathlib.Path(target_item).relative_to(f'{temporary}/target')).replace('/', '_')}.entitlements'
			sh_execute_command(temporary, [
				'codesign',
				'-d',
				'--entitlements', f':{target_item_entitlements}',
				f'{target_item}',
			], ensure_ok=False)
			if not pathlib.Path(target_item_entitlements).exists():
				target_item_entitlements = default_entitlements
			sh_execute_command(temporary, [
				'codesign',
				'-s', f'{keystore_name}',
				'--keychain', f'{keychain_file}',
				'--entitlements', f'{target_item_entitlements}',
				'--force',
				f'{target_item}',
			])
		if keystore != None:
			sh_execute_command(temporary, [
				'security',
				'delete-keychain',
				f'{keychain_file}',
			])
		fs_copy(
			f'{temporary}/target',
			f'{destination}',
			follow_link=True,
		)
	return

def pack_macintosh_dmg(
	source: str,
	destination: str,
	name: str,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package/{name}.app',
			follow_link=True,
		)
		sh_execute_command(temporary, [
			'create-dmg',
			'--window-pos', f'0', f'0',
			'--window-size', f'500', f'300',
			'--text-size', f'16',
			'--icon-size', f'128',
			'--icon', f'{name}.app', f'150', f'100',
			'--app-drop-link', f'350', f'100',
			'--format', f'UDZO',
			'--filesystem', f'APFS',
			f'{temporary}/package.dmg',
			f'{temporary}/package/{name}.app',
		])
		fs_copy(
			f'{temporary}/package.dmg',
			f'{destination}',
		)
	return

# ----------------

def sign_android_apk(
	source: str,
	destination: str,
	keystore: tuple[str, str] | None,
) -> None:
	with fs_temporary() as temporary:
		if keystore == None:
			return
		keystore_file, keystore_password = keystore
		fs_copy(
			f'{source}',
			f'{temporary}/target.apk',
		)
		sh_execute_command(temporary, [
			'apksigner',
			'sign',
			'--v1-signing-enabled', f'false',
			'--v2-signing-enabled', f'false',
			'--v3-signing-enabled', f'true',
			'--v4-signing-enabled', f'false',
			'--ks', f'{keystore_file}',
			'--ks-pass', f'pass:{keystore_password}',
			f'{temporary}/target.apk',
		])
		fs_copy(
			f'{temporary}/target.apk',
			f'{destination}',
		)
	return

# ----------------

def sign_iphone_executable(
	source: str,
	destination: str,
	keystore: tuple[str, str] | None,
) -> None:
	return sign_macintosh_executable(source, destination, keystore)

def pack_iphone_ipa(
	source: str,
	destination: str,
	name: str,
) -> None:
	with fs_temporary() as temporary:
		fs_copy(
			f'{source}',
			f'{temporary}/package/Payload/{name}.app',
			follow_link=True,
		)
		shutil.make_archive(
			f'{temporary}/package',
			'zip',
			f'{temporary}/package',
			f'',
		)
		fs_copy(
			f'{temporary}/package.zip',
			f'{destination}',
		)
	return

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

def locate_project(
	name: str | None = None,
) -> str:
	return f'{pathlib.Path(__file__).absolute().parent.parent.parent.as_posix()}{'' if name is None else f'/{name}'}'

def locate_project_local(
	name: str | None = None,
) -> str:
	return f'{locate_project()}/.local{'' if name is None else f'/{name}'}'

def locate_project_distribution(
	name: str | None = None,
) -> str:
	return f'{locate_project_local('distribution')}{'' if name is None else f'/{name}'}'

def locate_project_keystore(
) -> tuple[str, str] | None:
	file = f'{locate_project_local('keystore')}/file.p12'
	if not pathlib.Path(file).is_file():
		return None
	password = fs_read_file(f'{locate_project_local('keystore')}/password.txt')
	if not verify_keystore(file, password):
		raise RuntimeError('invaild keystore password')
	return (file, password)

# ----------------

def setup_project_library(
	platform: str,
) -> None:
	library_directory = None
	library_file_list = None
	if check_platform(platform, ['windows.amd64']):
		clang_file = shutil.which('clang')
		if clang_file == None:
			raise RuntimeError(f'could not found clang path')
		library_directory_list = fs_find(str(pathlib.Path(clang_file).parent.parent / 'x86_64-w64-mingw32/bin'))
		if len(library_directory_list) == 0:
			raise RuntimeError(f'could not found library directory')
		library_directory = library_directory_list[0]
		library_file_list = ['libc++.dll', 'libunwind.dll']
	if check_platform(platform, ['android.arm64']):
		ndk_home = os.environ.get('ANDROID_NDK_HOME')
		if ndk_home == None:
			raise RuntimeError(f'could not found ndk path')
		library_directory_list = fs_find(str(pathlib.Path(ndk_home) / 'toolchains/llvm/prebuilt/*/sysroot/usr/lib/aarch64-linux-android'))
		if len(library_directory_list) == 0:
			raise RuntimeError(f'could not found library directory')
		library_directory = library_directory_list[0]
		library_file_list = ['libc++_shared.so']
	destination = f'{locate_project_local('library')}/{platform}'
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

def build_project_module(
	path: str,
	build: typing.Callable[[str, tuple[str, str] | None, str, str], tuple[str, str] | None],
	platform: str,
	is_single: bool = False,
) -> None:
	with fs_temporary() as temporary:
		module_name_original = pathlib.Path(path).parent.name
		module_name_regularized = 'application' if is_single else '_'.join([item.lower() for item in re.split(r'(?=[A-Z])', module_name_original)[1:]])
		build_result = build(
			locate_project(module_name_original),
			locate_project_keystore(),
			temporary,
			platform,
		)
		if build_result == None:
			raise RuntimeError(f'unsupported platform \'{platform}\'')
		distribution_file = locate_project_distribution(f'{platform}.{module_name_regularized}{build_result[0]}')
		fs_copy(
			build_result[1],
			distribution_file,
		)
		print(f'>> BUILD MODULE >> {distribution_file}')
	return

def build_project_bundle(
	path: str,
	build: typing.Callable[[str, str, str, tuple[str, str] | None, str, str], tuple[str, str] | None],
	platform: str,
	is_single: bool = False,
) -> None:
	with fs_temporary() as temporary:
		build_result = build(
			locate_project(),
			locate_project_local(),
			locate_project_distribution(),
			locate_project_keystore(),
			temporary,
			platform,
		)
		if build_result == None:
			raise RuntimeError(f'unsupported platform \'{platform}\'')
		if is_single:
			distribution_file = build_result[1]
		else:
			distribution_file = locate_project_distribution(f'{platform}.bundle{build_result[0]}')
			fs_copy(
				build_result[1],
				distribution_file,
			)
		print(f'>> BUILD BUNDLE >> {distribution_file}')
	return
