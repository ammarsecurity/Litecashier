using System.Text.Json;
using PrintServer.Models;

namespace PrintServer.Services;

public class ConfigurationService
{
    private readonly string _configFilePath;
    private PrinterConfig _config;

    public ConfigurationService()
    {
        var currentDir = Directory.GetCurrentDirectory();
        _configFilePath = Path.Combine(currentDir, "print_server_config.json");
        _config = LoadConfig();
    }

    public PrinterConfig GetConfig()
    {
        return _config;
    }

    public void UpdateConfig(PrinterConfig newConfig)
    {
        _config = newConfig;
        SaveConfig(_config);
    }

    private PrinterConfig LoadConfig()
    {
        if (File.Exists(_configFilePath))
        {
            try
            {
                var json = File.ReadAllText(_configFilePath, System.Text.Encoding.UTF8);
                var config = JsonSerializer.Deserialize<PrinterConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return config ?? GetDefaultConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config file: {ex.Message}");
                Console.WriteLine("Using default configuration");
                return GetDefaultConfig();
            }
        }
        else
        {
            var defaultConfig = GetDefaultConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }
    }

    private void SaveConfig(PrinterConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configFilePath, json, System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config file: {ex.Message}");
        }
    }

    private PrinterConfig GetDefaultConfig()
    {
        return new PrinterConfig
        {
            Type = "windows",
            UsbVendorId = 0x04f9,
            UsbProductId = 0x2042,
            SerialPort = "COM3",
            NetworkHost = "192.168.1.100",
            NetworkPort = 9100,
            WindowsPrinterName = null,
            FilePath = "print_output.txt",
            UseEscPosCommands = true,
            Encoding = "utf-8",
            EscPosEncoding = 16
        };
    }
}


