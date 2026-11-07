using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FitQuest
{
    /// <summary>
    /// Все клавиши игры в одном месте.
    /// Работает и с новой системой ввода Unity (Input System), и со старой.
    /// </summary>
    public static class GameKeys
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K { get { return Keyboard.current; } }
        static bool Down(Key key) { return K != null && K[key].wasPressedThisFrame; }
        static bool Held(Key key) { return K != null && K[key].isPressed; }

        public static bool Jump { get { return Down(Key.Space) || Down(Key.UpArrow); } }
        public static bool Squat { get { return Down(Key.S) || Down(Key.DownArrow); } }
        public static bool Punch { get { return Down(Key.D) || Down(Key.RightArrow); } }
        public static bool HandsUp { get { return Down(Key.W) || Down(Key.LeftArrow); } }
        public static bool Confirm { get { return Down(Key.Enter) || Down(Key.NumpadEnter); } }
        public static bool Pause { get { return Down(Key.Escape) || Down(Key.P); } }
        public static bool DebugPanel { get { return Down(Key.F1); } }
        public static bool SloppyHeld { get { return Held(Key.LeftShift) || Held(Key.RightShift); } }
        public static bool WeakHeld { get { return Held(Key.LeftCtrl) || Held(Key.RightCtrl); } }

        // Ритм-режим: стрелки или WASD. Дорожки: 0 — влево, 1 — вниз, 2 — вверх, 3 — вправо.
        static readonly Key[] LaneArrow = { Key.LeftArrow, Key.DownArrow, Key.UpArrow, Key.RightArrow };
        static readonly Key[] LaneLetter = { Key.A, Key.S, Key.W, Key.D };
        public static bool LanePressed(int lane) { return Down(LaneArrow[lane]) || Down(LaneLetter[lane]); }
        public static bool LaneHeld(int lane) { return Held(LaneArrow[lane]) || Held(LaneLetter[lane]); }
#else
        public static bool Jump { get { return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow); } }
        public static bool Squat { get { return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow); } }
        public static bool Punch { get { return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow); } }
        public static bool HandsUp { get { return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.LeftArrow); } }
        public static bool Confirm { get { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); } }
        public static bool Pause { get { return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P); } }
        public static bool DebugPanel { get { return Input.GetKeyDown(KeyCode.F1); } }
        public static bool SloppyHeld { get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); } }
        public static bool WeakHeld { get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); } }

        static readonly KeyCode[] LaneArrow = { KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.RightArrow };
        static readonly KeyCode[] LaneLetter = { KeyCode.A, KeyCode.S, KeyCode.W, KeyCode.D };
        public static bool LanePressed(int lane) { return Input.GetKeyDown(LaneArrow[lane]) || Input.GetKeyDown(LaneLetter[lane]); }
        public static bool LaneHeld(int lane) { return Input.GetKey(LaneArrow[lane]) || Input.GetKey(LaneLetter[lane]); }
#endif
    }
}
