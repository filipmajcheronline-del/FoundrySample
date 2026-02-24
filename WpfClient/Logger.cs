using System;
using System.IO;
using System.Threading.Tasks;

namespace WpfClient
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        public static void Log(string text, long maxBytes = 5 * 1024 * 1024)
        {
            try
            {
                var logDir = Path.Combine(Directory.GetCurrentDirectory(), "LOG");
                Directory.CreateDirectory(logDir);
                var path = Path.Combine(logDir, "wpfclient.log");
                lock (_lock)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            var fi = new FileInfo(path);
                            if (fi.Length > maxBytes)
                            {
                                var dest = path + ".1";
                                if (File.Exists(dest)) File.Delete(dest);
                                File.Move(path, dest);
                            }
                        }
                        catch { }
                    }
                    File.AppendAllText(path, $"[{DateTime.UtcNow:O}] {text}{Environment.NewLine}");
                }
            }
            catch { }
        }
    }
}
