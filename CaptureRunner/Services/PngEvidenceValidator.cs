using System.Drawing;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class PngEvidenceValidator
{
    public PngValidationResult Validate(string? path, float expectedDpi)
    {
        var result = new PngValidationResult
        {
            ExpectedDpi = expectedDpi
        };

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            result.Note = "PNG file was not found.";
            return result;
        }

        result.Exists = true;

        try
        {
            using var image = Image.FromFile(path);
            result.Width = image.Width;
            result.Height = image.Height;
            result.HorizontalDpi = image.HorizontalResolution;
            result.VerticalDpi = image.VerticalResolution;

            var dpiMatches = Math.Abs(image.HorizontalResolution - expectedDpi) <= 1
                             && Math.Abs(image.VerticalResolution - expectedDpi) <= 1;
            result.Valid = image.Width > 1 && image.Height > 1 && dpiMatches;
            result.Note = result.Valid
                ? "PNG evidence passed dimension and DPI checks."
                : "PNG exists, but dimensions or DPI metadata did not match expectations.";
            return result;
        }
        catch (Exception ex)
        {
            result.Note = ex.Message;
            return result;
        }
    }
}
