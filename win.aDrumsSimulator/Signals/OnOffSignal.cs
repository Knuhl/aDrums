using System;

namespace win.aDrumsSimulator.Signals
{
    internal class OnOffSignal : SignalGenerator
    {
        private readonly TimeSpan _onDuration;
        private readonly byte _onValue;
        private readonly byte _offValue;
        private readonly long _totalTicks;

        public OnOffSignal(TimeSpan onDuration, TimeSpan offDuration, byte onValue = 150, byte offValue = byte.MinValue)
        {
            _onDuration = onDuration;
            _onValue = onValue;
            _offValue = offValue;
            _totalTicks = _onDuration.Ticks + offDuration.Ticks;
        }

        internal override byte GenerateValue()
            => CoerceValue((DateTime.Now.Ticks - CreationTime.Ticks) % _totalTicks <= _onDuration.Ticks
                ? _onValue
                : _offValue);
    }
}
