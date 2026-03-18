@echo off
set /p GH_USER="Enter your GitHub Username: "
set REPO_NAME=QuickMediaIngest

echo.
echo initializing Git in current folder...
git init

echo Adding files...
git add .

echo Committing...
git commit -m "Initial commit - Quick Media Ingest"

echo Setting branch to main...
git branch -M main

echo Checking for existing remote...
git remote remove origin 2>nul

echo Adding remote origin for %GH_USER%...
git remote add origin https://github.com/%GH_USER%/%REPO_NAME%.git

echo.
echo === IMPORTANT ===
echo Make sure you have created the repository "%REPO_NAME%" on GitHub.com BEFORE continuing!
echo.
pause

echo Pushing to GitHub...
git push -u origin main

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Push failed. 
    echo 1. Verify you created the repo '%REPO_NAME%' on GitHub.
    echo 2. Verify your credentials/tokens are set up in Git.
) else (
    echo [SUCCESS] Pushed to GitHub!
)
pause
