@echo off
set /p TAG_NAME="Enter Version Tag (e.g., v1.0.0): "

echo.
echo Adding local tag: %TAG_NAME%
git tag %TAG_NAME%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Could not create local tag. It might already exist!
    pause
    exit /b
)

echo Pushing tag to GitHub...
git push origin %TAG_NAME%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Push failed. 
    echo 1. Verify you have already pushed the repository to GitHub.
    echo 2. Verify you have permissions to push tags.
) else (
    echo.
    echo [SUCCESS] Tag %TAG_NAME% pushed to GitHub!
    echo Check the 'Actions' or 'Releases' tab on GitHub to see the build start.
)
pause
