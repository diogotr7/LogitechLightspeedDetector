using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LogitechLightspeedDetector
{
    public class LogitechDevice
    {
        private const byte LOGITECH_CMD_DEVICE_NAME_TYPE_GET_COUNT = 0x01;
        private const byte LOGITECH_CMD_DEVICE_NAME_TYPE_GET_DEVICE_NAME = 0x11;
        private const byte LOGITECH_CMD_DEVICE_NAME_TYPE_GET_TYPE = 0x21;
        private const byte LOGITECH_HIDPP_PAGE_DEVICE_NAME_TYPE = 0x0005;

        private const byte LOGITECH_HIDPP_PAGE_ROOT_IDX = 0x00;
        private const byte LOGITECH_HIDPP_PAGE_FEATURE_SET = 0x0001;
        private const byte LOGITECH_CMD_ROOT_GET_FEATURE = 0x01;
        private const byte LOGITECH_CMD_FEATURE_SET_GET_COUNT = 0x01;
        private const byte LOGITECH_CMD_FEATURE_SET_GET_ID = 0x11;

        private const byte LOGITECH_CMD_RGB_EFFECTS_GET_COUNT = 0x00;
        private const byte LOGITECH_CMD_RGB_EFFECTS_GET_INFO = 0x10;
        private const byte LOGITECH_CMD_RGB_EFFECTS_GET_CONTROL = 0x20;
        private const byte LOGITECH_CMD_RGB_EFFECTS_SET_CONTROL = 0x30;
        private const byte LOGITECH_CMD_RGB_EFFECTS_GET_STATE = 0x40;
        private const byte LOGITECH_CMD_RGB_EFFECTS_SET_STATE = 0x50;
        private const byte LOGITECH_CMD_RGB_EFFECTS_GET_CONFIG = 0x60;
        private const byte LOGITECH_CMD_RGB_EFFECTS_SET_CONFIG = 0x70;
        private const byte LOGITECH_CMD_RGB_EFFECTS_UNKNOWN = 0x80;

        private static readonly ushort[] logitech_RGB_pages =
        {
            0x8070,
            0x8071
        };

        public Dictionary<byte, HidDevice> Usages { get; } = new();
        public Dictionary<ushort, byte> Features { get; } = new();
        public byte DeviceIndex { get; private set; }
        public byte RgbFeatureIndex { get; private set; }
        public uint LogitechDeviceType { get; private set; }
        public bool Wireless { get; private set; }
        public string DeviceName { get; private set; }
        public byte LedCount { get; private set; }

        public LogitechDevice(Dictionary<byte, HidDevice> usages, byte deviceIndex, bool wireless)
        {
            DeviceIndex = deviceIndex;
            Usages = usages;
            Wireless = wireless;
            RgbFeatureIndex = 0;
            LedCount = 0;

            GetDeviceInfo();

            foreach (var item in logitech_RGB_pages)
            {
                var featureIndex = GetFeatureIndex(item);
                if (featureIndex > 0)
                {
                    Features.Add(item, featureIndex);
                    RgbFeatureIndex = featureIndex;
                }
            }

            if (RgbFeatureIndex == 0)
            {
                GetDeviceFeatureList();
            }
            else
            {
                GetRgbConfiguration();
            }
        }

        private void GetDeviceFeatureList()
        {
            if (!Usages.TryGetValue(1, out var device1))
                throw new Exception();

            if (!Usages.TryGetValue(2, out var device2))
                throw new Exception();

            if (!device1.TryOpen(out var deviceStream1))
                throw new Exception();

            if (!device2.TryOpen(out var deviceStream2))
                throw new Exception();

            var response = new FapResponse();
            var getIndexRequest = new FapShortRequest();

            getIndexRequest.Init(DeviceIndex, LOGITECH_HIDPP_PAGE_ROOT_IDX);
            getIndexRequest.FeatureCommand = LOGITECH_CMD_ROOT_GET_FEATURE;
            getIndexRequest.Data0 = LOGITECH_HIDPP_PAGE_FEATURE_SET >> 8;
            getIndexRequest.Data1 = LOGITECH_HIDPP_PAGE_FEATURE_SET & 0xFF;

            deviceStream1.Write(getIndexRequest.AsSpan());
            deviceStream2.Read(response.AsSpan());

            byte featureIndex = response.Data00;

            var getCountRequest = new FapShortRequest();
            getCountRequest.Init(DeviceIndex, featureIndex);
            getCountRequest.FeatureCommand = LOGITECH_CMD_FEATURE_SET_GET_COUNT;

            deviceStream1.Write(getCountRequest.AsSpan());
            deviceStream2.Read(response.AsSpan());
            var featureCount = response.Data00;

            var getFeaturesRequest = new FapShortRequest();
            getFeaturesRequest.Init(DeviceIndex, featureIndex);
            getFeaturesRequest.FeatureCommand = LOGITECH_CMD_FEATURE_SET_GET_ID;

            for (byte i = 0; Features.Count < featureCount; i++)
            {
                getFeaturesRequest.Data0 = i;
                deviceStream1.Write(getCountRequest.AsSpan());
                deviceStream2.Read(response.AsSpan());
                Features.Add((ushort)((response.Data00 << 8) | response.Data01), i);
            }

            deviceStream1.Dispose();
            deviceStream2.Dispose();
        }

        private void GetDeviceInfo()
        {
            if (!Usages.TryGetValue(2, out var device))
                throw new Exception();

            if (!device.TryOpen(out var deviceStream))
                throw new Exception();

            var response = new FapResponse();

            if (!Features.TryGetValue(LOGITECH_HIDPP_PAGE_DEVICE_NAME_TYPE, out var nameFeatureIndex))
            {
                FapLongRequest getIndex = new();
                getIndex.Init(DeviceIndex, LOGITECH_HIDPP_PAGE_ROOT_IDX, LOGITECH_CMD_ROOT_GET_FEATURE);
                getIndex.Data00 = LOGITECH_HIDPP_PAGE_DEVICE_NAME_TYPE >> 8;            //Get feature index of the Feature Set 0x0001
                getIndex.Data01 = LOGITECH_HIDPP_PAGE_DEVICE_NAME_TYPE & 0xFF;

                deviceStream.Write(getIndex.AsSpan());
                deviceStream.Read(response.AsSpan());
                nameFeatureIndex = response.Data00;

                Features.Add(LOGITECH_HIDPP_PAGE_DEVICE_NAME_TYPE, nameFeatureIndex);
            }

            var getLength = new FapLongRequest();
            getLength.Init(DeviceIndex, nameFeatureIndex, LOGITECH_CMD_DEVICE_NAME_TYPE_GET_COUNT);
            deviceStream.Write(getLength.AsSpan());
            deviceStream.Read(response.AsSpan());

            var nameLength = response.Data00;

            var getNameRequest = new FapLongRequest();
            getNameRequest.Init(DeviceIndex, nameFeatureIndex, LOGITECH_CMD_DEVICE_NAME_TYPE_GET_DEVICE_NAME);

            var deviceNameBuilder = new StringBuilder();
            while (deviceNameBuilder.Length < nameLength)//null term
            {
                getNameRequest.Data00 = (byte)deviceNameBuilder.Length;
                deviceStream.Write(getNameRequest.AsSpan());
                deviceStream.Read(response.AsSpan());
                deviceNameBuilder.Append(Encoding.UTF8.GetString(response.AsSpan()[4..]));
            }
            DeviceName = deviceNameBuilder.ToString().TrimEnd('\0');

            getNameRequest.Init(DeviceIndex, nameFeatureIndex, LOGITECH_CMD_DEVICE_NAME_TYPE_GET_TYPE);
            deviceStream.Write(getNameRequest.AsSpan());
            deviceStream.Read(response.AsSpan());
            LogitechDeviceType = response.Data00;

            deviceStream.Dispose();
        }

        private void GetRgbConfiguration()
        {
            if (!Usages.TryGetValue(2, out var device))
                throw new Exception();

            if (!device.TryOpen(out var deviceStream))
                throw new Exception();

            FapResponse response = new();

            FapLongRequest getCount = new();
            getCount.Init(DeviceIndex, RgbFeatureIndex, LOGITECH_CMD_RGB_EFFECTS_GET_COUNT);

            deviceStream.Write(getCount.AsSpan());
            deviceStream.Read(response.AsSpan());

            LedCount = response.Data00;

            deviceStream.Dispose();
        }

        private byte GetFeatureIndex(ushort featurePage)
        {
            if (!Usages.TryGetValue(2, out var device))
                throw new Exception();

            if (!device.TryOpen(out var deviceStream))
                throw new Exception();

            var response = new FapResponse();
            var getIndexRequest = new FapLongRequest();
            getIndexRequest.Init(DeviceIndex, LOGITECH_HIDPP_PAGE_ROOT_IDX, LOGITECH_CMD_ROOT_GET_FEATURE);
            getIndexRequest.Data00 = (byte)(featurePage >> 8);
            getIndexRequest.Data01 = (byte)(featurePage & 0xFF);

            deviceStream.Write(getIndexRequest.AsSpan());
            deviceStream.Read(response.AsSpan());

            deviceStream.Dispose();

            return response.Data00;
        }
    }
}
