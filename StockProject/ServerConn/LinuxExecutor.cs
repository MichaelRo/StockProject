using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace StockProject
{
    class LinuxExecutor : IExecutor
    {
        private SshClient sshConn;
        private SftpClient sftpConn;
        private ScpClient scpClient;

        public LinuxExecutor(SshClient ssh, SftpClient sftp, ScpClient scp)
        {
            sshConn = ssh;
            sftpConn = sftp;
            scpClient = scp;
        }

        public bool execute(ControlOptions option, List<string> args)
        {
            if (sshConn == null) return false;

            var isSuccess = false;

            sshConn.Connect();
            sftpConn.Connect();
            scpClient.Connect();

            switch (option)
            {
                case (ControlOptions.MAKE_DIR):
                    {
                        isSuccess = makeDir(args[0]);
                        break;
                    }

                case (ControlOptions.REMOVE_DIR):
                    {
                        isSuccess = makeDir(args[0]);
                        break;
                    }

                case (ControlOptions.COPY_FILE_LINUX_TO_WINDOWS):
                    {
                        isSuccess = copyFileLInuxToWindows(args[0], args[1]);
                        break;
                    }
                case (ControlOptions.COPY_FILE_WINDOWS_TO_LINUX):
                    {
                        isSuccess = copyFileWindowsToLinux(args[0], args[1]);
                        break;
                    }
                default:
                    break;
            }

            sshConn.Disconnect();
            sftpConn.Disconnect();
            scpClient.Disconnect();

            return isSuccess;

        }

        private bool makeDir(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName)) return false;

            sshConn.RunCommand("mkdir " + directoryName);
            return true;
        }

        private bool removeDir(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName)) return false;

            sshConn.RunCommand("rm -r " + directoryName);
            return true;
        }

        private bool copyFileLInuxToWindows(string pathOnLinux, string pathOnWindows)
        {
            if (string.IsNullOrEmpty(pathOnWindows) || string.IsNullOrEmpty(pathOnLinux))
                return false;
            
            using (var file = File.OpenWrite(pathOnWindows))
            {
                sftpConn.DownloadFile(pathOnLinux, file);
            }
            return true;
        }

        private bool copyFileWindowsToLinux(string pathOnWindows, string pathOnLinux)
        {
            if (string.IsNullOrEmpty(pathOnWindows) || string.IsNullOrEmpty(pathOnLinux))
                return false;

            using (var file = File.OpenRead(pathOnWindows))
            {
                sftpConn.UploadFile(file, pathOnLinux);
            }
            return true;
        }

        

        public void executeCommand(string command)
        {
            sshConn.Connect();
            sshConn.RunCommand(command);
            sshConn.Disconnect();  
        }
    }
}
