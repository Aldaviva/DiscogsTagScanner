$chromium_path = Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe" -Name "(default)"
$project_dir = Split-Path $script:MyInvocation.MyCommand.Path
$project_name = Split-Path -Leaf $project_dir 
$source_dir = "$project_dir\src"
$output_dir = "$project_dir\bin"

New-Item -Path $output_dir -ItemType Directory -Force > $null

Write-Output "Packing extension"
Start-Process -FilePath $chromium_path -ArgumentList "--pack-extension=`"$source_dir`" --pack-extension-key=`"$project_dir\PackExtensionPrivateKey.pem`"" -Wait

Move-Item -Path "$project_dir\src.crx" -Destination "$output_dir\DiscogsTagScanner.crx" -Force