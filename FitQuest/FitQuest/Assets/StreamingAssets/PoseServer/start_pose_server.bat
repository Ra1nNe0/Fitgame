@echo off
setlocal
title FitQuest Pose Server
set "HERE=%~dp0"
set "VENV=%LOCALAPPDATA%\FitQuest\venv"
if exist "%VENV%\Scripts\python.exe" goto run
echo First start: creating Python environment for FitQuest (needs internet, about 200 MB)...
where py >nul 2>nul
if %errorlevel%==0 (py -3 -m venv "%VENV%") else (python -m venv "%VENV%")
if not exist "%VENV%\Scripts\python.exe" (
  echo.
  echo Python 3 not found. Install it from https://www.python.org/downloads/ and tick "Add python.exe to PATH".
  pause
  exit /b 1
)
"%VENV%\Scripts\python.exe" -m pip install --upgrade pip
"%VENV%\Scripts\python.exe" -m pip install -r "%HERE%requirements.txt"
if errorlevel 1 (
  echo Package installation failed. Check the internet connection and run again.
  rmdir /s /q "%VENV%"
  pause
  exit /b 1
)
:run
"%VENV%\Scripts\python.exe" "%HERE%pose_server.py" %*
if errorlevel 1 pause
