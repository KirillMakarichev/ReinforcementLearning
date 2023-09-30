using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GeometryDashBot;

public class MemoryWriter
{
    private IntPtr _processHandle; // Здесь нужно сохранить дескриптор процесса
    private readonly ProcessModule _processModule;

    public MemoryWriter(IntPtr processHandle, ProcessModule processModule)
    {
        this._processHandle = processHandle;
        _processModule = processModule;
    }

    public void PlayerFreeze()
    {
        WriteBytes(new byte[] { 0x90, 0x90, 0x90 }, 0x203519);
    }

    public void WriteBytes(byte[] buffer, int address)
    {
        WriteAt(buffer, address);
    }

    public void WriteAt(byte[] buffer, int address)
    {
        int bytesWritten = 0;
        WriteProcessMemory(_processHandle, (IntPtr)address, buffer, buffer.Length, ref bytesWritten);
    }

    public int ResolveLayers(int[] offsets, string module = null)
    {
        List<int> offsetList = new List<int>(offsets);

        int address;
        if (module == null)
        {
            address = BaseAddress;
        }
        else
        {
            address = GetBaseAddress(processId, module);
        }

        if (offsetList.Count > 0)
        {
            address += offsetList[0];
            offsetList.RemoveAt(0);
        }

        foreach (int offset in offsetList)
        {
            address = Read<int>(address) + offset;
        }

        return address;
    }

    private int BaseAddress
    {
        // Реализуйте логику получения базового адреса процесса
        get { return _processModule.BaseAddress.ToInt32(); }
    }

    private int processId;
    public int ProcessId
    {
        get { return processId; }
        set { processId = value; }
    }

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesWritten
    );

    // Реализуйте логику для получения базового адреса модуля
    private int GetBaseAddress(int processId, string moduleName)
    {
        // Ваш код здесь
        return 0;
    }

    // Реализуйте логику для чтения из памяти
    private T Read<T>(int address)
    {
        // Ваш код здесь
        return default(T);
    }
}