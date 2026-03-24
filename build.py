import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import common.script.utility as utility
import Windows.build as build_windows

# ----------------

def main(
	platform: str,
) -> None:
	utility.ensure_platform(platform, ['windows.amd64'])
	# build
	if utility.check_platform(platform, ['windows.amd64']):
		build_windows.main(platform)
	return

if __name__ == '__main__':
	main(sys.argv[1])
