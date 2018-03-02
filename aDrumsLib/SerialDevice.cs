/*
 * Modified version of: Firmata.Net
 * Arduino.cs - Arduino/firmata library for Visual C# .NET Copyright (C) 2009 Tim Farley
 */
using System;
using System.Collections.Generic;

namespace aDrumsLib
{
    /**
     * Together with the Firmata 2 firmware (an Arduino sketch uploaded to the
     * Arduino board), this class allows you to control the Arduino board from
     * Processing: reading from and writing to the digital pins and reading the
     * analog inputs.
     */
    internal class SerialDevice
    {
        private readonly ISerialPort _sp = null;

        private const int BAUDRATE = 115200;

        public Version aDrumVersion { get; private set; }

        public string PortName
        {
            get
            {
                return _sp.PortName;
            }
            set
            {
                _sp.PortName = value;
            }
        }
        public bool IsOpen => _sp.IsOpen;

        public SerialDevice(ISerialPort serialPort)
        {
            _sp = serialPort;
            _sp.BaudRate = BAUDRATE;
            _sp.DtrEnable = true;
            _sp.Open();

            var r = RunCommand(SysExMsg.Handshake, CommandType.Get);
            if (r.Values.Length != 2) throw new Exception($"Not a valid aDrum response in {serialPort.PortName}");
            aDrumVersion = new Version(r.Values[0], r.Values[1]);

        }

        public void Send(params byte[] message)
        {
            _sp.Write(message, 0, message.Length);
        }

        private void SendSysExMsg(SysExMessage sysEx)
        {
            Send(sysEx.ToArray());
        }

        private byte[] ReadExistingBytes()
        {
            var bytes = new byte[_sp.BytesToRead];
            if (_sp.BytesToRead > 1) _sp.Read(bytes, 0, _sp.BytesToRead);
            return bytes;
        }

        internal SysExMessage RunCommand(SysExMsg command, CommandType type, params byte[] values)
        {
            return RunCommand(new SysExMessage(command, type, values));
        }

        internal SysExMessage RunCommand(SysExMessage msg, int timeout = 5)
        {
            if (_sp.BytesToRead != 0) //clear the buffer from any sent bytes previously
                _sp.ReadExisting();
            Send(msg.ToArray());
            var r = ReadSysEx(timeout);
            if (r.Command != msg.Command)
                throw new ArrayTypeMismatchException($"Command Mismatch. Command Sent: '{msg.Command}', Command Read: '{r.Command}'");
            return r;
        }

        private SysExMessage ReadSysEx(int timeoutSeconds)
        {
            return new SysExMessage(ReceiveSysEx(timeoutSeconds));
        }

        private byte[] ReceiveSysEx(int timeoutSeconds)
        {
            var storedInputData = new List<byte>();
            bool parsingSysex = false;
            var startTime = DateTime.UtcNow.Ticks;
            
            _sp.ReadTimeout = timeoutSeconds * 1000;

            while (new TimeSpan(DateTime.UtcNow.Ticks - startTime).Seconds < timeoutSeconds)
            {
                lock (this)
                {
                    int inputData = _sp.ReadByte();
                    if (parsingSysex)
                        if (inputData == SysExMessage.END_SYSEX)
                            return storedInputData.ToArray();
                        else
                            storedInputData.Add((byte)inputData);
                    else if (inputData == SysExMessage.START_SYSEX)
                        parsingSysex = true;
                }
            }

            throw new TimeoutException();
        }
        
        public static string[] GetPortNames() => Factory.GetPortNames();

        public void Close()
        {
            _sp.Close();
        }

        ~SerialDevice()
        {
            if (_sp.IsOpen) _sp.Close();
        }

    } // End class




} // End namespace