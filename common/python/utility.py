import os
import sys
import re
import pathlib
import shutil
import tempfile
import subprocess

# ----------------

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

def fs_read_file(
	source: str,
) -> str:
	return pathlib.Path(source).read_text('utf-8')

def fs_create_directory(
	target: str,
) -> None:
	os.makedirs(target, exist_ok=True)
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

# ----------------

def execute_command(
	location: str,
	command: list[str],
	environment: dict[str, str] = {},
) -> None:
	actual_environment = os.environ.copy()
	for environment_name, environment_value in environment.items():
		actual_environment[environment_name] = environment_value
	subprocess.run(command, env=actual_environment, cwd=location, shell=sys.platform == 'win32').check_returncode()
	return

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
) -> str:
	return f'{get_project()}/.local'

def get_project_certificate(
	type: str,
) -> tuple[str | None, str]:
	file = f'{get_project_local()}/certificate/file.{type}'
	if not pathlib.Path(file).is_file():
		return (None, '')
	password = fs_read_file(f'{get_project_local()}/certificate/password.{type}.txt')
	return (file, password)

def get_project_distribution(
	name: str | None,
) -> str:
	return f'{get_project_local()}/distribution{'' if name is None else f'/{name}'}'

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
