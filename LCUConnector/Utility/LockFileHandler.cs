using System.IO;
using System.Threading.Tasks;

namespace LCUConnector.Utility
{
    internal class LockFileHandler
    {
        private const string FileName = "lockfile";

        public async Task<(int port, string token)> ParseLockFileAsync(string path)
        {
            var lockfilePath = await WaitForFileAsync(path).ConfigureAwait(false);
            
            await using var fileStream =
                new FileStream(lockfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            
            var contents = await reader.ReadToEndAsync();
            var items = contents.Split(':');

            var processId = int.Parse(items[1]);
            var port = int.Parse(items[2]);
            var token = items[3];

            return (port, token);
        }
        
        private async Task<string> WaitForFileAsync(string path)
        {
            var filePath = Path.Combine(path, FileName);
            if (File.Exists(filePath))
                return filePath;

            var fileCreated = new TaskCompletionSource<bool>();
            var fileWatcher = new FileSystemWatcher(path);
            
            fileWatcher.Created += (_, e) =>
            {
                if (e.Name != FileName) return;
                
                filePath = e.FullPath;
                fileWatcher.Dispose();
                fileCreated.SetResult(true);
            };
            
            fileWatcher.EnableRaisingEvents = true;

            await fileCreated.Task;
            return filePath;
        }
    }
}