Param(
     [Parameter(Mandatory=$true, HelpMessage="NuGet API Key")][String]$apikey
   )

# download latest nuget.exe
$nugeturl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$nugetexe = "$($env:temp)\nuget.exe"
Remove-Item "$($nugetexe)"
$client = new-object System.Net.WebClient
$client.DownloadFile($nugeturl, $nugetexe)

# create the nuspec file
iex "$($nugetexe) pack '.\ImageListView\ImageListView.csproj' -Properties Configuration=Release"
iex "$($nugetexe) push .\*.nupkg $($apikey)"
Remove-Item ".\*.nupkg"