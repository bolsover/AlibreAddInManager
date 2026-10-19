using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AlibreCatalogManager.Publishing
{
    /// <summary>Runs MSBuild on a project, streaming its output.</summary>
    public static class ProjectBuilder
    {
        /// <returns>MSBuild's exit code; 0 is success.</returns>
        public static Task<int> BuildAsync(string msbuild, string projectFile, string configuration, Action<string> output, CancellationToken cancel)
        {
            // /nr:false so no worker nodes linger holding the output DLL after the build.
            var start = new ProcessStartInfo(msbuild,
                $"\"{projectFile}\" /t:Build /p:Configuration={configuration} /nologo /nr:false /v:minimal")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = start, EnableRaisingEvents = true };
            var done = new TaskCompletionSource<int>();
            process.OutputDataReceived += (s, e) => { if (e.Data != null) output(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) output(e.Data); };
            process.Exited += (s, e) =>
            {
                // Flush the asynchronous readers before reporting the exit code.
                process.WaitForExit();
                var code = process.ExitCode;
                process.Dispose();
                if (cancel.IsCancellationRequested) done.TrySetCanceled();
                else done.TrySetResult(code);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            cancel.Register(() =>
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // Already exited.
                }
            });

            return done.Task;
        }
    }
}
