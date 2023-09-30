using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GeometryDashAPI.Memory;

namespace GeometryDashBot;

class GDApiManager : GameProcess
{
    private ProcessModule _mainModule;
    private readonly MemoryWriter _memoryWriter;
    private const string windowTitle = "Geometry Dash";
    public GDApiManager()
    {
        Initialize(Access.PROCESS_VM_READ);
        _mainModule = GetModule("GeometryDash.exe");
        _memoryWriter = new MemoryWriter(Game.Handle, _mainModule);
    }

    public bool IsDead()
    {
        return Read<bool>(_mainModule, new[] { 0x3222D0, 0x164, 0x39C });
    }
    
    public float GetLevelLength()
    {
        return Read<float>(_mainModule, new[] { 0x3222D0, 0x164, 0x3B4 });
    }

    public float GetXPos()
    {
        return Read<float>(_mainModule, new[] { 0x3222D0, 0x164, 0x224, 0x34 });
    }

    public float GetLevelPercent()
    {
        return GetXPos() / GetLevelLength() * 100.0f;
    }

    public void Freeze()
    {
        _memoryWriter.PlayerFreeze();
    }

    public static bool GameOnTop() => GetActiveWindowTitle() == windowTitle;
    
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private static string GetActiveWindowTitle()
    {
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);
        IntPtr handle = GetForegroundWindow();

        if (GetWindowText(handle, Buff, nChars) > 0)
        {
            return Buff.ToString();
        }
        return null;
    }
}