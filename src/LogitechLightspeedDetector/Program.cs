using System;
using System.Collections.Generic;

namespace LogitechLightspeedDetector
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            List<string> lines = new();
            foreach (var device in LogitechLightspeedDetector.DetectDongle())
            {
                lines.Add("Found Logitech Device from dongle:");
                lines.Add($"Name: {device.DeviceName}");
                lines.Add($"Type: {device.LogitechDeviceType}");
                lines.Add($"Wireless Index: {device.DeviceIndex}");
                lines.Add($"Led count: {device.LedCount}");
            }

            foreach (var device in LogitechLightspeedDetector.DetectPowerplay())
            {
                lines.Add("Found Logitech Device from powerplay:");
                lines.Add($"Name: {device.DeviceName}");
                lines.Add($"Type: {device.LogitechDeviceType}");
                lines.Add($"Wireless Index: {device.DeviceIndex}");
                lines.Add($"Led count: {device.LedCount}");
            }

            foreach (var device in LogitechLightspeedDetector.DetectG915())
            {
                lines.Add("Found Logitech Device from G915 detector:");
                lines.Add($"Name: {device.DeviceName}");
                lines.Add($"Type: {device.LogitechDeviceType}");
                lines.Add($"Wireless Index: {device.DeviceIndex}");
                lines.Add($"Led count: {device.LedCount}");
            }

            foreach (var device in LogitechLightspeedDetector.DetectG733())
            {
                lines.Add("Found Logitech Device from G733 detector:");
                lines.Add($"Name: {device.DeviceName}");
                lines.Add($"Type: {device.LogitechDeviceType}");
                lines.Add($"Wireless Index: {device.DeviceIndex}");
                lines.Add($"Led count: {device.LedCount}");
            }

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            System.IO.File.WriteAllLines("Output.txt", lines);
            Console.WriteLine("Wrote to file");
        }
    }
}
