using HidSharp;
using System.Collections.Generic;
using System.Linq;

namespace LogitechLightspeedDetector
{
    public static class LogitechLightspeedDetector
    {
        public static IEnumerable<LogitechDevice> Discover()
        {
            const int LOGITECH_VID = 0x046D;
            const int LOGITECH_G_LIGHTSPEED_RECEIVER_PID = 0xC539;

            var receiverDevices = DeviceList.Local.GetHidDevices(LOGITECH_VID, LOGITECH_G_LIGHTSPEED_RECEIVER_PID).ToList();
            var interfaceTwo = receiverDevices.Where(d => d.DevicePath.Contains("mi_02")).ToList();
            //this is terrible but i don't know how else to filter interfaces

            Dictionary<byte, HidDevice> deviceUsages = new();
            foreach (var item in interfaceTwo)
            {
                deviceUsages.Add((byte)item.GetUsage(), item);
            }

            foreach (var item in GetWirelessDevices(deviceUsages))
            {
                yield return new LogitechDevice(deviceUsages, item.Value, true);
            }
        }

        private static Dictionary<uint, byte> GetWirelessDevices(Dictionary<byte, HidDevice> device_usages)
        {
            const byte LOGITECH_RECEIVER_ADDRESS = 0xFF;
            const byte LOGITECH_SET_REGISTER_REQUEST = 0x80;
            const byte LOGITECH_GET_REGISTER_REQUEST = 0x81;

            Dictionary<uint, byte> map = new();

            if (device_usages.TryGetValue(1, out var device))
            {
                var stream = device.Open();

                var response = new FapResponse();

                var getConnectedDevices = new FapShortRequest();
                getConnectedDevices.Init(LOGITECH_RECEIVER_ADDRESS, LOGITECH_GET_REGISTER_REQUEST);

                stream.Write(getConnectedDevices.AsSpan());
                stream.Read(response.AsSpan());

                bool wireless_notifications = (response.Data01 & 1) == 1;
                if (!wireless_notifications)
                {
                    response = new FapResponse();

                    getConnectedDevices.Init(LOGITECH_RECEIVER_ADDRESS, LOGITECH_SET_REGISTER_REQUEST);
                    getConnectedDevices.Data1 = 1;

                    stream.Write(getConnectedDevices.AsSpan());
                    stream.Read(response.AsSpan());

                    if (getConnectedDevices.FeatureIndex == 0x8f)
                    {
                        //error??
                    }
                }

                response = new FapResponse();

                getConnectedDevices.Init(LOGITECH_RECEIVER_ADDRESS, LOGITECH_GET_REGISTER_REQUEST);
                getConnectedDevices.FeatureCommand = 0x02;

                stream.Write(getConnectedDevices.AsSpan());
                stream.Read(response.AsSpan());

                int deviceCount = response.Data01;
                if (deviceCount > 0)
                {
                    //log "Faking a reconnect to get device list"
                    deviceCount++;

                    response = new FapResponse();
                    getConnectedDevices.Init(LOGITECH_RECEIVER_ADDRESS, LOGITECH_SET_REGISTER_REQUEST);
                    getConnectedDevices.FeatureCommand = 0x02;
                    getConnectedDevices.Data0 = 0x02;
                    stream.Write(getConnectedDevices.AsSpan());

                    for (int i = 0; i < deviceCount; i++)
                    {
                        var devices = new FapResponse();
                        stream.Read(devices.AsSpan());
                        uint wirelessPid = (uint)((devices.Data02 << 8) | devices.Data01);
                        if (devices.DeviceIndex != 0xff)
                        {
                            map.Add(wirelessPid, devices.DeviceIndex);
                        }
                    }
                }
            }

            return map;
        }
    }
}
