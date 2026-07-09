using System.Runtime.InteropServices;
using System.Text;

namespace PrintServer.Services;

public class RawPrinterHelper
{
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPTStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDocName = string.Empty;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pOutputFile = string.Empty;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDataType = string.Empty;
    }

    public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes)
    {
        IntPtr hPrinter = IntPtr.Zero;
        DOCINFOA di = new DOCINFOA();
        bool bSuccess = false;
        int dwWritten = 0;

        di.pDocName = "POS Print Job";
        di.pDataType = "RAW";

        try
        {
            if (OpenPrinter(szPrinterName, out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
                        Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);
                        bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out dwWritten);
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }

            if (bSuccess == false)
            {
                int dwError = Marshal.GetLastWin32Error();
                Console.WriteLine($"Error writing to printer. Error code: {dwError}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SendBytesToPrinter: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            bSuccess = false;
        }

        return bSuccess;
    }
}

