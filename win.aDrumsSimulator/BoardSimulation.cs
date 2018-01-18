using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using aDrumsLib;

namespace win.aDrumsSimulator
{
    internal class BoardSimulation
    {
        public const byte MaxPinCount = 37;
        public readonly Queue<byte[]> Answers = new Queue<byte[]>();
        private static readonly Random Rand = new Random();
        
        private byte[] _pinType = new byte[MaxPinCount];
        private byte[] _pinThreshold = new byte[MaxPinCount];
        private byte[] _pinNoteOnThreshold = new byte[MaxPinCount];
        private byte[] _pinPitch = new byte[MaxPinCount];

        private readonly Version _currentVersion;
        private readonly string _fileName = "aDrumSimulator.eeprom";

        public BoardSimulation()
        {
            _currentVersion =
                new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
            GetFromEeprom(_fileName, _currentVersion);
        }

        internal void ExecuteCommand(SysExMessage msg)
        {
            bool isSet = (msg.Command & 1) > 0;
            SysExMsg cmd = (SysExMsg)(msg.Command >> 1);
            Program.Log($"{(isSet ? "SET" : "GET")} {cmd} " + msg.Values.Aggregate(string.Empty,
                            (s, b) => s + b.ToString("X") + " | ", s => s.TrimEnd(' ', '|')));
            switch(cmd)
            {
                case SysExMsg.MSG_GET_HANDSHAKE:
                    Answers.Enqueue(SerialMessage(msg.Command, (byte) _currentVersion.Major, (byte) _currentVersion.Minor));
                    break;
                case SysExMsg.MSG_GET_PINCOUNT:
                    Answers.Enqueue(SerialMessage(msg.Command, MaxPinCount, 0));
                    break;
                case SysExMsg.MSG_GET_PINVALUE:
                    byte pin = msg.Values[0];

                    Answers.Enqueue(pin == byte.MaxValue
                        ? SerialMessage(msg.Command, pin,
                            Enumerable.Range(0, MaxPinCount).Select(x => GetPinValue((byte) x)).ToArray())
                        : SerialMessage(msg.Command, pin, GetPinValue(pin)));
                    break;
                case SysExMsg.MSG_EEPROM:
                    if (isSet)
                        SaveToEeprom(_fileName, _currentVersion);
                    else
                        GetFromEeprom(_fileName, _currentVersion);
                    break;
                case SysExMsg.MSG_pinType:
                    SetOrGetArrayValue(isSet, ref _pinType, msg.Command, msg.Values);
                    break;
                case SysExMsg.MSG_pinNoteOnThreshold:
                    SetOrGetArrayValue(isSet, ref _pinNoteOnThreshold, msg.Command, msg.Values);
                    break;
                case SysExMsg.MSG_pinThreshold:
                    SetOrGetArrayValue(isSet, ref _pinThreshold, msg.Command, msg.Values);
                    break;
                case SysExMsg.MSG_pinPitch:
                    SetOrGetArrayValue(isSet, ref _pinPitch, msg.Command, msg.Values);
                    break;
            }
        }

        private void GetFromEeprom(string fileName, Version currentVersion)
        {
            if (!File.Exists(fileName)) return;
            using (var fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                byte versionMajor = (byte) fs.ReadByte();
                byte versionMinor = (byte) fs.ReadByte();
                byte pinCount = (byte) fs.ReadByte();
                if (currentVersion.Major != versionMajor || currentVersion.Minor != versionMinor ||
                    pinCount != MaxPinCount)
                    //TODO DG.PPV.DEM 18.01.2018: msg
                    return;

                for (int i = 0; i < MaxPinCount; i++)
                    _pinType[i] = (byte) fs.ReadByte();
                for (int i = 0; i < MaxPinCount; i++)
                    _pinPitch[i] = (byte) fs.ReadByte();
                for (int i = 0; i < MaxPinCount; i++)
                    _pinThreshold[i] = (byte) fs.ReadByte();
                for (int i = 0; i < MaxPinCount; i++)
                    _pinNoteOnThreshold[i] = (byte) fs.ReadByte();
            }
        }

        private void SaveToEeprom(string fileName, Version currentVersion)
        {
            using (var fs = File.Open(fileName, FileMode.Create, FileAccess.Write))
            {
                foreach (var b in GetEepromBytes(currentVersion))
                    fs.WriteByte(b);
            }
        }

        private IEnumerable<byte> GetEepromBytes(Version currentVersion)
        {
            yield return (byte) currentVersion.Major;
            yield return (byte) currentVersion.Minor;
            yield return MaxPinCount;
            foreach (var b in _pinType)
                yield return b;
            foreach (var b in _pinPitch)
                yield return b;
            foreach (var b in _pinThreshold)
                yield return b;
            foreach (var b in _pinNoteOnThreshold)
                yield return b;
        }

        private byte GetPinValue(byte pin)
        {
            if (pin >= MaxPinCount || _pinType[pin] == (byte) TriggerType.Disabled)
                return byte.MinValue;
            byte value = (byte) Rand.Next(byte.MinValue, byte.MaxValue);

            while (value == SysExMessage.START_SYSEX || value == SysExMessage.END_SYSEX)
                value = (byte) Rand.Next(byte.MinValue, byte.MaxValue);

            return value;
        }

        private void SetOrGetArrayValue(bool set, ref byte[] array, byte command, byte[] values)
        {
            if (set)
                array[values[0]] = values[1];
            else
                Answers.Enqueue(SerialMessage(command, values[0], array[values[0]]));
        }

        private static byte[] SerialMessage(byte command, byte pin, byte value)
            => SerialMessage(command, pin, new[] {value});

        private static byte[] SerialMessage(byte command, byte pin, IEnumerable<byte> values)
            => new SysExMessage(new[]
            {
                command,
                pin
            }.Concat(values)).ToArray();
    }
}
