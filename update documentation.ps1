Clear-Host

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$RepoURL = "https://github.com/oozcitak/imagelistview.git"
$DocsDir = Join-Path -Path $ScriptDir -ChildPath ".\Documentation\_site"
$TempDir = Join-Path -Path $ScriptDir -ChildPath ".\TEMP_DOCS"

# create a temporary directory
Set-Location $DocsDir
if (Test-Path $TempDir) { Remove-Item -Path $TempDir -Recurse -Force; }
New-Item -Path $TempDir -ItemType Directory -Force

# clone the repo with the gh-pages branch
git clone $RepoURL --branch gh-pages $TempDir

# clear repo directory
Set-Location $TempDir
git rm -r *

# Copy documentation into the repo
Copy-Item $DocsDir\* -Destination $TempDir -Recurse -Force

# push the new docs to the remote branch
git add . -A
git commit -m "Update generated documentation"
git push origin gh-pages

# remove temp directory
Set-Location $DocsDir
if (Test-Path $TempDir) { Remove-Item -Path $TempDir -Recurse -Force; }
