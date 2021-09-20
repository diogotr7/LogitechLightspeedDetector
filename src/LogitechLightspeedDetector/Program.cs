using System;
using System.IO;
using System.Collections.Generic;

namespace LogitechLightspeedDetector
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            List<string> lines = new();
            foreach (var device in LogitechLightspeedDetector.Detect())
            {
                lines.AddRange(GetDeviceInfo(device));
            }

            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            File.WriteAllLines("Output.txt", lines);
            Console.WriteLine("Wrote to file");
        }

        public static IEnumerable<string> GetDeviceInfo(LogitechDevice device)
        {
            yield return $"Name: {device.DeviceName}";
            yield return $"Type: {device.LogitechDeviceType}";
            yield return $"Dongle PID: 0x{device.DonglePid:X4}";
            yield return $"Wireless PID: 0x{device.WirelessPid:X4}";
            yield return $"Wireless Index: {device.DeviceIndex}";
            yield return $"Led count: {device.LedCount}";
            yield return $"RGB Feature Index: {device.RgbFeatureIndex}";
        }
    }
}
