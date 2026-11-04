using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Kuaff.Tractor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string appDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            if (!string.IsNullOrEmpty(appDirectory))
            {
                Environment.CurrentDirectory = appDirectory;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            Application.ThreadException += new ThreadExceptionEventHandler(OnThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);

            MainForm mainForm = new MainForm();
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-fullscreen") >= 0)
            {
                mainForm.Shown += new EventHandler(delegate { mainForm.ToggleFullScreen(); });
            }
            if (Array.IndexOf(args, "-autoplay") >= 0)
            {
                mainForm.Shown += new EventHandler(delegate
                {
                    mainForm.gameConfig.IsDebug = true;
                    mainForm.isNew = true;
                    mainForm.init();
                    mainForm.timer.Start();
                });
            }
            Application.Run(mainForm);
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            WriteLog(e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteLog(e.ExceptionObject as Exception);
        }

        private static void WriteLog(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter("error.log", true, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]");
                    sw.WriteLine(ex.ToString());
                    sw.WriteLine();
                }
            }
            catch
            {
            }
        }
    }
}