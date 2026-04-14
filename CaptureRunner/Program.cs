using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using CaptureRunner.Models;
using CaptureRunner.Runner;
using CaptureRunner.Services;
using FlaUI.UIA3;

namespace CaptureRunner;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("CaptureRunner requires Windows because FlaUI/UIA3 depends on the Windows UI Automation stack.");
            return 1;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        ScannerOptions options;
        try
        {
            options = ParseArguments(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 1;
        }

        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        var jsonOptions = CreateJsonOptions();

        if (!File.Exists(options.ExecutablePath))
        {
            Console.Error.WriteLine($"Executable not found: {options.ExecutablePath}");
            return 1;
        }

        var windowService = new WindowService();
        var displayTopologyService = new DisplayTopologyService();
        var environmentPreflightService = new EnvironmentPreflightService(displayTopologyService);
        var windowPlacementService = new WindowPlacementService(displayTopologyService, windowService);
        var treeService = new TreeService();
        var validator = new Validator(treeService);
        var navigationEngine = new NavigationEngine(treeService, validator, windowService);
        var explorer = new UiaExplorer(treeService);
        var windowSelectionClickService = new WindowSelectionClickService();
        var snagitCaptureService = new SnagitCaptureService(windowService, windowSelectionClickService);
        var nativeWindowCaptureService = new NativeWindowCaptureService();
        var scanner = new ScreenScanner(windowService, explorer, navigationEngine, validator, snagitCaptureService, nativeWindowCaptureService, windowPlacementService);
        var discoverer = new WindowDiscoverer(windowService, treeService);

        AppSession? session = null;
        OperatorNoticeService? operatorNotice = null;
        MouseParkingService? mouseParking = null;
        List<ScreenPlan> screens = new();

        try
        {
            if (!string.IsNullOrWhiteSpace(options.EnvironmentProfilePath))
            {
                if (!File.Exists(options.EnvironmentProfilePath))
                {
                    Console.Error.WriteLine($"Environment profile not found: {options.EnvironmentProfilePath}");
                    return 1;
                }

                var environmentProfileContent = File.ReadAllText(options.EnvironmentProfilePath);
                options.EnvironmentProfile = JsonSerializer.Deserialize<EnvironmentProfile>(environmentProfileContent, jsonOptions);
            }

            var preflight = environmentPreflightService.Run(options);
            if (!preflight.Passed && options.StrictPreflight)
            {
                Console.Error.WriteLine("Environment preflight failed:");
                foreach (var check in preflight.Checks.Where(check => !check.Passed))
                {
                    Console.Error.WriteLine($"- {check.Name}: {check.Message}");
                }

                if (preflight.ActualDisplays.Count > 0)
                {
                    Console.Error.WriteLine($"- actual_displays: {DisplayTopologyService.DescribeDisplays(preflight.ActualDisplays)}");
                }

                return 1;
            }

            var operatorDisplay = displayTopologyService.FindMatch(options.EnvironmentProfile?.DisplayProfile.OperatorDisplay, preflight.ActualDisplays);

            if (!string.IsNullOrWhiteSpace(options.PlanPath))
            {
                if (!File.Exists(options.PlanPath))
                {
                    Console.Error.WriteLine($"Plan file not found: {options.PlanPath}");
                    return 1;
                }

                var planContent = File.ReadAllText(options.PlanPath);
                screens = JsonSerializer.Deserialize<List<ScreenPlan>>(planContent, jsonOptions) ?? new List<ScreenPlan>();
                if (screens.Count == 0)
                {
                    Console.Error.WriteLine("The input plan is empty. Provide at least one screen definition.");
                    return 1;
                }
            }

            var requiresCaptureCursorControl = !options.WorkerMode && screens.Any(options.IsSnagitCaptureEnabled);

            if (options.ShowOperatorNotice && !options.WorkerMode)
            {
                operatorNotice = new OperatorNoticeService(operatorDisplay, options.OperatorMessage);
                operatorNotice.Show();
            }

            if (requiresCaptureCursorControl)
            {
                Console.WriteLine("Mouse parking skipped: Snagit capture is enabled and may need temporary cursor control on the capture display.");
            }
            else if (!options.WorkerMode)
            {
                mouseParking = new MouseParkingService(operatorDisplay);
                mouseParking.Start();
            }

            session = windowService.EnsureSession(options.ExecutablePath, options.StartupTimeout);
            using var automation = new UIA3Automation();

            if (!string.IsNullOrWhiteSpace(options.DiscoveryOutputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(options.DiscoveryOutputPath) ?? Environment.CurrentDirectory);
                var discovery = discoverer.Discover(session, automation, options);
                File.WriteAllText(options.DiscoveryOutputPath, JsonSerializer.Serialize(discovery, jsonOptions));
                Console.WriteLine($"Discovery written to {options.DiscoveryOutputPath}");
                if (string.IsNullOrWhiteSpace(options.PlanPath))
                {
                    return 0;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath) ?? Environment.CurrentDirectory);

            List<ScreenScanResult> results;
            if (options.WorkerMode)
            {
                results = new List<ScreenScanResult>(screens.Count);
                foreach (var screen in screens)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanning {screen.RowId} ({screen.ScreenName})");
                    var rowResult = scanner.Scan(session, automation, screen, options);
                    rowResult.Diagnostics.Preflight = preflight;
                    results.Add(rowResult);
                }
            }
            else
            {
                results = RunScreensWithWorkers(screens, options, jsonOptions);
                foreach (var result in results)
                {
                    result.Diagnostics.Preflight = preflight;
                }
            }

            File.WriteAllText(options.OutputPath, JsonSerializer.Serialize(results, jsonOptions));
            Console.WriteLine($"Report written to {options.OutputPath}");

            return results.Any(result => string.Equals(result.Classification.Automatable, "none", StringComparison.OrdinalIgnoreCase))
                ? 2
                : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Scanner initialization failed: {ex.Message}");
            return 1;
        }
        finally
        {
            mouseParking?.Dispose();
            operatorNotice?.Dispose();

            if (session is not null && session.StartedByScanner && !options.KeepApplicationOpen)
            {
                windowService.TryCloseSession(session);
            }
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
    }

    private static ScannerOptions ParseArguments(IReadOnlyList<string> args)
    {
        var options = new ScannerOptions();

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
                case "--exe":
                    options.ExecutablePath = GetNextValue(args, ref index, arg);
                    break;
                case "--plan":
                    options.PlanPath = GetNextValue(args, ref index, arg);
                    break;
                case "--output":
                    options.OutputPath = GetNextValue(args, ref index, arg);
                    break;
                case "--discover-output":
                    options.DiscoveryOutputPath = GetNextValue(args, ref index, arg);
                    break;
                case "--environment-profile":
                    options.EnvironmentProfilePath = GetNextValue(args, ref index, arg);
                    break;
                case "--startup-timeout-ms":
                    options.StartupTimeout = TimeSpan.FromMilliseconds(ParsePositiveInt(GetNextValue(args, ref index, arg), arg));
                    break;
                case "--navigation-timeout-ms":
                    options.NavigationTimeout = TimeSpan.FromMilliseconds(ParsePositiveInt(GetNextValue(args, ref index, arg), arg));
                    break;
                case "--screen-timeout-ms":
                    options.ScreenTimeout = TimeSpan.FromMilliseconds(ParsePositiveInt(GetNextValue(args, ref index, arg), arg));
                    break;
                case "--capture-timeout-ms":
                    options.CaptureTimeout = TimeSpan.FromMilliseconds(ParsePositiveInt(GetNextValue(args, ref index, arg), arg));
                    break;
                case "--keep-open":
                    options.KeepApplicationOpen = true;
                    break;
                case "--no-operator-notice":
                    options.ShowOperatorNotice = false;
                    break;
                case "--operator-message":
                    options.OperatorMessage = GetNextValue(args, ref index, arg);
                    break;
                case "--snagit-hotkey":
                    options.SnagitHotkey = GetNextValue(args, ref index, arg);
                    break;
                case "--snagit-exe":
                    options.SnagitExecutablePath = GetNextValue(args, ref index, arg);
                    break;
                case "--snagit-watch-dir":
                    options.SnagitWatchDirectory = GetNextValue(args, ref index, arg);
                    break;
                case "--capture-output-dir":
                    options.CaptureOutputDirectory = GetNextValue(args, ref index, arg);
                    break;
                case "--capture-backend":
                    options.CaptureBackend = ParseCaptureBackend(GetNextValue(args, ref index, arg), arg);
                    break;
                case "--native-capture-area":
                    options.NativeCaptureArea = ParseNativeCaptureArea(GetNextValue(args, ref index, arg), arg);
                    break;
                case "--png-dpi":
                    options.NativePngDpi = ParsePositiveFloat(GetNextValue(args, ref index, arg), arg);
                    break;
                case "--strict-preflight":
                    options.StrictPreflight = true;
                    break;
                case "--allow-preflight-warnings":
                    options.StrictPreflight = false;
                    break;
                case "--worker-mode":
                    options.WorkerMode = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(options.ExecutablePath))
        {
            throw new ArgumentException("Missing required argument: --exe <path-to-application>");
        }

        if (string.IsNullOrWhiteSpace(options.PlanPath) && string.IsNullOrWhiteSpace(options.DiscoveryOutputPath))
        {
            throw new ArgumentException("Missing required argument: provide --plan <path-to-plan.json>, --discover-output <path>, or both.");
        }

        if (options.CaptureBackend == CaptureBackend.Snagit
            && (!string.IsNullOrWhiteSpace(options.SnagitHotkey) ^ !string.IsNullOrWhiteSpace(options.SnagitWatchDirectory)))
        {
            throw new ArgumentException("Snagit capture requires both --snagit-hotkey and --snagit-watch-dir.");
        }

        options.ExecutablePath = Path.GetFullPath(options.ExecutablePath);
        if (!string.IsNullOrWhiteSpace(options.PlanPath))
        {
            options.PlanPath = Path.GetFullPath(options.PlanPath);
        }

        if (!string.IsNullOrWhiteSpace(options.EnvironmentProfilePath))
        {
            options.EnvironmentProfilePath = Path.GetFullPath(options.EnvironmentProfilePath);
        }

        options.OutputPath = Path.GetFullPath(options.OutputPath);
        options.CaptureOutputDirectory = Path.GetFullPath(options.CaptureOutputDirectory);
        if (!string.IsNullOrWhiteSpace(options.DiscoveryOutputPath))
        {
            options.DiscoveryOutputPath = Path.GetFullPath(options.DiscoveryOutputPath);
        }

        if (!string.IsNullOrWhiteSpace(options.SnagitWatchDirectory))
        {
            options.SnagitWatchDirectory = Path.GetFullPath(options.SnagitWatchDirectory);
        }

        if (!string.IsNullOrWhiteSpace(options.SnagitExecutablePath))
        {
            options.SnagitExecutablePath = Path.GetFullPath(options.SnagitExecutablePath);
        }

        return options;
    }

    private static string GetNextValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {option}");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid value for {option}: {value}");
        }

        return parsed;
    }

    private static float ParsePositiveFloat(string value, string option)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid value for {option}: {value}");
        }

        return parsed;
    }

    private static CaptureBackend ParseCaptureBackend(string value, string option)
    {
        if (!Enum.TryParse<CaptureBackend>(value, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"Invalid value for {option}: {value}");
        }

        return parsed;
    }

    private static NativeCaptureArea ParseNativeCaptureArea(string value, string option)
    {
        if (!Enum.TryParse<NativeCaptureArea>(value, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException($"Invalid value for {option}: {value}");
        }

        return parsed;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            Usage:
              CaptureRunner --exe <path-to-app.exe> (--plan <plan.json> | --discover-output <discovery.json> | both) [--output <report.json>] [--environment-profile <profile.json>] [--strict-preflight | --allow-preflight-warnings] [--startup-timeout-ms 20000] [--navigation-timeout-ms 4000] [--screen-timeout-ms 120000] [--capture-timeout-ms 20000] [--keep-open] [--no-operator-notice] [--operator-message "<text>"] [--capture-backend <snagit|native>] [--snagit-hotkey "Ctrl+Shift+5"] [--snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe"] [--snagit-watch-dir <folder>] [--native-capture-area <window|client>] [--png-dpi <dpi>] [--capture-output-dir <folder>]

            Example:
              CaptureRunner --exe "C:\Path\To\App.exe" --plan ".\sample-plan.json" --output ".\Output\report.json"

              CaptureRunner --exe "C:\Path\To\App.exe" --discover-output ".\Output\discovery.json"

              CaptureRunner --exe "C:\Path\To\App.exe" --plan ".\wave1-snagit-poc-plan.json" --output ".\Output\wave1-snagit-poc-report.json" --environment-profile ".\Profiles\certified-single-4k.json" --snagit-hotkey "Ctrl+Shift+5" --snagit-exe "C:\Program Files\TechSmith\Snagit\SnagitCapture.exe" --snagit-watch-dir "C:\SnagitDrop" --capture-output-dir ".\Output\captures" --keep-open

              CaptureRunner --exe "C:\Path\To\App.exe" --plan ".\wave1-snagit-poc-plan.json" --output ".\Output\wave1-native-poc-report.json" --environment-profile ".\Profiles\certified-dual-monitor.json" --capture-backend native --native-capture-area client --png-dpi 600 --capture-output-dir ".\Output\captures-native" --keep-open
            """);
    }

    private static List<ScreenScanResult> RunScreensWithWorkers(
        IReadOnlyList<ScreenPlan> screens,
        ScannerOptions options,
        JsonSerializerOptions jsonOptions)
    {
        var results = new List<ScreenScanResult>(screens.Count);

        foreach (var screen in screens)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Scanning {screen.RowId} ({screen.ScreenName})");
            var result = RunScreenInWorker(screen, options, jsonOptions);
            results.Add(result);

            File.WriteAllText(options.OutputPath, JsonSerializer.Serialize(results, jsonOptions));
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Partial report updated: {results.Count}/{screens.Count} screens; last result {screen.RowId} => {result.Classification.Automatable}/{result.Classification.Confidence} via {result.OpenResult.MethodUsed}");
        }

        return results;
    }

    private static ScreenScanResult RunScreenInWorker(
        ScreenPlan screen,
        ScannerOptions options,
        JsonSerializerOptions jsonOptions)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CaptureRunner", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var tempPlanPath = Path.Combine(tempDir, "plan.json");
        var tempOutputPath = Path.Combine(tempDir, "report.json");
        File.WriteAllText(tempPlanPath, JsonSerializer.Serialize(new[] { screen }, jsonOptions));

        Process? worker = null;

        try
        {
            var processPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Unable to resolve the current CaptureRunner executable path.");

            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory
            };

            startInfo.ArgumentList.Add("--worker-mode");
            startInfo.ArgumentList.Add("--exe");
            startInfo.ArgumentList.Add(options.ExecutablePath);
            startInfo.ArgumentList.Add("--plan");
            startInfo.ArgumentList.Add(tempPlanPath);
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(tempOutputPath);
            startInfo.ArgumentList.Add("--startup-timeout-ms");
            startInfo.ArgumentList.Add(((int)options.StartupTimeout.TotalMilliseconds).ToString());
            startInfo.ArgumentList.Add("--navigation-timeout-ms");
            startInfo.ArgumentList.Add(((int)options.NavigationTimeout.TotalMilliseconds).ToString());
            startInfo.ArgumentList.Add("--capture-timeout-ms");
            startInfo.ArgumentList.Add(((int)options.CaptureTimeout.TotalMilliseconds).ToString());
            startInfo.ArgumentList.Add("--keep-open");
            startInfo.ArgumentList.Add("--no-operator-notice");

            if (options.CaptureBackend != CaptureBackend.Snagit)
            {
                startInfo.ArgumentList.Add("--capture-backend");
                startInfo.ArgumentList.Add(options.CaptureBackend.ToString());
            }

            if (options.SnagitCaptureEnabled)
            {
                startInfo.ArgumentList.Add("--snagit-hotkey");
                startInfo.ArgumentList.Add(options.SnagitHotkey!);
                if (!string.IsNullOrWhiteSpace(options.SnagitExecutablePath))
                {
                    startInfo.ArgumentList.Add("--snagit-exe");
                    startInfo.ArgumentList.Add(options.SnagitExecutablePath);
                }
                startInfo.ArgumentList.Add("--snagit-watch-dir");
                startInfo.ArgumentList.Add(options.SnagitWatchDirectory!);
                startInfo.ArgumentList.Add("--capture-output-dir");
                startInfo.ArgumentList.Add(options.CaptureOutputDirectory);
            }

            if (options.CaptureBackend == CaptureBackend.Native)
            {
                startInfo.ArgumentList.Add("--capture-output-dir");
                startInfo.ArgumentList.Add(options.CaptureOutputDirectory);

                if (options.NativeCaptureArea != NativeCaptureArea.Window)
                {
                    startInfo.ArgumentList.Add("--native-capture-area");
                    startInfo.ArgumentList.Add(options.NativeCaptureArea.ToString());
                }

                if (options.NativePngDpi.HasValue)
                {
                    startInfo.ArgumentList.Add("--png-dpi");
                    startInfo.ArgumentList.Add(options.NativePngDpi.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (!string.IsNullOrWhiteSpace(options.EnvironmentProfilePath))
            {
                startInfo.ArgumentList.Add("--environment-profile");
                startInfo.ArgumentList.Add(options.EnvironmentProfilePath);
            }

            startInfo.ArgumentList.Add(options.StrictPreflight
                ? "--strict-preflight"
                : "--allow-preflight-warnings");

            worker = Process.Start(startInfo)
                     ?? throw new InvalidOperationException($"Unable to start the worker process for {screen.RowId}.");

            if (!worker.WaitForExit((int)options.ScreenTimeout.TotalMilliseconds))
            {
                TryKillProcessTree(worker);
                return ScreenScanResult.CreateTimeoutFailure(screen, options.ScreenTimeout);
            }

            if (!File.Exists(tempOutputPath))
            {
                return ScreenScanResult.CreateFatalFailure(
                    screen,
                    $"Worker exited without writing a report for {screen.RowId} (exit code {worker.ExitCode}).",
                    null);
            }

            var outputContent = File.ReadAllText(tempOutputPath);
            var results = JsonSerializer.Deserialize<List<ScreenScanResult>>(outputContent, jsonOptions) ?? new List<ScreenScanResult>();
            if (results.Count == 0)
            {
                return ScreenScanResult.CreateFatalFailure(
                    screen,
                    $"Worker wrote an empty report for {screen.RowId} (exit code {worker.ExitCode}).",
                    null);
            }

            return results[0];
        }
        catch (Exception ex)
        {
            TryKillProcessTree(worker);
            return ScreenScanResult.CreateFatalFailure(
                screen,
                $"Worker orchestration failed for {screen.RowId}: {ex.Message}",
                ex.ToString());
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static void TryKillProcessTree(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort only.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Temp cleanup is best effort only.
        }
    }
}
