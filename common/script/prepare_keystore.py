import os
import sys
sys.dont_write_bytecode = True
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
import common.script.utility as utility

# ----------------

def main(
	password: str,
	subject: str,
) -> None:
	keystore = utility.locate_project_local('keystore')
	utility.fs_create_directory(
		f'{keystore}',
	)
	utility.generate_keystore(
		f'{keystore}/file.p12',
		password,
		subject,
	)
	utility.fs_write_file(
		f'{keystore}/password.txt',
		f'{password}',
	)
	print(f'>> KEYSTORE >> {keystore}')
	return

if __name__ == '__main__':
	main(sys.argv[1], sys.argv[2])
