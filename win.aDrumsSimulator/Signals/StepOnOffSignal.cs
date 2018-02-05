using System;

namespace win.aDrumsSimulator.Signals
{
    internal class StepOnOffSignal : OnOffSignal
    {
        private readonly byte _step;
        private readonly byte _maxValue;
        private readonly byte _minValue;
        private byte _currentStep;
        private bool _gettingOnValue;

        public StepOnOffSignal(TimeSpan onDuration, TimeSpan offDuration, byte step, byte maxValue = 150,
            byte minValue = byte.MinValue, byte offValue = byte.MinValue) : base(onDuration, offDuration, null, null)
        {
            _step = step;
            _maxValue = maxValue;
            _minValue = minValue;
            _currentStep = maxValue;
            OnValueGetter = () =>
            {
                if (!_gettingOnValue)
                {
                    _gettingOnValue = true;
                    _currentStep = GetNextStep();
                }

                return _currentStep;
            };
            OffValueGetter = () =>
            {
                _gettingOnValue = false;
                return offValue;
            };
        }

        private byte GetNextStep()
        {
            int nextStep = _currentStep + _step;
            if (nextStep > _maxValue)
                _currentStep = _minValue;
            else
                _currentStep = (byte) nextStep;
            return _currentStep;
        }
    }
}
