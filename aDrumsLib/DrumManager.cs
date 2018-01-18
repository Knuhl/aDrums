using System;
using System.Collections.Generic;
using System.Linq;

namespace aDrumsLib
{
    public class DrumManager : IDisposable
    {
        public ISerialPort SerialPort { get; }

        private readonly object _serialLock = new object();
        private SerialDevice SerialD { get; set; }
        
        public string Jacks { get; private set; }

        public Version FW_Version => SerialD?.aDrumVersion;

        public int PinCount { get; set; }

        public List<MidiTrigger> Triggers { get; set; }

        public bool IsConnected => SerialD != null;

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
            lock (_serialLock)
            {
                SerialD = new SerialDevice(SerialPort);

                PinCount = SerialD.RunCommand(SysExMsg.MSG_GET_PINCOUNT, CommandType.Get).Values[0];
            }
            LoadSettings();
        }

        private void Disconnect()
        {
            lock (_serialLock)
            {
                if (SerialD?.IsOpen == true) SerialD.Close();
                SerialD = null;
            }

            Triggers = null;
        }

        public void LoadSettings()
        {
            if (Triggers == null)
                Triggers = Enumerable.Range(0, PinCount).Select(i => new MidiTrigger((Pins) i)).ToList();

            foreach (var t in Triggers)
            {
                lock (_serialLock)
                    t.getValues(SerialD);
            }
        }

        public void SaveSettings()
        {
            foreach (var t in Triggers)
            {
                lock (_serialLock)
                    t.setValues(SerialD);
            }
        }

        public void WriteSettingsToEEPROM()
        {
            SaveSettings();
            lock (_serialLock)
                SerialD.Send(new SysExMessage(SysExMsg.MSG_EEPROM, CommandType.Set).ToArray());
        }

        public void LoadSettingsFromEEPROM()
        {
            lock (_serialLock)
                SerialD.Send(new SysExMessage(SysExMsg.MSG_EEPROM, CommandType.Get).ToArray());
            LoadSettings();
        }

        public byte GetPinValue(Pins pin)
        {
            lock (_serialLock)
                return SerialD.RunCommand(SysExMsg.MSG_GET_PINVALUE, CommandType.Get, (byte) pin).Values[1];
        }

        public List<byte> GetAllPinValues()
        {
            lock (_serialLock)
                return SerialD.RunCommand(SysExMsg.MSG_GET_PINVALUE, CommandType.Get, byte.MaxValue).Values.Skip(1)
                    .ToList();
        }

        public void Dispose()
        {
            Disconnect();
        }

    }
}
