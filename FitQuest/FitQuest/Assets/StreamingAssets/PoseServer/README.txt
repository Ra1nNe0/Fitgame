FitQuest Pose Server — распознавание позы с веб-камеры.

Игра запускает start_pose_server.bat сама (меню «Камера и AI» или автоматически при старте).
При первом запуске батник создаёт окружение Python в %LOCALAPPDATA%\FitQuest\venv и ставит
пакет mediapipe (нужен интернет, около 200 МБ). Модель pose_landmarker_lite.task (около 6 МБ)
скачивается с официального сервера Google в %LOCALAPPDATA%\FitQuest\models.

Нужен Python 3 (python.org, при установке отметить "Add python.exe to PATH").

Проверка без игры:
  python pose_server.py --no-heartbeat          — камера 0, кадры уходят на 127.0.0.1:50510
  python pose_server.py --image фото.jpg        — распознать позу на фотографии

Модель: MediaPipe Pose Landmarker (Google), лицензия Apache 2.0.
