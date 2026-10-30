using System;
using System.IO;
using System.Management; // Requires NuGet package: System.Management
using System.Text;

namespace DiskInfoExporter
{
    class Program
    {
        static void Main(string[] csArgs)
        {
            // Define the output path for the CSV file
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string csvFilePath = Path.Combine(desktopPath, "HardDriveInventory.csv");

            Console.WriteLine("Scanning physical disk drives via WMI...");

            try
            {
                // StringBuilder to efficiently compile the CSV content in memory
                StringBuilder csvContent = new StringBuilder();

                // Add the CSV header line
                csvContent.AppendLine("DeviceID,Model,SerialNumber");

                // Query the Win32_DiskDrive class for physical hard drives
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT DeviceID, Model, SerialNumber FROM Win32_DiskDrive"))
                {
                    int driveCount = 0;

                    foreach (ManagementObject wmiDevice in searcher.Get())
                    {
                        driveCount++;

                        // Retrieve WMI property values safely, handling null potentials
                        string deviceId = wmiDevice["DeviceID"]?.ToString() ?? "Unknown ID";
                        string model = wmiDevice["Model"]?.ToString() ?? "Unknown Model";
                        string serialNumber = wmiDevice["SerialNumber"]?.ToString() ?? "Unknown Serial";

                        // Sanitize values to prevent CSV formatting corruption (escape double quotes)
                        deviceId = EscapeCsvField(deviceId);
                        model = EscapeCsvField(model);
                        serialNumber = EscapeCsvField(serialNumber).Trim(); // WMI serials sometimes contain padding spaces

                        // Append row data to the CSV structure
                        csvContent.AppendLine($"\"{deviceId}\",\"{model}\",\"{serialNumber}\"");

                        // Display findings directly in the console window
                        Console.WriteLine($"\n[Drive #{driveCount}]");
                        Console.WriteLine($"  Path:   {deviceId}");
                        Console.WriteLine($"  Model:  {model}");
                        Console.WriteLine($"  Serial: {serialNumber}");
                    }

                    if (driveCount == 0)
                    {
                        Console.WriteLine("No physical disk drives detected.");
                        return;
                    }
                }

                // Write the constructed CSV dataset out to the physical file
                File.WriteAllText(csvFilePath, csvContent.ToString(), Encoding.UTF8);

                Console.WriteLine("\n==================================================");
                Console.WriteLine($"Success! Inventory exported successfully.");
                Console.WriteLine($"File Saved To: {csvFilePath}");
                Console.WriteLine("==================================================");
            }
            catch (ManagementException mex)
            {
                Console.WriteLine($"\nWMI Query Error: Run the application with administrative privileges if access is denied.\nDetails: {mex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Sanitizes text inputs to prevent syntax breakage inside standard CSV columns.
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            // Standard RFC 4180 CSV escaping: double up any internal quote characters
            return field.Replace("\"", "\"\"");
        }
    }
}
