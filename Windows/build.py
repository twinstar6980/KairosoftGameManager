import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import common.script.utility as utility

# ----------------

def main(
	platform: str,
) -> None:
	utility.ensure_platform(platform, ['windows.amd64'])
	module_directory, module_name = utility.get_project_module(__file__)
	module_distribution_file = utility.get_project_distribution(f'{platform}.application')
	if utility.check_platform(platform, ['windows.amd64']):
		module_distribution_file += '.msix'
		utility.fs_remove(
			f'{module_distribution_file}',
		)
		utility.execute_command(module_directory, [
			'MSBuild',
			'-restore',
			'-verbosity:minimal',
			'-property:Platform=x64',
			'-property:Configuration=Release',
			'-property:GenerateAppxPackageOnBuild=true',
		])
		utility.fs_copy(
			f'{utility.fs_resolve(f'{module_directory}/.build/bin/Application/x64.Release/AppPackages/Application_*_Test/Application_*.msix')[0]}',
			f'{module_distribution_file}',
		)
		utility.sign_windows_executable(
			f'{module_distribution_file}',
		)
	print(f'>> BUILD >> {module_distribution_file}')
	return

if __name__ == '__main__':
	main(sys.argv[1])
