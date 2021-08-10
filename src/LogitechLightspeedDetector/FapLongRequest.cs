using System.Runtime.InteropServices;

namespace LogitechLightspeedDetector
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 20)]
    public struct FapLongRequest
    {
        private const int LOGITECH_LONG_MESSAGE = 0x11;

        public byte ReportId;
        public byte DeviceIndex;
        public byte FeatureIndex;
        public byte FeatureCommand;
        public byte Data00;
        public byte Data01;
        public byte Data02;
        public byte Data03;
        public byte Data04;
        public byte Data05;
        public byte Data06;
        public byte Data07;
        public byte Data08;
        public byte Data09;
        public byte Data10;
        public byte Data11;
        public byte Data12;
        public byte Data13;
        public byte Data14;
        public byte Data15;

        public void Init(byte device_index, byte feature_index, byte feature_command)
        {
            ReportId = LOGITECH_LONG_MESSAGE;
            DeviceIndex = device_index;
            FeatureIndex = feature_index;
            FeatureCommand = feature_command;
            Data00 = 0;
            Data01 = 0;
            Data02 = 0;
            Data03 = 0;
            Data04 = 0;
            Data05 = 0;
            Data06 = 0;
            Data07 = 0;
            Data08 = 0;
            Data09 = 0;
            Data10 = 0;
            Data11 = 0;
            Data12 = 0;
            Data13 = 0;
            Data14 = 0;
            Data15 = 0;
        }
    }
}
