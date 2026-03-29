import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import common.script.utility as utility

# ----------------

def build(
	source: str,
	keystore: tuple[str, str] | None,
	temporary: str,
	platform: str,
) -> tuple[str, str] | None:
	destination = None
	if utility.check_platform(platform, ['windows.amd64']):
		utility.sh_execute_command(source, [
			'MSBuild',
			'-restore',
			'-verbosity:minimal',
			'-property:Platform=x64',
			'-property:Configuration=Release',
			'-property:GenerateAppxPackageOnBuild=true',
		])
		utility.fs_copy(
			f'{utility.fs_find(f'{source}/.build/bin/Application/x64.Release/AppPackages/Application_*_Test/Application_*.msix')[0]}',
			f'{temporary}/artifact.msix',
		)
		utility.sign_windows_executable(
			f'{temporary}/artifact.msix',
			f'{temporary}/artifact.msix',
			'msix',
			keystore,
		)
		destination = ('.msix', f'{temporary}/artifact.msix')
	return destination

if __name__ == '__main__':
	utility.build_project_module(__file__, build, sys.argv[1], is_single=True)
