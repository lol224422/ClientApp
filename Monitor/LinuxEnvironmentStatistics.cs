#define LOG_MEMORY_PERF_COUNTERS

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Monitor
{
    internal class LinuxEnvironmentStatistics 
    {
        private const float KB = 1024f;

        /// <inheritdoc />
        public float? TotalPhysicalMemory { get; private set; }

        /// <inheritdoc />
        public float? CpuUsage { get; private set; }

        /// <inheritdoc />
        public float? AvailableMemory { get; private set; }

        /// <inheritdoc /> 
  //     private long MemoryUsage => GC.GetTotalMemory(false);




        private const string MEMINFO_FILEPATH = "/proc/meminfo";
        private const string CPUSTAT_FILEPATH = "/proc/stat";

        private async Task <object> UpdateAvailableMemory()
        {
            var memAvailableLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemAvailable");

            if (string.IsNullOrWhiteSpace(memAvailableLine))
            {
                memAvailableLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemFree");
                if (string.IsNullOrWhiteSpace(memAvailableLine))
                {
                    Console.WriteLine($"Couldn't read 'MemAvailable' or 'MemFree' line from '{MEMINFO_FILEPATH}'");
                    return null;
                }
            }

            if (!float.TryParse(new string(memAvailableLine.Where(char.IsDigit).ToArray()), out var availableMemInKb))
            {
                Console.WriteLine($"Couldn't parse meminfo output: '{memAvailableLine}'");
                return null;
            }

            AvailableMemory = availableMemInKb;

            return AvailableMemory;
        }

        private async Task UpdateTotalPhysicalMemory()
        {
            var memTotalLine = await ReadLineStartingWithAsync(MEMINFO_FILEPATH, "MemTotal");

            if (string.IsNullOrWhiteSpace(memTotalLine))
            {
                Console.WriteLine($"Couldn't read 'MemTotal' line from '{MEMINFO_FILEPATH}'");
                return;
            }

            // Format: "MemTotal:       16426476 kB"
            if (!float.TryParse(new string(memTotalLine.Where(char.IsDigit).ToArray()), out var totalMemInKb))
            {
                Console.WriteLine($"Couldn't parse meminfo output");
                return;
            }

            TotalPhysicalMemory = totalMemInKb;
        }


        private long _prevIdleTime;
        private long _prevTotalTime;
        private int MONITOR_PERIOD = 2000;

        private async Task UpdateCpuUsage(int i)
        {
            var cpuUsageLine = await ReadLineStartingWithAsync(CPUSTAT_FILEPATH, "cpu  ");

            if (string.IsNullOrWhiteSpace(cpuUsageLine))
            {
                Console.WriteLine($"Couldn't read line from '{CPUSTAT_FILEPATH}'");
                return;
            }

            // Format: "cpu  20546715 4367 11631326 215282964 96602 0 584080 0 0 0"
            var cpuNumberStrings = cpuUsageLine.Split(' ').Skip(2);

            if (cpuNumberStrings.Any(n => !long.TryParse(n, out _)))
            {
                Console.WriteLine($"Failed to parse '{CPUSTAT_FILEPATH}' output correctly. Line: {cpuUsageLine}");
                return;
            }

            var cpuNumbers = cpuNumberStrings.Select(long.Parse).ToArray();
            var idleTime = cpuNumbers[3];
            var iowait = cpuNumbers[4]; // Iowait is not real cpu time
            var totalTime = cpuNumbers.Sum() - iowait;

            if (i > 0)
            {
                var deltaIdleTime = idleTime - _prevIdleTime;
                var deltaTotalTime = totalTime - _prevTotalTime;

                if (deltaTotalTime == 0f)
                {
                    return;
                }

                var currentCpuUsage = (1.0f - deltaIdleTime / ((float)deltaTotalTime)) * 100f;

                var previousCpuUsage = CpuUsage ?? 0f;
                CpuUsage = (previousCpuUsage + 2 * currentCpuUsage) / 3;
            }

            _prevIdleTime = idleTime;
            _prevTotalTime = totalTime;
        }



        private async Task<string> ReadLineStartingWithAsync(string path, string lineStartsWith)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 512, FileOptions.SequentialScan | FileOptions.Asynchronous))
            using (var r = new StreamReader(fs, Encoding.ASCII))
            {
                string line;
                while ((line = await r.ReadLineAsync()) != null)
                {
                    if (line.StartsWith(lineStartsWith, StringComparison.Ordinal))
                        return line;
                }
            }
            return null;
        }

        public async Task<string> GetMemoryStatus()
        {
            await UpdateAvailableMemory();
            await UpdateTotalPhysicalMemory();
            float RamUsed = (float)((TotalPhysicalMemory - AvailableMemory) / (1024 * 1024));
            float ToTalRam = (float)(TotalPhysicalMemory / (1024 * 1024));
            string status = "RAM used - " + RamUsed + " from " + ToTalRam;
            return status;
        }

        public async Task<string> MonitorCpuUsage(CancellationToken ct)
        {
            int i = 0;
            while (true)
            {
                if (ct.IsCancellationRequested)
                    return "Error";

                try
                {
                    await Task.Run(async ()=>{
                        await UpdateCpuUsage(i);
                    });
           
                    var logStr = $"CpuUsage = {CpuUsage?.ToString("0.0")}";
                    Console.WriteLine(logStr);
                    if (logStr.Contains("."))
                    {
                        return logStr;
                    }               
                    await Task.Delay(MONITOR_PERIOD, ct);
                }
                catch (Exception ex) when (ex.GetType() != typeof(TaskCanceledException)) // !!!
                {
                    Console.WriteLine(ex.Message, "LinuxEnvironmentStatistics: error");
                    await Task.Delay(MONITOR_PERIOD + MONITOR_PERIOD + MONITOR_PERIOD, ct);
                }
                if (i < 2)
                    i++;
            }
        }


    }
}
