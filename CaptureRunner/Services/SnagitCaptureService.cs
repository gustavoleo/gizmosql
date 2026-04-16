using System.Diagnostics;
using System.IO.Compression;
using System.Windows.Forms;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;

namespace CaptureRunner.Services;

public sealed class SnagitCaptureService
{
    private static readonly string[] CaptureExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".tif",
        ".tiff",
        ".snagx"
    };

    private readonly WindowService _windowService;
    private readonly WindowSelectionClickService _windowSelectionClickService;

    public SnagitCaptureService(WindowService windowService, WindowSelectionClickService windowSelectionClickService)
    {
        _windowService = windowService;
        _windowSelectionClickService = windowSelectionClickService;
    }

    public CaptureResult Capture(Window? window, ScreenPlan plan, ScannerOptions options)
    {
        if (!options.IsSnagitCaptureEnabled(plan))
        {
            return new CaptureResult
            {
                Enabled = false,
                Attempted = false,
                Success = false,
                Method = "none",
                Note = "Snagit capture is disabled."
            };
        }

        var result = new CaptureResult
        {
            Enabled = true,
            Attempted = true,
            Success = false,
            Method = "snagit_hotkey"
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var settings = ResolveSettings(plan, options);
            if (!settings.Enabled)
            {
                result.Enabled = false;
                result.Attempted = false;
                result.Method = "none";
                result.Note = "Snagit capture is disabled for this screen.";
                return result;
            }

            result.Method = settings.Mode switch
            {
                CaptureMode.Window => "snagit_window_preset",
                CaptureMode.Menu => "snagit_menu_preset",
                CaptureMode.Region => "snagit_region_preset",
                _ => "snagit_hotkey"
            };

            Directory.CreateDirectory(settings.WatchDirectory);
            Directory.CreateDirectory(options.CaptureOutputDirectory);
            EnsureSnagitReady(options);

            _windowService.TryBringToFront(window);
            Thread.Sleep(500);

            var knownFiles = SnapshotFiles(settings.WatchDirectory);

            KeyboardInputService.SendHotkey(settings.Hotkey);

            if (settings.Mode == CaptureMode.Window)
            {
                if (window is null)
                {
                    result.Note = "Window capture mode was requested, but no window was available to select.";
                    return result;
                }

                var clickResult = _windowSelectionClickService.Click(window, settings.CaptureHints);
                result.Note = clickResult.Note;
                if (!clickResult.Success)
                {
                    return result;
                }
            }

            var sourceFile = WaitForCaptureFile(
                settings.WatchDirectory,
                knownFiles,
                options.CaptureTimeout);

            if (sourceFile is null)
            {
                result.Note = $"No new capture file appeared in {settings.WatchDirectory} within {options.CaptureTimeout.TotalMilliseconds:0} ms.";
                return result;
            }

            var outputFile = SaveManagedCapture(sourceFile, plan, options.CaptureOutputDirectory);

            result.Success = true;
            result.SourceFile = sourceFile;
            result.OutputFile = outputFile;
            result.Note = Path.GetExtension(sourceFile).Equals(".snagx", StringComparison.OrdinalIgnoreCase)
                ? "Snagit hotkey triggered and the capture package was extracted into the managed output folder."
                : "Snagit hotkey triggered and capture file was copied into the managed output folder.";
            return result;
        }
        catch (Exception ex)
        {
            result.Note = ex.Message;
            result.Exception = ex.ToString();
            return result;
        }
        finally
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
        }
    }

    private static Dictionary<string, DateTime> SnapshotFiles(string directory)
    {
        return Directory
            .EnumerateFiles(directory)
            .Where(file => CaptureExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToDictionary(file => file, file => File.GetLastWriteTimeUtc(file), StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureSnagitReady(ScannerOptions options)
    {
        if (Process.GetProcessesByName("SnagitCapture").Any(process => !process.HasExited))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SnagitExecutablePath) || !File.Exists(options.SnagitExecutablePath))
        {
            throw new InvalidOperationException(
                $"SnagitCapture.exe is not running and the configured Snagit executable was not found: {options.SnagitExecutablePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = options.SnagitExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(options.SnagitExecutablePath) ?? Environment.CurrentDirectory,
            UseShellExecute = true
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException($"Unable to start Snagit from {options.SnagitExecutablePath}.");
        }

        Thread.Sleep(1500);
    }

    private static string? WaitForCaptureFile(
        string directory,
        IReadOnlyDictionary<string, DateTime> knownFiles,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var candidate = Directory
                .EnumerateFiles(directory)
                .Where(file => CaptureExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .Select(file => new
                {
                    Path = file,
                    LastWriteUtc = File.GetLastWriteTimeUtc(file)
                })
                .OrderByDescending(item => item.LastWriteUtc)
                .FirstOrDefault(item => !knownFiles.TryGetValue(item.Path, out var previousWrite)
                                        || previousWrite < item.LastWriteUtc);

            if (candidate is not null && IsStable(candidate.Path))
            {
                return candidate.Path;
            }

            Thread.Sleep(250);
        }

        return null;
    }

    private static bool IsStable(string path)
    {
        try
        {
            var firstLength = new FileInfo(path).Length;
            Thread.Sleep(200);
            var secondLength = new FileInfo(path).Length;
            if (firstLength != secondLength)
            {
                return false;
            }

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return stream.Length >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string SaveManagedCapture(string sourceFile, ScreenPlan plan, string outputDirectory)
    {
        var extension = Path.GetExtension(sourceFile);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        if (!extension.Equals(".snagx", StringComparison.OrdinalIgnoreCase))
        {
            var outputFile = Path.Combine(outputDirectory, CaptureOutputFileNameBuilder.Build(plan, extension));
            File.Copy(sourceFile, outputFile, overwrite: true);
            return outputFile;
        }

        using var archive = ZipFile.OpenRead(sourceFile);
        var imageEntry = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .Where(entry => CaptureExtensions.Contains(Path.GetExtension(entry.Name), StringComparer.OrdinalIgnoreCase))
            .Where(entry => !entry.Name.Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase))
            .Where(entry => !entry.Name.Contains(".backup.", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Length)
            .FirstOrDefault();

        if (imageEntry is null)
        {
            throw new InvalidOperationException($"No primary image entry was found inside {sourceFile}.");
        }

        var imageExtension = Path.GetExtension(imageEntry.Name);
        if (string.IsNullOrWhiteSpace(imageExtension))
        {
            imageExtension = ".png";
        }

        var extractedFile = Path.Combine(outputDirectory, CaptureOutputFileNameBuilder.Build(plan, imageExtension));
        using var sourceStream = imageEntry.Open();
        using var targetStream = File.Create(extractedFile);
        sourceStream.CopyTo(targetStream);
        return extractedFile;
    }

    private static ResolvedCaptureSettings ResolveSettings(ScreenPlan plan, ScannerOptions options)
    {
        var hotkey = string.IsNullOrWhiteSpace(plan.Capture.PresetHotkey)
            ? options.SnagitHotkey
            : plan.Capture.PresetHotkey;
        var watchDirectory = string.IsNullOrWhiteSpace(plan.Capture.WatchDirectory)
            ? options.SnagitWatchDirectory
            : plan.Capture.WatchDirectory;

        return new ResolvedCaptureSettings
        {
            Enabled = !string.IsNullOrWhiteSpace(hotkey) && !string.IsNullOrWhiteSpace(watchDirectory),
            Hotkey = hotkey ?? string.Empty,
            WatchDirectory = watchDirectory ?? string.Empty,
            Mode = plan.Capture.Mode,
            CaptureHints = plan.Capture
        };
    }

    private sealed class ResolvedCaptureSettings
    {
        public required bool Enabled { get; init; }

        public required string Hotkey { get; init; }

        public required string WatchDirectory { get; init; }

        public required CaptureMode Mode { get; init; }

        public required CaptureHints CaptureHints { get; init; }
    }
}
