import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import common.script.utility as utility
import Windows.build as build_windows

# ----------------

def build(
	source: str,
	local: str,
	distribution: str,
	keystore: tuple[str, str] | None,
	temporary: str,
	platform: str,
) -> tuple[str, str] | None:
	destination = None
	if not utility.check_platform(platform, ['windows.amd64']):
		return destination
	# build
	if utility.check_platform(platform, ['windows.amd64']):
		utility.build_project_module(build_windows.__file__, build_windows.build, platform, is_single=True)
		destination = ('', f'{distribution}/{platform}.{'application'}{'.msix'}')
	return destination

if __name__ == '__main__':
	utility.build_project_bundle(__file__, build, sys.argv[1], is_single=True)
