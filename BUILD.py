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
	if platform not in ['windows.amd64', 'linux.amd64', 'macintosh.arm64', 'android.arm64', 'iphone.arm64']:
		raise RuntimeError(f'invalid platform \'{platform}\'')
	# build
	if platform in ['windows.amd64']:
		Windows.BUILD.main(platform)

if __name__ == '__main__':
	main(sys.argv[1])
