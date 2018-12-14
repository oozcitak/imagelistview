$env:VSINSTALLDIR="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community"
$env:VisualStudioVersion="15.8"

$docfx = "..\packages\docfx.console.2.40.4\tools\docfx.exe"

Start-Process $docfx -ArgumentList "docfx.json --serve"
Start-Sleep -s 5
Start-Process "http://localhost:8080"