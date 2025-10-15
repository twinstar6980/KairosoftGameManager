import os
import sys
import re
import pathlib
import shutil
import tempfile
import subprocess

# ----------------

def fs_copy(source: str, destination: str) -> None:
    if not pathlib.Path(source).exists():
        raise RuntimeError(f'invalid source \'{source}\'')
    if not pathlib.Path(destination).parent.exists():
        fs_create_directory(f'{pathlib.Path(destination).parent}')
    if pathlib.Path(source).is_file():
        shutil.copy(source, destination)
    if pathlib.Path(source).is_dir():
        shutil.copytree(source, destination)
    return

def fs_remove(source: str) -> None:
    if pathlib.Path(source).is_file():
        os.remove(source)
    if pathlib.Path(source).is_dir():
        shutil.rmtree(source)
    return

def fs_read_file(source: str) -> str:
    return pathlib.Path(source).read_text('utf-8')

def fs_create_directory(target: str) -> None:
    os.makedirs(target, exist_ok=True)
    return

# ----------------

def execute_command(location: str, command: list[str], environment: dict[str, str] = {}) -> None:
    actual_environment = os.environ.copy()
    for environment_name, environment_value in environment.items():
        actual_environment[environment_name] = environment_value
    subprocess.run(command, env=actual_environment, cwd=location, shell=sys.platform == 'win32').check_returncode()
    return

# ----------------

def get_project() -> str:
    return f'{pathlib.Path(__file__).absolute().parent.parent.parent}'

def get_project_module(path: str) -> tuple[str, str]:
    name = pathlib.Path(path).parent.name
    name_snake = '_'.join([item.lower() for item in re.split(r'(?=[A-Z])', name)[1:]])
    return (f'{get_project()}/{name}', name_snake)

def get_project_local() -> str:
    return f'{get_project()}/.local'

def get_project_certificate(type: str) -> tuple[str | None, str]:
    file = f'{get_project_local()}/certificate/file.{type}'
    if not pathlib.Path.is_file(file):
        return (None, '')
    password = fs_read_file(f'{get_project_local()}/certificate/password.{type}.txt')
    return (file, password)

def get_project_distribution(name: str | None) -> str:
    return f'{get_project_local()}/distribution{'' if name is None else f'/{name}'}'

# ----------------

def sign_windows_msix(target: str) -> None:
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
