import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from common.python.utility import *

# ----------------

def main(platform: str) -> None:
    if platform not in ['amd64']:
        raise RuntimeError(f'invalid platform \'{platform}\'')
    module_directory, module_name = get_project_module(__file__)
    module_distribution_file = get_project_distribution(f'{platform}.{module_name}.msix')
    if platform in ['amd64']:
        fs_remove(
            f'{module_distribution_file}',
        )
        execute_command(module_directory, [
            'MSBuild',
            '-restore',
            '-verbosity:minimal',
            '-property:Platform=x64',
            '-property:Configuration=Release',
            '-property:GenerateAppxPackageOnBuild=true',
        ])
        fs_copy(
            f'{list(pathlib.Path(f'{module_directory}/.build/bin/Application/x64.Release/AppPackages').glob('Application_*_Test/Application_*.msix'))[0]}',
            f'{module_distribution_file}',
        )
        sign_windows_msix(
            f'{module_distribution_file}',
        )
    print(f'>> DISTRIBUTE >> {module_distribution_file}')

if __name__ == '__main__':
    main(sys.argv[1])
