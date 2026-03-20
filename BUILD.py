import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from common.python.utility import *
import Windows.BUILD

# ----------------

def main(
	platform: str,
) -> None:
	ensure_platform(platform, ['windows.amd64'])
	# build
	if check_platform(platform, ['windows.amd64']):
		Windows.BUILD.main(platform)
	return

if __name__ == '__main__':
	main(sys.argv[1])
