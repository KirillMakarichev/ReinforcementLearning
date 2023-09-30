using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GeometryDashBot;

public class ScreenshotMaker
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    public static Size GetMonitorSize()
    {
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        using var g = Graphics.FromHwnd(hwnd);
        return new Size((int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height);
    }

    public static Bitmap TakeScreenshot(Size size)
    {
        var bmp = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(bmp);
        graphics.CopyFromScreen(Point.Empty, Point.Empty, bmp.Size);
        return bmp;
    }
    
    public static Bitmap TakeScreenshot()
    {
        var size = GetMonitorSize();
        var bmp = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(bmp);
        graphics.CopyFromScreen(Point.Empty, Point.Empty, bmp.Size);
        return bmp;
    }
    
    public static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
    {
        var result = new Bitmap(width, height);
        using var g = Graphics.FromImage(result);
        g.DrawImage(bmp, 0, 0, width, height);

        return result;
    }
}