if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$out = $dir + "\out"
Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue

# KK -------------------------------------
Write-Output ("Creating KK release")

New-Item -ItemType Directory -Force -Path ($out + "\BepInEx\plugins\KoikatuVR\Images") | Out-Null
New-Item -ItemType Directory -Force -Path ($out + "\BepInEx\patchers\KoikatuVR") | Out-Null
New-Item -ItemType Directory -Force -Path ($out + "\Koikatu_Data") | Out-Null

Copy-Item -Path ($dir + "\KK\*") -Destination ($out + "\BepInEx\plugins\KoikatuVR") -ErrorAction Stop -Force | Out-Null
# Copy-Item copies empty directories and I don't see any way to tell it to only copy files
Remove-Item -Path ($out + "\BepInEx\plugins\KoikatuVR\Data") -Force
Remove-Item -Path ($out + "\BepInEx\plugins\KoikatuVR\Patcher") -Force
Remove-Item -Path ($out + "\BepInEx\plugins\KoikatuVR\Plugins") -Force

Copy-Item -Path ($dir + "\KK\Patcher\*") -Destination ($out + "\BepInEx\patchers\KoikatuVR") | Out-Null
Copy-Item -Path ($dir + "\KK\Images\*") -Destination ($out + "\BepInEx\plugins\KoikatuVR\Images\") -Force | Out-Null
Copy-Item -Path ($dir + "\KK\Data\*") -Destination ($out + "\Koikatu_Data") -Recurse  | Out-Null

$ver = "v" + (Get-ChildItem -Path ($dir + "\KK\KoikatuVR.dll") -Force -ErrorAction Stop)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
Write-Output ("Version " + $ver)
Compress-Archive -Path ($out + "\*") -Force -CompressionLevel "Optimal" -DestinationPath ($dir +"KK_VR_" + $ver + ".zip")

Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue

# KKS ------------------------------------
Write-Output ("Creating KKS release")

New-Item -ItemType Directory -Force -Path ($out + "\BepInEx\plugins\KKS_VR\Images") | Out-Null
New-Item -ItemType Directory -Force -Path ($out + "\CharaStudio_Data") | Out-Null
New-Item -ItemType Directory -Force -Path ($out + "\KoikatsuSunshine_Data") | Out-Null

Copy-Item -Path ($dir + "\KKS\*") -Destination ($out + "\BepInEx\plugins\KKS_VR") -Force -ErrorAction Stop | Out-Null
# Copy-Item copies empty directories and I don't see any way to tell it to only copy files
Remove-Item -Path ($out + "\BepInEx\plugins\KKS_VR\Libs") -Force

Copy-Item -Path ($dir + "\KKS\Images\*") -Destination ($out + "\BepInEx\plugins\KKS_VR\Images\") -Force | Out-Null
Copy-Item -Path ($dir + "\KKS\Libs\_Data\*") -Destination ($out + "\CharaStudio_Data") -Recurse  | Out-Null
Copy-Item -Path ($dir + "\KKS\Libs\_Data\*") -Destination ($out + "\KoikatsuSunshine_Data") -Recurse  | Out-Null

$ver = "v" + (Get-ChildItem -Path ($dir + "\KKS\KKS_MainGameVR.dll") -Force -ErrorAction Stop)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
Write-Output ("Version " + $ver)
Compress-Archive -Path ($out + "\*") -Force -CompressionLevel "Optimal" -DestinationPath ($dir +"KKS_VR_" + $ver + ".zip")

Remove-Item -Force -Path ($out) -Recurse -ErrorAction SilentlyContinue
