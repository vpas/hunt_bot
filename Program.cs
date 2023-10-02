using System.Diagnostics;

namespace hunt_bot {
    internal static class Program {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (Process p = Process.GetCurrentProcess()) {
                p.PriorityClass = ProcessPriorityClass.BelowNormal;
            }

            SetProcessDPIAware();

            var botRunner = new BotRunner();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(botRunner));
        }
    }
}