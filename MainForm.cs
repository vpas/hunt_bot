namespace hunt_bot {
    public partial class MainForm : Form {
        private BotRunner botRunner;

        public MainForm(BotRunner botRunner) {
            InitializeComponent();
            this.botRunner = botRunner;
        }

        private void startButton_Click(object sender, EventArgs e) {
            botRunner.Start();
        }

        private void Stop_Click(object sender, EventArgs e) {
            botRunner.Stop();
        }
    }
}