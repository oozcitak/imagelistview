Param(
     [Parameter(Mandatory=$true, HelpMessage="NuGet API Key")][String]$apikey
   )

# download latest nuget.exe
$nugeturl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$nugetexe = "$($env:temp)\nuget.exe"
$client = new-object System.Net.WebClient
$client.DownloadFile($nugeturl, $nugetexe)

# create the nuspec file for the "Release" build
Invoke-Expression "$($nugetexe) Pack '.\ImageListView\ImageListView.csproj' -Properties Configuration=Release"
Invoke-Expression "$($nugetexe) Push .\*.nupkg -Source https://www.nuget.org -ApiKey $($apikey) -Verbosity Detailed"
Remove-Item ".\*.nupkg"
