using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CaptureRunner.Models;
using FlaUI.Core.AutomationElements;

namespace CaptureRunner.Services;

public sealed class NativeWindowCaptureService
{
    public CaptureResult Capture(Window? window, ScreenPlan plan, ScannerOptions options)
    {
        var result = new CaptureResult
        {
            Enabled = true,
            Attempted = true,
            Success = false,
            Method = options.NativeCaptureArea == NativeCaptureArea.Client
                ? "native_client_png"
                : "native_window_png"
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (window is null)
            {
                result.Note = "Native capture requested, but no target window was available.";
                return result;
            }

            var handle = GetWindowHandle(window);
            if (handle == IntPtr.Zero)
            {
                result.Note = "Native capture requested, but the target window does not expose a native handle.";
                return result;
            }

            Directory.CreateDirectory(options.CaptureOutputDirectory);

            using var bitmap = options.NativeCaptureArea switch
            {
                NativeCaptureArea.Client => CaptureClientArea(handle),
                _ => CaptureWindowArea(window)
            };

            if (options.NativePngDpi.HasValue && options.NativePngDpi.Value > 0)
            {
                bitmap.SetResolution(options.NativePngDpi.Value, options.NativePngDpi.Value);
            }

            var outputFile = Path.Combine(
                options.CaptureOutputDirectory,
                CaptureOutputFileNameBuilder.Build(plan, ".png"));

            bitmap.Save(outputFile, ImageFormat.Png);

            result.Success = true;
            result.OutputFile = outputFile;
            result.SourceFile = outputFile;
            result.Note = options.NativePngDpi.HasValue && options.NativePngDpi.Value > 0
                ? $"Captured {options.NativeCaptureArea.ToString().ToLowerInvariant()} area to PNG with {options.NativePngDpi.Value:0.##} DPI metadata."
                : $"Captured {options.NativeCaptureArea.ToString().ToLowerInvariant()} area to PNG.";
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

    private static Bitmap CaptureWindowArea(Window window)
    {
        var rect = window.BoundingRectangle;
        var left = (int)Math.Round((double)rect.Left);
        var top = (int)Math.Round((double)rect.Top);
        var width = Math.Max(1, (int)Math.Round((double)rect.Width));
        var height = Math.Max(1, (int)Math.Round((double)rect.Height));

        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Bitmap CaptureClientArea(IntPtr windowHandle)
    {
        if (!GetClientRect(windowHandle, out var clientRect))
        {
            throw new InvalidOperationException("Unable to read the client area for native capture.");
        }

        var topLeft = new POINT { X = clientRect.Left, Y = clientRect.Top };
        if (!ClientToScreen(windowHandle, ref topLeft))
        {
            throw new InvalidOperationException("Unable to translate the client area into screen coordinates.");
        }

        var width = Math.Max(1, clientRect.Right - clientRect.Left);
        var height = Math.Max(1, clientRect.Bottom - clientRect.Top);

        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(topLeft.X, topLeft.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static IntPtr GetWindowHandle(Window window)
    {
        try
        {
            if (window.Properties.NativeWindowHandle.TryGetValue(out var handle))
            {
                return new IntPtr(handle);
            }
        }
        catch
        {
            // Best effort only.
        }

        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
}
