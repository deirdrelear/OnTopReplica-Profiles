using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using OnTopReplica.Properties;
using OnTopReplica.StartupOptions;
using OnTopReplica.ProfileManagement;

namespace OnTopReplica {
    
    static class Program {

        public static PlatformSupport Platform { get; private set; }

        static MainForm _mainForm;

        public static bool IsManagedReplica { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            try {
                AppPaths.SetupPaths();
            }
            catch (Exception ex) {
                MessageBox.Show(string.Format("Unable to setup application folders: {0}", ex), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Log.Write("Launching OnTopReplica Refactor v.{0}", Application.ProductVersion);

            //Hook fatal abort handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            //Initialize and check for platform support
            Platform = PlatformSupport.Create();
            if (!Platform.CheckCompatibility())
                return;
            Platform.PreHandleFormInit();

            Log.Write("Platform support initialized");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Update settings if needed
            if (Settings.Default.MustUpdate) {
                Settings.Default.Upgrade();
                Settings.Default.MustUpdate = false;
            }

            //Profile manager is handled before the normal OTR CLI parser.
            if (ProfileManagerMode.IsRequested(args)) {
                using (var profileManager = new ProfileManagerForm(ProfileManagerMode.GetRequestedProfile(args), ProfileManagerMode.ShouldAutoApply(args))) {
                    Application.Run(profileManager);
                }
                return;
            }

            //Load startup options
            var options = StartupOptions.Factory.CreateOptions(args);
            string optionsMessage = options.DebugMessage;
            if (!string.IsNullOrEmpty(optionsMessage)) { //show dialog if debug message present or if parsing failed
                var dlg = new CommandLineReportForm(options.Status, optionsMessage);
                dlg.ShowDialog();
            }
            if (options.Status == CliStatus.Information || options.Status == CliStatus.Error)
                return;

            IsManagedReplica = options.ManagedReplica;
            
            //Load language
            if (Settings.Default.Language != null &&
                !Settings.Default.Language.Equals(CultureInfo.InvariantCulture)) {
                Thread.CurrentThread.CurrentUICulture = Settings.Default.Language;
            }

            //Show form
            using (_mainForm = new MainForm(options)) {
                if (IsManagedReplica)
                    _mainForm.ShowInTaskbar = false;

                Log.Write("Entering application loop");

                //Enter GUI loop
                Application.Run(_mainForm);

                //Managed profile replicas are disposable view-only workers. They must not
                //race each other writing the shared OnTopReplica user settings file.
                if (IsManagedReplica) {
                    Log.Write("Managed replica ended; skipping settings persistence");
                    return;
                }

                //Re-enable chrome to store correct position (position is stored always WITH chrome: when restoring fails, the position stays ok)
                Settings.Default.RestoreLastShowChrome = _mainForm.IsChromeVisible;
                if (!_mainForm.IsChromeVisible)
                    _mainForm.IsChromeVisible = true;

                //Persist settings
                Log.Write("Last position before shutdown: {0}, size: {1}", _mainForm.Location, _mainForm.Size);
                Settings.Default.RestoreLastPosition = _mainForm.Location;
                Settings.Default.RestoreLastSize = _mainForm.ClientSize;

                //Store last thumbnail, if any
                if (_mainForm.ThumbnailPanel.IsShowingThumbnail && _mainForm.CurrentThumbnailWindowHandle != null) {
                    Settings.Default.RestoreLastWindowTitle = _mainForm.CurrentThumbnailWindowHandle.Title;
                    Settings.Default.RestoreLastWindowHwnd = _mainForm.CurrentThumbnailWindowHandle.Handle.ToInt64();
                    Settings.Default.RestoreLastWindowClass = _mainForm.CurrentThumbnailWindowHandle.Class;
                }
                else {
                    Settings.Default.RestoreLastWindowTitle = string.Empty;
                    Settings.Default.RestoreLastWindowHwnd = 0;
                    Settings.Default.RestoreLastWindowClass = string.Empty;
                }

                Log.Write("Persisting settings");
                Settings.Default.Save();
            }

            Log.Write("Shutting down OnTopReplica Refactor");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            Log.WriteException("Unhandled exception", e.ExceptionObject as Exception);

            string path = AppPaths.GenerateCrashDumpPath();

            using (var s = new FileStream(path, FileMode.Create)) {
                using (var sw = new StreamWriter(s)) {
                    sw.WriteLine("OnTopReplica Refactor Dump file");
                    sw.WriteLine("This file has been created because OnTopReplica Refactor crashed.");
                    sw.WriteLine("Please report this crash on the project's issue tracker.");
                    sw.WriteLine();

                    sw.WriteLine("Last exception:");
                    sw.WriteLine(e.ExceptionObject.ToString());
                    sw.WriteLine();

                    sw.WriteLine("Last log entries:");
                    foreach (var logEntry in Log.Queue) {
                        sw.WriteLine(logEntry);
                    }
                    sw.WriteLine();

                    sw.WriteLine("OnTopReplica Refactor v.{0}", Application.ProductVersion);
                    sw.WriteLine("OS: {0}", Environment.OSVersion.ToString());
                    sw.WriteLine(".NET: {0}", Environment.Version.ToString());
                    sw.WriteLine("DWM: {0}", WindowsFormsAero.OsSupport.IsCompositionEnabled);
                    sw.WriteLine("Launch command: {0}", Environment.CommandLine);
                    sw.WriteLine("UTC time: {0} {1}", DateTime.UtcNow.ToShortDateString(), DateTime.UtcNow.ToShortTimeString());
                }
            }
        }

    }
}
