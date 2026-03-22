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
		shutil.copy(source, destination)
	if pathlib.Path(source).is_dir():
		shutil.copytree(source, destination)
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
) -> int:
	actual_environment = os.environ.copy()
	for environment_name, environment_value in environment.items():
		actual_environment[environment_name] = environment_value
	result = subprocess.run(command, env=actual_environment, cwd=location, shell=sys.platform == 'win32')
	if ensure_ok:
		result.check_returncode()
	return result.returncode

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

def get_project_certificate(
	type: str,
) -> tuple[str | None, str]:
	file = f'{get_project_local('certificate')}/file.{type}'
	if not pathlib.Path(file).is_file():
		return (None, '')
	password = fs_read_file(f'{get_project_local('certificate')}/password.{type}.txt')
	return (file, password)

def get_project_distribution(
	name: str | None = None,
) -> str:
	return f'{get_project_local('distribution')}{'' if name is None else f'/{name}'}'

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

def sign_windows_msix(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		certificate_file, certificate_password = get_project_certificate('pfx')
		if certificate_file == None:
			return
		execute_command(temporary, [
			'signtool',
			'sign',
			'/q',
			'/fd', f'SHA256',
			'/f', f'{certificate_file}',
			'/p', f'{certificate_password}',
			f'{target}',
		])
	return

# ----------------

def sign_macintosh_app(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		default_entitlements = f'{temporary}/default.entitlements'
		fs_write_file(
			default_entitlements,
			'<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"><plist version="1.0"><dict></dict></plist>',
		)
		fs_create_directory(
			f'{temporary}/original',
		)
		package_list: list[str] = []
		package_list += fs_resolve(f'{target}/Frameworks/*.framework')
		package_list += fs_resolve(f'{target}/PlugIns/*.appex')
		package_list += fs_resolve(f'{target}/Contents/Frameworks/*.framework')
		package_list += fs_resolve(f'{target}/Contents/PlugIns/*.appex')
		package_list += [target]
		for package in package_list:
			package_entitlements = f'{temporary}/original/{str(pathlib.Path(package).relative_to(target)).replace('/', '_')}.entitlements'
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
				'--force',
				'--sign', '-',
				'--entitlements', f'{package_entitlements}',
				f'{package}',
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
		certificate_file, certificate_password = get_project_certificate('jks')
		if certificate_file == None:
			return
		execute_command(temporary, [
			'apksigner',
			'sign',
			'--v1-signing-enabled', f'false',
			'--v2-signing-enabled', f'false',
			'--v3-signing-enabled', f'true',
			'--v4-signing-enabled', f'false',
			'--ks', f'{certificate_file}',
			'--ks-pass', f'pass:{certificate_password}',
			f'{target}',
		])
	return

# ----------------

def sign_iphone_binary(
	target: str,
) -> None:
	with tempfile.TemporaryDirectory() as temporary:
		execute_command(temporary, [
			'ldid',
			'-S',
			f'{target}',
		])
	return

def sign_iphone_app(
	target: str,
) -> None:
	return sign_macintosh_app(target)

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
			raise RuntimeError('can not found clang path')
		library_directory_list = fs_resolve(str(pathlib.Path(clang_file).parent.parent / 'x86_64-w64-mingw32/bin'))
		if len(library_directory_list) == 0:
			raise RuntimeError('can not found library directory')
		library_directory = library_directory_list[0]
		library_file_list = ['libc++.dll', 'libunwind.dll']
	if check_platform(platform, ['android.arm64']):
		ndk_home = os.environ.get('ANDROID_NDK_HOME')
		if ndk_home == None:
			raise RuntimeError('can not found ndk path')
		library_directory_list = fs_resolve(str(pathlib.Path(ndk_home) / 'toolchains/llvm/prebuilt/*/sysroot/usr/lib/aarch64-linux-android'))
		if len(library_directory_list) == 0:
			raise RuntimeError('can not found library directory')
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
