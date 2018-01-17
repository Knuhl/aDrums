using System;
using System.Collections.Generic;

namespace aDrumsLib
{
    public class DrumManager : IDisposable
    {
        public ISerialPort SerialPort { get; }

        private SerialDevice SerialD { get; set; }
        
        public string Jacks { get; private set; }

        public Version FW_Version
        {
            get
            {
                return SerialD != null ? SerialD.aDrumVersion : null;
            }
        }

        public int PinCount { get; set; }

        public List<MidiTrigger> Triggers { get; set; }

        public bool IsConnected
        {
            get
            {
                return SerialD != null;
            }
        }

        public DrumManager(string comPort) : this(Factory.GetSerialPort(comPort))
        {
        }

        public DrumManager(ISerialPort serialPort)
        {
            if (serialPort == null)
                throw new ArgumentNullException(nameof(serialPort));
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                if (serialPort.IsOpen)
                    throw new ArgumentException($"Already connected to device {SerialD.PortName}");
            }
            SerialPort = serialPort;
            Connect();
        }
        
        private void Connect()
        {
            SerialD = new SerialDevice(SerialPort);

            PinCount = SerialD.RunCommand(SysExMsg.MSG_GET_PINCOUNT, CommandType.Get).Values[0];

            LoadSettings();
        }

        private void Disconnect()
        {
            if (SerialD?.IsOpen == true) SerialD.Close();
            SerialD = null;
            Triggers = null;
        }

        public void LoadSettings()
        {
            var lTriggers = new List<MidiTrigger>();
            for (int i = 0; i < PinCount; i++)
            {
                var t = new MidiTrigger((Pins)i);
                t.getValues(SerialD);
                lTriggers.Add(t);
            }
            Triggers = lTriggers;
        }

        public void SaveSettings()
        {
            foreach (var t in Triggers)
            {
                t.setValues(SerialD);
            }
        }

        public void WriteSettingsToEEPROM()
        {
            SerialD.Send(new SysExMessage(SysExMsg.MSG_EEPROM, CommandType.Set).ToArray());
        }

        public void LoadSettingsFromEEPROM()
        {
            SerialD.Send(new SysExMessage(SysExMsg.MSG_EEPROM, CommandType.Get).ToArray());
            LoadSettings();
        }

        public byte GetPinValue(Pins pin)
        {
            var pinValue = SerialD.RunCommand(SysExMsg.MSG_GET_PINVALUE, CommandType.Get, (byte) pin).Values[1];
            return pinValue;
        }

        public void Dispose()
        {
            Disconnect();
        }

    }
}
