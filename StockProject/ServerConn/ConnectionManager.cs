using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace StockProject
{
    class ConnectionManager
    {
        private IExecutor executer;
        private SshClient sshConn;
        private ScpClient scpClient;
        private SftpClient sftpConn;

        public bool createConnection()
        {
            if (!tryInitConnection())
                return false;

            executer = new LinuxExecutor(sshConn, sftpConn, scpClient);
            return true;
        }

        public void InitServerFS()
        {

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            var ServerDirectoryName = ConfigurationManager.AppSettings["ServerDirectoryName"];
            var ServerJavaDirectoryName = ConfigurationManager.AppSettings["ServerJavaDirectoryName"];
            var ServerClassesDirectoryName = ConfigurationManager.AppSettings["ServerClassesDirectoryName"];
            var ServerStocksDirectoryName = ConfigurationManager.AppSettings["ServerStocksDirectoryName"];

            // Prepare the server for the run, Delete folders and recreate them
            string[] initVMCommands = new string[]
                    {
                        "rm -r stocksProj",
                        "mkdir stocksProj",
                        "mkdir stocksProj/Stocks",
                        "mkdir stocksProj/Java",
                        "mkdir stocksProj/Java/Classes"
                    };

            executer.execute(ControlOptions.REMOVE_DIR,new List<string>() { ServerDirectoryName });
            executer.execute(ControlOptions.MAKE_DIR, new List<string>() { ServerDirectoryName });
            executer.execute(ControlOptions.MAKE_DIR, new List<string>() { ServerJavaDirectoryName });
            executer.execute(ControlOptions.MAKE_DIR, new List<string>() { ServerClassesDirectoryName });
            executer.execute(ControlOptions.MAKE_DIR, new List<string>() { ServerStocksDirectoryName });
        }

        public void executeCommand(string command)
        {
            executer.executeCommand(command);
        }

        public void CopyWindowsToLinux(string windowsPath, string linuxPath)
        {
            executer.execute(ControlOptions.COPY_FILE_WINDOWS_TO_LINUX, new List<string>()
            {
              windowsPath ,
              linuxPath
            });
        }

        public void CopyLinuxToWindows(string linuxPath, string windowsPath)
        {
            executer.execute(ControlOptions.COPY_FILE_LINUX_TO_WINDOWS, new List<string>()
            {
              linuxPath ,
              windowsPath
            });
        }

        private bool tryInitConnection()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            var ip = ConfigurationManager.AppSettings["HostIP"];
            var userName = ConfigurationManager.AppSettings["HostPass"];
            var pass = ConfigurationManager.AppSettings["UserName"];

            try {
                sshConn = new SshClient(ip, userName, pass);

                sshConn.Connect();
                sshConn.Disconnect();
            } 
            catch
            {
                return false;
            }
            try
            {
                scpClient = new ScpClient(ip, userName, pass);

                scpClient.Connect();
                scpClient.Disconnect();
            }
            catch
            {
                return false;
            }
           
            try
            {
                sftpConn = new SftpClient(ip, userName, pass);

                sftpConn.Connect();
                sftpConn.Disconnect();


                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
