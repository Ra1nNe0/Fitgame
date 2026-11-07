@echo off
rem Builds pose_server.py into a folder with pose_server.exe (PyInstaller), so players need no Python.
rem Output: Tools\pose_build\dist\pose_server\pose_server.exe (+ pose model next to it).
rem Unity copies this folder into <Game>_Data\StreamingAssets\PoseServer\bin on build
rem (see Assets\Editor\PoseServerBuildStep.cs).
setlocal
set "HERE=%~dp0"
set "SRC=%HERE%..\..\FitQuest\FitQuest\Assets\StreamingAssets\PoseServer"
set "VENV=%LOCALAPPDATA%\FitQuest\venv"
set "MODEL=%LOCALAPPDATA%\FitQuest\models\pose_landmarker_lite.task"

if not exist "%VENV%\Scripts\python.exe" (
  echo No Python env %VENV%. Run %SRC%\start_pose_server.bat once first.
  exit /b 1
)
if not exist "%MODEL%" (
  echo No model %MODEL%. Run the pose server once, it downloads the model.
  exit /b 1
)

"%VENV%\Scripts\python.exe" -m pip install --quiet pyinstaller || exit /b 1

pushd "%HERE%"
"%VENV%\Scripts\python.exe" -m PyInstaller --noconfirm --clean --onedir --noconsole ^
  --name pose_server ^
  --collect-all mediapipe ^
  --exclude-module tkinter --exclude-module sounddevice ^
  --add-data "%MODEL%;." ^
  --distpath "%HERE%dist" --workpath "%HERE%work" --specpath "%HERE%work" ^
  "%SRC%\pose_server.py"
set "ERR=%errorlevel%"
popd
if not "%ERR%"=="0" exit /b %ERR%

copy /y "%MODEL%" "%HERE%dist\pose_server\" >nul
echo.
echo Done: %HERE%dist\pose_server\pose_server.exe
