using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace hunt_bot {
    public class InputSimulator {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        private static Dictionary<string, int> scanCodes = new Dictionary<string, int>() {
            {"ESC", 1 },
            {"1", 2 },
            {"2", 3 },
            {"3", 4 },
            {"4", 5 },
            {"5", 6 },
            {"6", 7 },
            {"7", 8 },
            {"8", 9 },
            {"9", 10},
            {"0", 11},
            {"-", 12},
            {"=", 13},
            {"bs", 14},
            {"Tab", 15},
            {"Q", 16},
            {"W", 17},
            {"E", 18},
            {"R", 19},
            {"T", 20},
            {"Y", 21},
            {"U", 22},
            {"I", 23},
            {"O", 24},
            {"P", 25},
            {"[", 26},
            {"]", 27},
            {"Enter", 28},
            {"CTRL", 29},
            {"A", 30},
            {"S", 31},
            {"D", 32},
            {"F", 33},
            {"G", 34},
            {"H", 35},
            {"J", 36},
            {"K", 37},
            {"L", 38},
            {";", 39},
            {"'", 40},
            {"`", 41},
            {"LShift", 42},
            {"Z", 44},
            {"X", 45},
            {"C", 46},
            {"V", 47},
            {"B", 48},
            {"N", 49},
            {"M", 50},
            {",", 51},
            {".", 52},
            {"/", 53},
            {"RShift", 54},
            {"PrtSc", 55},
            {"Alt", 56},
            {"Space", 57},
            {"Caps", 58},
            {"F1", 59},
            {"F2", 60},
            {"F3", 61},
            {"F4", 62},
            {"F5", 63},
            {"F6", 64},
            {"F7", 65},
            {"F8", 66},
            {"F9", 67},
            {"F10", 68},
            {"Num", 69},
            {"Scroll", 70},
            {"Home (7)", 71},
            {"Up (8)", 72},
            {"PgUp (9)", 73},
            {"Left (4)", 75},
            {"Center (5)", 76},
            {"Right (6)", 77},
            {"+", 78},
            {"End (1)", 79},
            {"Down (2)", 80},
            {"PgDn (3)", 81},
            {"Ins", 82},
            {"Del", 83},
        };

        [Flags]
        public enum MouseEventFlags {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const int VK_Q = 0x51;
        public const int VK_D = 0x44;

        public const int SCAN_CODE_Q = 0x10;
        public const int SCAN_CODE_D = 0x20;


        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion {
            [FieldOffset(0)] public MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public HardwareInput hi;
        }

        public struct Input {
            public int type;
            public InputUnion u;
        }

        [Flags]
        public enum InputType {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008
        }

        [Flags]
        public enum MouseEventF {
            Absolute = 0x8000,
            HWheel = 0x01000,
            Move = 0x0001,
            MoveNoCoalesce = 0x2000,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            VirtualDesk = 0x4000,
            Wheel = 0x0800,
            XDown = 0x0080,
            XUp = 0x0100
        }

        public static void SendKeyboardMsg(UInt32 eventCode, int key) {
            var handle = GetForegroundWindow();
            PostMessage(handle, eventCode, key, 0);
        }

        public static void SetKeyState(string key, bool isDown) {
            KeyEventF keyEventF = isDown ? KeyEventF.KeyDown : KeyEventF.KeyUp;
            Input[] inputs = new Input[] {
                new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = 0,
                            wScan = (ushort)scanCodes[key],
                            dwFlags = (uint)(keyEventF | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        public static void MouseMove(int dx, int dy) {
            Input[] inputs = new Input[]
            {
                new Input
                {
                    type = (int) InputType.Mouse,
                    u = new InputUnion
                    {
                        mi = new MouseInput
                        {
                            dx = dx,
                            dy = dy,
                            dwFlags = (uint)(MouseEventF.Move),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        public static void HoldKey(string key, int durationMs) {
            Console.WriteLine($"Holding {key} for {durationMs}ms");
            SetKeyState(key: key, isDown: true);
            Thread.Sleep(durationMs);
            SetKeyState(key: key, isDown: false);
        }

        public static void HoldKeys(string[] keys, int durationMs) {
            Console.WriteLine($"Holding {String.Join(',', keys)} for {durationMs}ms");
            foreach (string key in keys) {
                SetKeyState(key: key, isDown: true);
            }
            Thread.Sleep(durationMs);
            foreach (string key in keys) {
                SetKeyState(key: key, isDown: false);
            }
        }
    }
}
