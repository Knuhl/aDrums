using System;
using aDrumsLib;

namespace win.aDrumsSimulator.Signals
{
    internal abstract class SignalGenerator
    {
        protected readonly DateTime CreationTime;

        internal SignalGenerator()
        {
            CreationTime = DateTime.Now;
        }

        internal abstract byte GenerateValue();

        internal byte CoerceValue(byte value)
        {
            while (value == SysExMessage.START_SYSEX || value == SysExMessage.END_SYSEX)
                value--;

            return value;
        }
    }
}
