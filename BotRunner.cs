using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace hunt_bot {
    public class BotRunner {
        const string HUNT_WINDOW_TITLE = "Hunt: Showdown";

        private bool isRunning = false;
        private Random random = new Random();

        private Bitmap referenceRegionBitmap;
        private Rectangle screenRegionRect;
        private readonly int checkIntervalMillisec;
        private readonly int maxNormalizedPerPixelDiff;

        public BotRunner(
            int checkIntervalMillisec = 1000,
            int maxNormalizedPerPixelDiff = 20) {
            this.checkIntervalMillisec = checkIntervalMillisec;
            this.maxNormalizedPerPixelDiff = maxNormalizedPerPixelDiff;

            LoadReferenceRegion();

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

        private void LoadReferenceRegion() {
            var screenHeight = Screen.PrimaryScreen.Bounds.Height;
            var screenWidth = Screen.PrimaryScreen.Bounds.Width;
            if (screenHeight == 1200 && screenWidth == 1920) {
                referenceRegionBitmap = Properties.Resources.revive_button_1920_1200;
                screenRegionRect = new Rectangle(
                    location: new Point(
                        int.Parse(Properties.Resources.revive_button_1920_1200_left_x),
                        int.Parse(Properties.Resources.revive_button_1920_1200_top_y)
                    ),
                    size: referenceRegionBitmap.Size
                );
                //} else if (screenHeight == 1440) {
                //    referenceRegionBitmap = Properties.Resources.hunt_death_reference_region_1440p;
                //    screenRegionRect = new Rectangle(
                //        location: new Point(
                //            int.Parse(Properties.Resources.reference_region_1440p_left_x),
                //            int.Parse(Properties.Resources.reference_region_1440p_top_y)
                //        ),
                //        size: referenceRegionBitmap.Size
                //    );
                //} else if (screenHeight == 1200) {
                //    referenceRegionBitmap = Properties.Resources.hunt_death_reference_region_1200p;
                //    screenRegionRect = new Rectangle(
                //        location: new Point(
                //            int.Parse(Properties.Resources.reference_region_1200p_left_x),
                //            int.Parse(Properties.Resources.reference_region_1200p_top_y)
                //        ),
                //        size: referenceRegionBitmap.Size
                //    );
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
                        Thread.Sleep(checkIntervalMillisec);
                        continue;
                    }

                    var capturedScreenRegionBitmap = CaptureScreenRegion(screenRegionRect);
                    capturedScreenRegionBitmap = NormalizeBitmap(capturedScreenRegionBitmap);
                    var diff = CalcBitmapDiff(referenceRegionBitmap, capturedScreenRegionBitmap);
                    Console.WriteLine($"RunDetector diff: {diff}");
                    var isPlayerDead = diff <= maxNormalizedPerPixelDiff;
                    Console.WriteLine($"RunDetector newIsPlayerDead: {isPlayerDead}");

                    if (isPlayerDead) {
                        var reviveButtonCenterX = screenRegionRect.X + screenRegionRect.Width / 2;
                        var reviveButtonCenterY = screenRegionRect.Y + screenRegionRect.Height / 2;
                        InputSimulator.SetCursorPos(reviveButtonCenterX, reviveButtonCenterY);
                        Thread.Sleep(100);
                        InputSimulator.mouse_event((int)InputSimulator.MouseEventFlags.LeftDown, reviveButtonCenterX, reviveButtonCenterY, 0, 0);
                        Thread.Sleep(100);
                        InputSimulator.mouse_event((int)InputSimulator.MouseEventFlags.LeftUp, reviveButtonCenterX, reviveButtonCenterY, 0, 0);
                        Thread.Sleep(1000);
                    } else {
                        StrafeRandomly(1000);
                    }
                } else {
                    Thread.Sleep(checkIntervalMillisec);
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
        
        private void StrafeRandomly(int totalDurationMs, int minDurationMs = 300, int maxDurationMs = 1000) {
            var curDurationMs = 0;
            int curKey = 0;
            if (random.NextSingle() < 0.5) {
                curKey = InputSimulator.SCAN_CODE_Q;
            } else {
                curKey = InputSimulator.SCAN_CODE_D;
            }
            while (curDurationMs < totalDurationMs) {
                if (curKey == InputSimulator.SCAN_CODE_D) {
                    curKey = InputSimulator.SCAN_CODE_Q;
                } else {
                    curKey = InputSimulator.SCAN_CODE_D;
                }
                var d = random.Next(minDurationMs, maxDurationMs);
                d = Math.Min(d, totalDurationMs - curDurationMs);
                curDurationMs += d;
                InputSimulator.HoldKey(curKey, d);
            }
        }
    }
}
