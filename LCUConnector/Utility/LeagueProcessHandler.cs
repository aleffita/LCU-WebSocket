using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LCUConnector.Utility
{
    internal class LeagueProcessHandler
    {
        private const string ProcessName = "LeagueClientUx";
        private Process Process { get; set; }
        public Action<string> ExecutablePath { get; set; }
        
        public void PoolPath()
        {
            while (true)
            {
                var processes = Process.GetProcessesByName(ProcessName);
                if (processes.Length > 0)
                {
                    Process = processes[0];

                    if (Process.MainModule != null)
                    {
                        var path = Path.GetDirectoryName(Process.MainModule.FileName);
                        ExecutablePath?.Invoke(path);
                        break;
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}