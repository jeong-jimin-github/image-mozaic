using System;
using System.IO;
using System.Windows.Forms;

namespace ImageMosaicEditor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                string logPath = WriteCrashLog(ex);
                try
                {
                    MessageBox.Show(
                        L10n.F("프로그램 시작 중 오류가 발생했습니다.\n\n{0}\n\n오류 로그:\n{1}", ex.Message, logPath),
                        L10n.T("ImageMosaicEditor 시작 오류"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // If WinForms itself cannot show a dialog, the crash log still remains.
                }
            }
        }

        private static string WriteCrashLog(Exception ex)
        {
            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ImageMosaicEditor");
                Directory.CreateDirectory(directory);

                string path = Path.Combine(directory, "crash.log");
                File.WriteAllText(path,
                    $"Time: {DateTimeOffset.Now:O}{Environment.NewLine}" +
                    $"Version: {Application.ProductVersion}{Environment.NewLine}" +
                    $"BaseDirectory: {AppContext.BaseDirectory}{Environment.NewLine}" +
                    $"OS: {Environment.OSVersion}{Environment.NewLine}" +
                    $"64-bit process: {Environment.Is64BitProcess}{Environment.NewLine}{Environment.NewLine}" +
                    ex);
                return path;
            }
            catch
            {
                return L10n.T("오류 로그를 저장하지 못했습니다.");
            }
        }
    }
}
