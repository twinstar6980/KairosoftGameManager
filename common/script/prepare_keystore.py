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
	keystore_directory = utility.get_project_local('keystore')
	utility.fs_create_directory(
		f'{keystore_directory}',
	)
	utility.generate_keystore(
		f'{keystore_directory}/file.p12',
		password,
		subject,
	)
	utility.fs_write_file(
		f'{keystore_directory}/password.txt',
		f'{password}',
	)
	print(f'>> KEYSTORE >> {keystore_directory}')
	return

if __name__ == '__main__':
	main(sys.argv[1], sys.argv[2])
