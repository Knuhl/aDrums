using System;

namespace win.aDrumsSimulator.Signals
{
    internal class OnOffSignal : SignalGenerator
    {
        protected TimeSpan OnDuration;
        protected Func<byte> OnValueGetter;
        protected Func<byte> OffValueGetter;
        protected long TotalTicks;

        public OnOffSignal(TimeSpan onDuration, TimeSpan offDuration, Func<byte> onValueGetter)
            : this(onDuration, offDuration, onValueGetter, () => 0)
        {
        }

        public OnOffSignal(TimeSpan onDuration, TimeSpan offDuration, byte onValue = 150, byte offValue = byte.MinValue)
            : this(onDuration, offDuration, () => onValue, () => offValue)
        {
        }

        public OnOffSignal(TimeSpan onDuration, TimeSpan offDuration, Func<byte> onValueGetter, Func<byte> offValueGetter)
        {
            OnDuration = onDuration;
            TotalTicks = OnDuration.Ticks + offDuration.Ticks;
            OnValueGetter = onValueGetter;
            OffValueGetter = offValueGetter;
        }
        
        internal override byte GenerateValue()
            => CoerceValue((DateTime.Now.Ticks - CreationTime.Ticks) % TotalTicks <= OnDuration.Ticks
                ? OnValueGetter()
                : OffValueGetter());
    }
}
