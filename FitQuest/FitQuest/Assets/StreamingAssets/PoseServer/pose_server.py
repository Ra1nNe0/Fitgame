"""
FitQuest Pose Server.

Берёт кадры с веб-камеры, находит позу человека нейросетью MediaPipe Pose Landmarker
(33 точки тела) и отправляет точки в игру по UDP на 127.0.0.1.

Протокол — одна текстовая строка на кадр (UTF-8), поля разделены "|":
  FQ1|pose|<номер>|<fps>|<ширина>|<высота>|<33 x (x,y,видимость)>|<33 x (x,y,z) в метрах>|<jpeg base64 или пусто>
  FQ1|none|<номер>|<fps>|<ширина>|<высота>|||<jpeg base64 или пусто>
  FQ1|status|<сообщение>
Игра раз в секунду присылает "ping" на порт команд; без ping сервер сам завершается.

Запуск вручную:  python pose_server.py            (камера 0)
Проверка на фото: python pose_server.py --image photo.jpg
"""
import argparse
import base64
import os
import socket
import sys
import threading
import time
import urllib.request

MODEL_NAME = "pose_landmarker_lite.task"
MODEL_URL = ("https://storage.googleapis.com/mediapipe-models/pose_landmarker/"
             "pose_landmarker_lite/float16/latest/pose_landmarker_lite.task")
GAME_PORT = 50510      # сюда сервер шлёт кадры
COMMAND_PORT = 50511   # здесь сервер слушает ping/quit
PREVIEW_EVERY = 2      # картинка-превью в каждом втором кадре
PREVIEW_SIZE = (192, 144)


def log(message):
    print("[FitQuest] " + message, flush=True)


def data_dir():
    base = os.environ.get("LOCALAPPDATA") or os.path.join(os.path.expanduser("~"), ".local", "share")
    path = os.path.join(base, "FitQuest")
    os.makedirs(path, exist_ok=True)
    return path


def find_model(explicit):
    """Ищет файл модели; если его нет — скачивает с официального сервера Google."""
    here = [os.path.dirname(os.path.abspath(__file__))]
    if getattr(sys, "frozen", False):          # собранный pose_server.exe: модель лежит рядом с ним
        here = [os.path.dirname(sys.executable), getattr(sys, "_MEIPASS", here[0])] + here
    candidates = ([explicit] + [os.path.join(folder, MODEL_NAME) for folder in here]
                  + [os.path.join(data_dir(), "models", MODEL_NAME)])
    for path in candidates:
        if path and os.path.isfile(path) and os.path.getsize(path) > 1_000_000:
            return path

    target = candidates[-1]
    os.makedirs(os.path.dirname(target), exist_ok=True)
    log("Скачиваю модель позы (около 6 МБ)...")
    tmp = target + ".part"
    urllib.request.urlretrieve(MODEL_URL, tmp)
    os.replace(tmp, target)
    log("Модель сохранена: " + target)
    return target


class PoseEngine:
    """Обёртка над MediaPipe Pose Landmarker."""

    def __init__(self, model_path, video=True):
        import mediapipe as mp
        from mediapipe.tasks import python as mp_tasks
        from mediapipe.tasks.python import vision

        self.mp = mp
        options = vision.PoseLandmarkerOptions(
            base_options=mp_tasks.BaseOptions(model_asset_path=model_path),
            running_mode=vision.RunningMode.VIDEO if video else vision.RunningMode.IMAGE,
            num_poses=1,
            min_pose_detection_confidence=0.5,
            min_pose_presence_confidence=0.5,
            min_tracking_confidence=0.5)
        self.landmarker = vision.PoseLandmarker.create_from_options(options)
        self.video = video
        self.last_ts = -1

    def detect(self, rgb, timestamp_ms=None):
        """Возвращает (точки на картинке, точки в метрах) или None, если человека нет."""
        image = self.mp.Image(image_format=self.mp.ImageFormat.SRGB, data=rgb)
        if self.video:
            ts = int(timestamp_ms)
            if ts <= self.last_ts:
                ts = self.last_ts + 1
            self.last_ts = ts
            result = self.landmarker.detect_for_video(image, ts)
        else:
            result = self.landmarker.detect(image)

        if not result.pose_landmarks:
            return None
        image_points = [(p.x, p.y, p.visibility if p.visibility is not None else 1.0)
                        for p in result.pose_landmarks[0]]
        world_points = [(p.x, p.y, p.z) for p in result.pose_world_landmarks[0]]
        return image_points, world_points

    def close(self):
        self.landmarker.close()


def format_numbers(points):
    return ",".join("%.4f,%.4f,%.4f" % p for p in points)


def build_packet(seq, fps, width, height, pose, preview_b64):
    if pose is None:
        return "FQ1|none|%d|%.1f|%d|%d|||%s" % (seq, fps, width, height, preview_b64)
    image_points, world_points = pose
    return "FQ1|pose|%d|%.1f|%d|%d|%s|%s|%s" % (
        seq, fps, width, height, format_numbers(image_points), format_numbers(world_points), preview_b64)


def encode_preview(cv2, bgr):
    small = cv2.resize(bgr, PREVIEW_SIZE, interpolation=cv2.INTER_AREA)
    ok, jpeg = cv2.imencode(".jpg", small, [cv2.IMWRITE_JPEG_QUALITY, 60])
    return base64.b64encode(jpeg.tobytes()).decode("ascii") if ok else ""


