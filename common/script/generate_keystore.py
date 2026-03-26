import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import common.script.utility as utility

# ----------------

def main(
	target: str,
	password: str,
	subject: str,
) -> None:
	utility.generate_keystore(
		target,
		password,
		subject,
	)
	return

if __name__ == '__main__':
	main(sys.argv[1], sys.argv[2], sys.argv[3])
