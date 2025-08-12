namespace Safari
{
    internal static class Program
    {
        /// <summary>
        /// Entry point of the Safari simulation application.
        /// Initializes application configuration settings and launches the main GUI form.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initializes application-wide settings
            ApplicationConfiguration.Initialize();
            // Launches the main application window (Form1) and starts the message loop.
            Application.Run(new Form1());
        }
    }
}