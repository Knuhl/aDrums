using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using aDrumsLib;
using Prism.Events;
using Prism.Mvvm;
using win.WPF.aDrumsManager.Events;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class MidiTriggerViewModel : ViewModelBase
    {
        private readonly DrumManager _drumManager;
        private readonly IEventAggregator _eventAggregator;
        public MidiTrigger Trigger { get; }
        
        private ObservableCollection<TriggerType> _triggerTypes;
        public ObservableCollection<TriggerType> TriggerTypes =>
            _triggerTypes ?? (_triggerTypes =
                new ObservableCollection<TriggerType>(Enum.GetValues(typeof(TriggerType)).Cast<TriggerType>()));

        public TriggerType TriggerType
        {
            get { return Trigger.Type; }
            set
            {
                bool wasInactive = Trigger.Type == TriggerType.Disabled;
                Trigger.Type = value;
                _drumManager.SaveSettings();
                if (wasInactive != (value == TriggerType.Disabled))
                    _eventAggregator.GetEvent<TriggerActiveChangedEvent>()
                        .Publish(new KeyValuePair<Pins, bool>(Trigger.PinNumber, wasInactive));
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<CurveType> _curveTypes;
        public ObservableCollection<CurveType> CurveTypes =>
            _curveTypes ?? (_curveTypes =
                new ObservableCollection<CurveType>(Enum.GetValues(typeof(CurveType)).Cast<CurveType>()));

        public MidiTriggerViewModel(DrumManager drumManager, MidiTrigger trigger, IEventAggregator eventAggregator)
        {
            _drumManager = drumManager;
            _eventAggregator = eventAggregator;
            Trigger = trigger;
        }
    }
}
