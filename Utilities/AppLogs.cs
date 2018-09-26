using System;
using System.Configuration;
using System.IO;

namespace Utilities
{
    public class AppLogs
    {
        string filePath = String.Empty;
        static String rootFolder = ConfigurationManager.AppSettings["LogFolder"];
        String fileLog = String.Empty;

        private void LogFolderExist()
        {
            if (!System.IO.Directory.Exists(rootFolder))
                System.IO.Directory.CreateDirectory(rootFolder);
        }

        public void CreateLogFile(String CustomName)
        {
            LogFolderExist();

            fileLog = System.IO.Path.Combine(rootFolder, CustomName + ".txt");
            if (!System.IO.File.Exists(fileLog))
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(fileLog);
                file.Close();
            }
        }

        public void WriteOnLogFile(String text)
        {
            using (StreamWriter sw = File.AppendText(fileLog))
                sw.WriteLine(text);
        }

        public void WriteOnLogFile(String text, DateTime CustomDate)
        {
            using (StreamWriter sw = File.AppendText(fileLog))
                sw.WriteLine(String.Format("[{0}] - {1}", CustomDate.ToString(), text));
        }
    }
}
