using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace aDrumsLib
{
    sealed class SimulatedSerialPort : ISerialPort
    {
        public const string SimulatedSerialPortName = "COM_SIMULATED";
        public const byte MAX_PIN_COUNT = 37;

        private readonly Queue<byte[]> _answers = new Queue<byte[]>();
        private byte[] _currentlyReadAnswer;
        private int _currentReadOffset;

        private byte[] _pinType = new byte[MAX_PIN_COUNT];
        private byte[] _pinThreshold = new byte[MAX_PIN_COUNT];
        private byte[] _pinNoteOnThreshold = new byte[MAX_PIN_COUNT];
        private byte[] _pinPitch = new byte[MAX_PIN_COUNT];

        public int BaudRate { get; set; }

        public int BytesToRead { get; set; }

        public bool DtrEnable { get; set; }

        public bool IsOpen { get; set; }

        public string PortName { get; set; } = SimulatedSerialPortName;

        public int ReadTimeout { get; set; } = 30;

        private static readonly Random Rand = new Random();

        public void Close()
        {
            IsOpen = false;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Rand.Next();
        }

        public int ReadByte()
        {
            if (_currentlyReadAnswer == null)
            {
                _currentlyReadAnswer = _answers.Dequeue();
                _currentReadOffset = 0;
            }
            if (_currentlyReadAnswer == null)
                return -1;
            int result = _currentlyReadAnswer[_currentReadOffset++];

            if (_currentReadOffset >= _currentlyReadAnswer.Length)
                _currentlyReadAnswer = null;

            return result;
        }

        public string ReadExisting()
        {
            return Rand.Next().ToString();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            Debug.WriteLine("SerialPort Received message");
            SysExMessage msg = new SysExMessage(buffer.Skip(offset + 1).Take(count - 2));
            ExecuteCommand(msg);
        }

        private void ExecuteCommand(SysExMessage msg)
        {
            bool isSet = (msg.Command & 1) > 0;
            SysExMsg cmd = (SysExMsg)(msg.Command >> 1);
            switch(cmd)
            {
                case SysExMsg.MSG_GET_HANDSHAKE:
                    _answers.Enqueue(SerialMessage(msg.Command, 1, 0));
                    break;
                case SysExMsg.MSG_GET_PINCOUNT:
                    _answers.Enqueue(SerialMessage(msg.Command, MAX_PIN_COUNT, 0));
                    break;
                case SysExMsg.MSG_GET_PINVALUE:
                    byte pin = msg.Values[0];
                    int pinValue = GetPinValue(pin);
                    byte pinValueByte = pinValue < byte.MinValue ? byte.MinValue :
                        pinValue > byte.MaxValue ? byte.MaxValue : (byte) pinValue;
                    _answers.Enqueue(SerialMessage(msg.Command, pin, pinValueByte));
                    break;
                case SysExMsg.MSG_EEPROM:

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

        private int GetPinValue(byte pin)
        {
            if (pin >= MAX_PIN_COUNT || _pinType[pin] == (byte) TriggerType.Disabled)
                return -1;
            return Rand.Next(byte.MinValue, byte.MaxValue);
        }

        private void SetOrGetArrayValue(bool set, ref byte[] array, byte command, byte[] values)
        {
            if (set)
                array[values[0]] = values[1];
            else
                _answers.Enqueue(SerialMessage(command, values[0], array[values[0]]));
        }

        private static byte[] SerialMessage(byte command, byte pin, byte value)
        {
            return new SysExMessage(new[]
            {
                command,
                pin,
                value
            }).ToArray();
        }
    }
}
