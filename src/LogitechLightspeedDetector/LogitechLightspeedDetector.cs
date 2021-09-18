using HidSharp;
using System.Collections.Generic;
using System.Linq;

namespace LogitechLightspeedDetector
{
    public static class LogitechLightspeedDetector
    {
        private const int LOGITECH_VID = 0x046D;
        private const int LOGITECH_G_LIGHTSPEED_RECEIVER_PID = 0xC539;
        private const int LOGITECH_G_LIGHTSPEED_POWERPLAY_PID = 0xC53A;
        private const int LOGITECH_G_LIGHTSPEED_G915_PID = 0xC541;
        private const int LOGITECH_G_LIGHTSPEED_G733_PID = 0x0AB5;

        public static IEnumerable<LogitechDevice> DetectAll()
        {
            List<IEnumerable<LogitechDevice>> enumerables = new()
            {
                DetectDongle(),
                DetectPowerplay(),
                DetectG915(),
                DetectG733()
            };

            foreach (IEnumerable<LogitechDevice> element in enumerables)
            {
                foreach (LogitechDevice subelement in element)
                {
                    yield return subelement;
                }
            }
        }

        public static IEnumerable<LogitechDevice> DetectDongle()
        {
            return Detect(LOGITECH_G_LIGHTSPEED_RECEIVER_PID);
        }

        public static IEnumerable<LogitechDevice> DetectPowerplay()
        {
            return Detect(LOGITECH_G_LIGHTSPEED_POWERPLAY_PID);
        }

        public static IEnumerable<LogitechDevice> DetectG915()
        {
            return Detect(LOGITECH_G_LIGHTSPEED_G915_PID);
        }

        public static IEnumerable<LogitechDevice> DetectG733()
        {
            return Detect(LOGITECH_G_LIGHTSPEED_G733_PID);
        }

        private static IEnumerable<LogitechDevice> Detect(int pid)
        {
            var receiverDevices = DeviceList.Local.GetHidDevices(LOGITECH_VID, pid).ToList();
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
