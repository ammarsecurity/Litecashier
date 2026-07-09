namespace PrintServer.Models;

public class PrinterConfig
{
    public string Type { get; set; } = "windows"; // 'usb', 'serial', 'network', 'file', 'windows'
    public int UsbVendorId { get; set; } = 0x04f9; // Brother printer vendor ID
    public int UsbProductId { get; set; } = 0x2042; // Brother printer product ID
    public string SerialPort { get; set; } = "COM3";
    public string NetworkHost { get; set; } = "192.168.1.100";
    public int NetworkPort { get; set; } = 9100;
    public string? WindowsPrinterName { get; set; } = null; // Use specific printer name or null for default
    public string FilePath { get; set; } = "print_output.txt";
    public bool UseEscPosCommands { get; set; } = true; // Use ESC/POS commands for thermal printers
    public string Encoding { get; set; } = "windows-1256"; // 'utf-8', 'windows-1256', 'cp1256'
    public int EscPosEncoding { get; set; } = 17; // ESC/POS encoding: 16=UTF-8, 17=Windows-1256 (Arabic), 0=PC437
}

