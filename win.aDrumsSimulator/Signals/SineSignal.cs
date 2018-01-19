using System;
using aDrumsLib;

namespace win.aDrumsSimulator.Signals
{
    internal class SineSignal : SignalGenerator
    {
        private readonly double _xDiff;
        private const double Deg = Math.PI / 180.0 / 80000.0;

        public SineSignal(Pins pin)
        {
            _xDiff = ((byte) pin) * 3000000.0;
        }

        internal override byte GenerateValue()
            => CoerceValue((byte) (Math.Sin((DateTime.Now.Ticks + _xDiff) * Deg) * 50.0 + 70.0));
    }
}
