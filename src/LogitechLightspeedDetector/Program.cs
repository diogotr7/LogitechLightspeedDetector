using System;

namespace LogitechLightspeedDetector
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            foreach (var device in LogitechLightspeedDetector.Discover())
            {
                Console.WriteLine("Found Logitech Device:");
                Console.WriteLine($"Name: {device.DeviceName}");
                Console.WriteLine($"Type: {device.LogitechDeviceType}");
                Console.WriteLine($"Wireless Index: {device.DeviceIndex}");
                Console.WriteLine($"Led count: {device.LedCount}");
            }

            Console.ReadLine();
        }
    }
}
