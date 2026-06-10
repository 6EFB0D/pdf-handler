using System.Diagnostics;

namespace PdfHandler.UI.Helpers;

public static class BrowserHelper
{
    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }
}
