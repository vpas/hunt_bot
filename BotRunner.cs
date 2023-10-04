using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hunt_bot {
    public class BotRunner {
        [Serializable]
        public class Config {
            public enum BotMode {
                STRAFE_RANDOMLY,
                STRAFE_PREDICTABLY,
                SPRINT_PREDICTABLY,
            }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public enum Action {
                LEFT,
                RIGHT,
                FORWARD,
                SPRINT,
            }

            public Config() { }

            public int CheckIntervalMs { get; set; } = 1000;

            public Dictionary<Action, string> keyMappings { get; set; } = new() {
                { Action.LEFT, "Q" },
                { Action.RIGHT, "D" },
                { Action.FORWARD, "W" },
                { Action.SPRINT, "Tab" },
            };
            public float mouseSensitivity { get; set; } = 0.28442f;

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public BotMode Mode { get; set; } = BotMode.STRAFE_RANDOMLY;
            
            public int StrafeRandomlyMinDurationMs { get; set; } = 300;
            public int StrafeRandomlyMaxDurationMs { get; set; } = 1000;
            
            public int StrafePredictablyDurationMs { get; set; }
            
            public int SpringPredictablyDurationMs { get; set; }
        }

        const string HUNT_WINDOW_TITLE = "Hunt: Showdown";
        const string CONFIG_FILENAME = "config.json";

        private bool isRunning = false;
        private Random random = new Random();

        private Bitmap referenceRegionBitmap;
        private Rectangle screenRegionRect;
        private Rectangle reviveButtonRect;
        private readonly int maxNormalizedPerPixelDiff;
        private Config config;
        private const float SENS_MULT = 9308.85874f;
        private float turn360Dx;

        public BotRunner(
            int maxNormalizedPerPixelDiff = 30) {
            this.maxNormalizedPerPixelDiff = maxNormalizedPerPixelDiff;

            LoadReferenceRegion();
            this.config = LoadConfig();
            turn360Dx = SENS_MULT / config.mouseSensitivity;
            Console.WriteLine($"turn360Dx: {turn360Dx}");

            var botRunnerThread = new Thread(new ThreadStart(this.RunBot)) {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            botRunnerThread.Start();
        }

        public void Start() {
            isRunning = true;
        }

        public void Stop() {
            isRunning = false;
        }

        private Config LoadConfig() {
            if (File.Exists(CONFIG_FILENAME)) {
                var configText = File.ReadAllText(CONFIG_FILENAME);
                var config = JsonSerializer.Deserialize<Config>(configText);
                SaveConfig(config);
                return config;
            } else {
                var config = new Config();
                SaveConfig(config);
                return config;
            }
        }

        private static void SaveConfig(Config config) {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonText = JsonSerializer.Serialize(config, options);
            File.WriteAllText(CONFIG_FILENAME, jsonText);
        }

        
        private void LoadReferenceRegion() {
            var screenHeight = Screen.PrimaryScreen.Bounds.Height;
            var screenWidth = Screen.PrimaryScreen.Bounds.Width;
            if (screenHeight == 1200 && screenWidth == 1920) {
                referenceRegionBitmap = Properties.Resources.death_screen_1920_1200_ref_region;
                screenRegionRect = new Rectangle(
                    location: new Point(
                        int.Parse(Properties.Resources.death_screen_ref_region_1920_1200_left_x),
                        int.Parse(Properties.Resources.death_screen_ref_region_1920_1200_top_y)
                    ),
                    size: referenceRegionBitmap.Size
                );
                reviveButtonRect = new Rectangle(
                    location: new Point(
                        int.Parse(Properties.Resources.revive_button_1920_1200_left_x),
                        int.Parse(Properties.Resources.revive_button_1920_1200_top_y)
                    ),
                    size: Properties.Resources.revive_button_1920_1200.Size
                );
            } else if (screenHeight == 1080 && screenWidth == 1920) {
                referenceRegionBitmap = Properties.Resources.death_screen_1920_1080_ref_region;
                screenRegionRect = new Rectangle(
                    location: new Point(
                        int.Parse(Properties.Resources.death_screen_ref_region_1920_1080_left_x),
                        int.Parse(Properties.Resources.death_screen_ref_region_1920_1080_top_y)
                    ),
                    size: referenceRegionBitmap.Size
                );
                reviveButtonRect = new Rectangle(
                    location: new Point(
                        int.Parse(Properties.Resources.revive_button_1920_1080_left_x),
                        int.Parse(Properties.Resources.revive_button_1920_1080_top_y)
                    ),
                    size: Properties.Resources.revive_button_1920_1080.Size
                );
            } else {
                Console.WriteLine($"Unsupported screen Height: {screenHeight}");
            }
        }

        private void RunBot() {
            Console.WriteLine("RunBot");
            while (true) {
                var curWindowTitle = GetCaptionOfActiveWindow();
                Console.WriteLine($"curWindowTitle: {curWindowTitle}");
                if (curWindowTitle == HUNT_WINDOW_TITLE) {
                    if (!isRunning) {
                        Console.WriteLine("isRunning: false");
                        Thread.Sleep(config.CheckIntervalMs);
                        continue;
                    }


                    var capturedScreenRegionBitmap = CaptureScreenRegion(screenRegionRect);
                    capturedScreenRegionBitmap = NormalizeBitmap(capturedScreenRegionBitmap);
                    var diff = CalcBitmapDiff(referenceRegionBitmap, capturedScreenRegionBitmap);
                    Console.WriteLine($"RunDetector diff: {diff}");
                    var isPlayerDead = diff <= maxNormalizedPerPixelDiff;
                    Console.WriteLine($"RunDetector newIsPlayerDead: {isPlayerDead}");
                    

                    if (isPlayerDead) {
                        var reviveButtonCenterX = reviveButtonRect.X + reviveButtonRect.Width / 2;
                        var reviveButtonCenterY = reviveButtonRect.Y + reviveButtonRect.Height / 2;
                        InputSimulator.SetCursorPos(reviveButtonCenterX, reviveButtonCenterY);
                        Thread.Sleep(100);
                        InputSimulator.mouse_event((int)InputSimulator.MouseEventFlags.LeftDown, reviveButtonCenterX, reviveButtonCenterY, 0, 0);
                        Thread.Sleep(100);
                        InputSimulator.mouse_event((int)InputSimulator.MouseEventFlags.LeftUp, reviveButtonCenterX, reviveButtonCenterY, 0, 0);
                        Thread.Sleep(1000);
                    } else {
                        switch(config.Mode) {
                            case Config.BotMode.STRAFE_RANDOMLY: 
                                StrafeRandomly(totalDurationMs: 1000);
                                break;
                            case Config.BotMode.STRAFE_PREDICTABLY:
                                StrafePredictably();
                                break;
                            case Config.BotMode.SPRINT_PREDICTABLY:
                                SprintPredictably();
                                break;
                        }
                        
                    }
                } else {
                    Thread.Sleep(config.CheckIntervalMs);
                }
            }
        }

        private static Bitmap NormalizeBitmap(Bitmap bitmap) {
            //int rSum = 0;
            //int gSum = 0;
            //int bSum = 0;
            //for (int x = 0; x < bitmap.Width; x++) {
            //    for (int y = 0; y < bitmap.Height; y++) {
            //        var p = bitmap.GetPixel(x, y);
            //        rSum += p.R;
            //        gSum += p.G;
            //        bSum += p.B;
            //    }
            //}
            return bitmap;
        }

        private static double CalcBitmapDiff(Bitmap bitmap1, Bitmap bitmap2) {
            int diff_sqr_sum = 0;
            Debug.Assert(bitmap1.Size == bitmap2.Size);
            for (int x = 0; x < bitmap1.Size.Width; x++) {
                for (int y = 0; y < bitmap1.Size.Height; y++) {
                    var p1 = bitmap1.GetPixel(x, y);
                    var p2 = bitmap2.GetPixel(x, y);
                    diff_sqr_sum += Math.Abs(p1.R - p2.R) * Math.Abs(p1.R - p2.R);
                    diff_sqr_sum += Math.Abs(p1.G - p2.G) * Math.Abs(p1.G - p2.G);
                    diff_sqr_sum += Math.Abs(p1.B - p2.B) * Math.Abs(p1.B - p2.B);
                }
            }
            return Math.Sqrt(((double)diff_sqr_sum) / (bitmap1.Size.Width * bitmap1.Size.Height * 3));
        }

        private static Bitmap CaptureScreenRegion(Rectangle rect) {
            Bitmap bmp = new(rect.Width, rect.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        private string GetCaptionOfActiveWindow() {
            var strTitle = string.Empty;
            var handle = GetForegroundWindow();
            // Obtain the length of the text   
            var intLength = GetWindowTextLength(handle) + 1;
            var stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0) {
                strTitle = stringBuilder.ToString();
            }
            return strTitle;
        }
        
        private void StrafeRandomly(int totalDurationMs) {
            var curDurationMs = 0;
            string curKey;
            if (random.NextSingle() < 0.5) {
                curKey = config.keyMappings[Config.Action.LEFT];
            } else {
                curKey = config.keyMappings[Config.Action.RIGHT];
            }
            while (curDurationMs < totalDurationMs) {
                if (curKey == config.keyMappings[Config.Action.LEFT]) {
                    curKey = config.keyMappings[Config.Action.RIGHT];
                } else {
                    curKey = config.keyMappings[Config.Action.LEFT];
                }
                var d = random.Next(config.StrafeRandomlyMinDurationMs, config.StrafeRandomlyMaxDurationMs);
                d = Math.Min(d, totalDurationMs - curDurationMs);
                curDurationMs += d;
                InputSimulator.HoldKey(curKey, d);
            }
        }

        private void StrafePredictably() {
            InputSimulator.HoldKey(config.keyMappings[Config.Action.LEFT], config.StrafePredictablyDurationMs);
            InputSimulator.HoldKey(config.keyMappings[Config.Action.RIGHT], config.StrafePredictablyDurationMs);
        }

        private void SprintPredictably() {
            InputSimulator.HoldKeys(
                new string[] { config.keyMappings[Config.Action.FORWARD], config.keyMappings[Config.Action.SPRINT] },
                config.SpringPredictablyDurationMs
            );
            Thread.Sleep(500);
            Turn(180);
            Thread.Sleep(100);
            InputSimulator.HoldKeys(
                new string[] { config.keyMappings[Config.Action.FORWARD], config.keyMappings[Config.Action.SPRINT] },
                config.SpringPredictablyDurationMs
            );
            Thread.Sleep(500);
            Turn(180);
            Thread.Sleep(100);
        }

        private void Turn(int degrees) {
            var totalDx = (int)(turn360Dx * (degrees / 360f));
            var turnDurationMs = 200;
            var dxPerCycle = 10;
            var numCycles = totalDx / dxPerCycle;
            var sleepPerCycle = turnDurationMs / numCycles;
            while (totalDx > 0) {
                InputSimulator.MouseMove(dx: Math.Min(dxPerCycle, totalDx), dy: 0);
                totalDx -= dxPerCycle;
                Thread.Sleep(sleepPerCycle);
            }
        }
    }
}
