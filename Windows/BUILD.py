import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from common.python.utility import *

# ----------------

def main(
	platform: str,
) -> None:
	ensure_platform(platform, ['windows.amd64'])
	module_directory, module_name = get_project_module(__file__)
	module_distribution_file = get_project_distribution(f'{platform}.application')
	if check_platform(platform, ['windows.amd64']):
		module_distribution_file += '.msix'
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
			f'{fs_resolve(f'{module_directory}/.build/bin/Application/x64.Release/AppPackages/Application_*_Test/Application_*.msix')[0]}',
			f'{module_distribution_file}',
		)
		sign_windows_msix(
			f'{module_distribution_file}',
		)
	print(f'>> BUILD >> {module_distribution_file}')
	return

if __name__ == '__main__':
	main(sys.argv[1])
