﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace aDrumsLib
{
    public class MidiTrigger
    {
        public Pins PinNumber { get; private set; }
        public TriggerType Type { get; set; }
        public TriggerCurve Curve { get; set; }
        public TriggerCurveModification CurveModification { get; } = new TriggerCurveModification();

        public byte Pitch { get; set; }
        public byte Threshold { get; set; }
        public byte DurationThreshold { get; set; }

        public MidiTrigger() { }
        public MidiTrigger(Pins pinNumber) { PinNumber = pinNumber; }
        
        internal IEnumerable<SysExMessage> GetSysExMsg(CommandType ct)
        {
            yield return new SysExMessage(SysExMsg.PinType, ct, (byte)PinNumber, (byte)Type);
            yield return new SysExMessage(SysExMsg.PinThreshold, ct, (byte)PinNumber, Threshold);
            yield return new SysExMessage(SysExMsg.PinNote, ct, (byte)PinNumber, DurationThreshold);
            yield return new SysExMessage(SysExMsg.PinPitch, ct, (byte)PinNumber, Pitch);
            yield return new SysExMessage(SysExMsg.PinCurve, ct, GetCurveBytes().ToArray());
            yield return new SysExMessage(SysExMsg.PinCurveModifications, ct, GetCurveModificationBytes().ToArray());
        }

        internal void SetValues(SerialDevice sd)
        {
            foreach (var item in GetSysExMsg(CommandType.Set))
                sd.Send(item.ToArray());
        }

        internal void GetValues(SerialDevice sd)
        {
            foreach (var item in GetSysExMsg(CommandType.Get))
                Parse(sd.RunCommand(item));
        }

        private void Parse(SysExMessage msg)
        {
            if (msg.Values[0] != (byte)PinNumber) throw new Exception($"Pin mismatch {PinNumber} not equal to {msg.Values[1]}");
            var c = (SysExMsg)(msg.Command >> 1);
            switch (c)
            {
                case SysExMsg.PinType:
                    Type = (TriggerType)msg.Values[1];
                    break;
                case SysExMsg.PinThreshold:
                    Threshold = msg.Values[1];
                    break;
                case SysExMsg.PinNote:
                    DurationThreshold = msg.Values[1];
                    break;
                case SysExMsg.PinPitch:
                    Pitch = msg.Values[1];
                    break;
                case SysExMsg.PinCurve:
                    Curve = new TriggerCurve(msg.Values.Skip(1).ToArray());
                    break;
                case SysExMsg.PinCurveModifications:
                    CurveModification.ClearAndSetFromBytes(msg.Values.Skip(1).ToArray());
                    break;
                default:
                    throw new Exception ($"Trigger Parse Error: Command '{msg.Command}' not valid");
            }
        }

        private IEnumerable<byte> GetCurveBytes()
        {
            yield return (byte) PinNumber;

            if (Curve == null)
                yield break;

            foreach (var b in Curve.GetBytes())
                yield return b;
        }

        private IEnumerable<byte> GetCurveModificationBytes()
        {
            yield return (byte) PinNumber;

            if (CurveModification == null)
            {
                yield return 0;
                yield break;
            }
            
            foreach (var b in CurveModification.GetBytes())
                yield return b;
        }
    }

}
