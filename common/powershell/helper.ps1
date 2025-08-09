param (
)

$Global:ErrorActionPreference = "Stop"

$ProjectDirectory = (Resolve-Path -Path "${PSScriptRoot}/../..").Path.Replace('\', '/')
$ProjectLocalDirectory = "${ProjectDirectory}/.local"
$ProjectCertificateDirectory = "${ProjectLocalDirectory}/certificate"
$ProjectTemporaryDirectory = "${ProjectLocalDirectory}/temporary"
$ProjectDistributionDirectory = "${ProjectLocalDirectory}/distribution"

function My-SignMsix(
	[Parameter(Mandatory)]
	[String] $Source,
	[Parameter(Mandatory)]
	[String] $Destination
) {
	$CertificateFile = "${ProjectCertificateDirectory}/file.pfx"
	$CertificatePassword = Get-Content -Path "${ProjectCertificateDirectory}/password.txt"
	New-Item -Force -ItemType "Directory" -Path "${ProjectTemporaryDirectory}/SignMsix"
	Copy-Item -Force -Recurse -Path "${Source}" -Destination "${ProjectTemporaryDirectory}/SignMsix/unsigned.msix"
	signtool "sign" "/q" "/fd" "SHA256" "/f" "${CertificateFile}" "/p" "${CertificatePassword}" "${ProjectTemporaryDirectory}/SignMsix/unsigned.msix"
	Copy-Item -Force -Recurse -Path "${ProjectTemporaryDirectory}/SignMsix/unsigned.msix" -Destination "${Destination}"
	Remove-Item -Force -Recurse -Path "${ProjectTemporaryDirectory}/SignMsix"
	return
}