class CommandListener(threading.Thread):
    """Принимает ping/quit от игры. Если игра пропала — сервер завершается."""

    def __init__(self, port):
        super().__init__(daemon=True)
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(("127.0.0.1", port))
        self.sock.settimeout(0.5)
        self.started = time.time()
        self.last_ping = None
        self.quit = False

    def run(self):
        while not self.quit:
            try:
                data, _ = self.sock.recvfrom(1024)
            except socket.timeout:
                continue
            except OSError:
                break
            text = data.decode("utf-8", "ignore").strip()
            if text == "ping":
                self.last_ping = time.time()
            elif text == "quit":
                self.quit = True

    def should_stop(self):
        if self.quit:
            return True
        now = time.time()
        if self.last_ping is None:
            return now - self.started > 180     # игра так и не подключилась
        return now - self.last_ping > 10        # игра закрыта


def open_camera(cv2, index):
    if isinstance(index, str):                 # видеофайл вместо камеры (для проверки)
        cap = cv2.VideoCapture(index)
        return cap if cap.isOpened() else None
    backends = [cv2.CAP_DSHOW, cv2.CAP_MSMF, cv2.CAP_ANY] if sys.platform == "win32" else [cv2.CAP_ANY]
    for backend in backends:
        cap = cv2.VideoCapture(index, backend)
        if cap is not None and cap.isOpened():
            cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
            cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
            cap.set(cv2.CAP_PROP_FPS, 30)
            ok, _ = cap.read()
            if ok:
                return cap
            cap.release()
    return None


def run_camera(args):
    import cv2

    out = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    target = ("127.0.0.1", args.port)

    def status(message):
        log(message)
        out.sendto(("FQ1|status|" + message).encode("utf-8"), target)

    try:
        listener = CommandListener(args.command_port)
    except OSError:
        log("Сервер уже запущен (порт %d занят). Закрываю это окно." % args.command_port)
        return 0
    listener.start()

    status("Загружаю нейросеть...")
    try:
        engine = PoseEngine(find_model(args.model), video=True)
    except Exception as error:  # нет интернета, повреждён файл и т.п.
        status("Ошибка модели: %s" % error)
        time.sleep(5)
        return 1

    status("Открываю камеру %s..." % args.camera)
    cap = open_camera(cv2, args.camera)
    if cap is None:
        status("Камера не найдена. Проверьте подключение и закройте другие программы с камерой.")
        time.sleep(5)
        return 1
    status("Камера работает")

    seq = 0
    fps = 0.0
    last = time.time()
    t0 = time.monotonic()
    try:
        while not (args.heartbeat and listener.should_stop()):
            ok, frame = cap.read()
            if not ok and isinstance(args.camera, str):
                cap.set(cv2.CAP_PROP_POS_FRAMES, 0)   # видеофайл закончился — по кругу
                continue
            if not ok:
                status("Камера перестала отвечать")
                time.sleep(0.5)
                continue
            frame = cv2.flip(frame, 1)                       # зеркально, как в зеркале
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            pose = engine.detect(rgb, (time.monotonic() - t0) * 1000.0)

            now = time.time()
            dt = max(now - last, 1e-3)
            last = now
            fps = fps * 0.9 + (1.0 / dt) * 0.1 if fps > 0 else 1.0 / dt

            preview = encode_preview(cv2, frame) if args.preview and seq % PREVIEW_EVERY == 0 else ""
            h, w = frame.shape[:2]
            out.sendto(build_packet(seq, fps, w, h, pose, preview).encode("ascii"), target)
            seq += 1
            if seq % 300 == 0:
                log("кадров: %d, FPS: %.1f, человек в кадре: %s" % (seq, fps, "да" if pose else "нет"))
    except KeyboardInterrupt:
        pass
    finally:
        cap.release()
        engine.close()
        log("Сервер остановлен")
    return 0


def run_image(args):
    """Режим проверки: одна фотография -> пакет протокола в консоль."""
    import cv2
    bgr = cv2.imread(args.image)
    if bgr is None:
        log("Не удалось открыть " + args.image)
        return 1
    engine = PoseEngine(find_model(args.model), video=False)
    pose = engine.detect(cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB))
    h, w = bgr.shape[:2]
    print(build_packet(0, 0.0, w, h, pose, ""))
    engine.close()
    return 0 if pose else 2


def main():
    parser = argparse.ArgumentParser(description="FitQuest Pose Server")
    parser.add_argument("--camera", default="0", help="номер камеры или путь к видеофайлу")
    parser.add_argument("--port", type=int, default=GAME_PORT)
    parser.add_argument("--command-port", type=int, default=COMMAND_PORT)
    parser.add_argument("--model", default=None, help="путь к pose_landmarker_lite.task")
    parser.add_argument("--no-preview", dest="preview", action="store_false")
    parser.add_argument("--no-heartbeat", dest="heartbeat", action="store_false",
                        help="не завершаться без игры (для отладки)")
    parser.add_argument("--image", default=None, help="проверить одну фотографию")
    args = parser.parse_args()
    if args.camera.isdigit():
        args.camera = int(args.camera)
    return run_image(args) if args.image else run_camera(args)


if __name__ == "__main__":
    sys.exit(main())
