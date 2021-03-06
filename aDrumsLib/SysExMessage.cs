﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace aDrumsLib
{
    public class SysExMessage
    {
        public const byte START_SYSEX = 0xF0; // start a MIDI SysEx message
        public const byte END_SYSEX = 0xF7; // end a MIDI SysEx message

        public byte Command { get; set; }

        public byte[] Values { get; set; }
        
        public SysExMessage(SysExMsg command, CommandType type, params byte[] values)
        {
            Command = (byte)((((byte)command) << 1) | (byte)type);
            Values = values;
        }

        public SysExMessage(IEnumerable<Byte> Msg)
        {
            var l = Msg.ToList();
            Command = l.First();
            l.RemoveAt(0);
            Values = l.ToArray();
            if (Values.Length < 1)
            {

            }
        }

        public SysExMessage() { }

        public byte[] ToArray()
        {
            var r = new byte[Values.Length + 3];
            r[0] = START_SYSEX;
            r[1] = Command;
            for (int i = 0; i < Values.Length; i++)
            {
                r[i + 2] = Values[i];
            }
            r[r.Length - 1] = END_SYSEX;
            return r.ToArray();
        }

    }


    #region enums

    public enum CommandType : byte
    {
        Get = 0,
        Set = 1,
    }

    public enum SysExMsg : byte
    {
        Handshake = 0,
        GetPinCount = 8,
        GetPinValue = 16,
        Eeprom = 100,
        PinType = 1,
        PinThreshold = 2,
        PinNote = 3,
        PinPitch = 4,
        PinCurve = 5,
        PinCurveModifications = 6
    }
    #endregion
}
