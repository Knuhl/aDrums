using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using aDrumsLib;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Practices.ObjectBuilder2;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Commands;
using Prism.Events;
using win.WPF.aDrumsManager.Events;

namespace win.WPF.aDrumsManager.ViewModels
{
    public class DrumManagerViewModel : DialogViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;
        
        private DrumManager _drumManager;
        public DrumManager DrumManager
        {
            get { return _drumManager; }
            set
            {
                _drumManager = value;
                RaisePropertyChanged();
                SaveToEepromCommand.RaiseCanExecuteChanged();
                LoadFromEepromCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<MidiTriggerViewModel> _triggerCollection;
        public ObservableCollection<MidiTriggerViewModel> TriggerCollection
        {
            get { return _triggerCollection; }
            set
            {
                _triggerCollection = value;
                RaisePropertyChanged();
            }
        }

        private readonly object _pinValuesLock = new object();
        private Dictionary<DateTime, List<byte>> _pinValues = new Dictionary<DateTime, List<byte>>();

        private MidiTriggerViewModel _selectedTrigger;
        public MidiTriggerViewModel SelectedTrigger
        {
            get { return _selectedTrigger; }
            set
            {
                _selectedTrigger = value;
                RaisePropertyChanged();
            }
        }

        private bool _plotCurrentPinValues;
        public bool PlotCurrentPinValues
        {
            get { return _plotCurrentPinValues; }
            set
            {
                if (_plotCurrentPinValues == value) return;
                _plotCurrentPinValues = value;
                RaisePropertyChanged();
                if (value)
                    CurrentValuePlot.Series.OfType<DataPointSeries>().ForEach(x => x.Points.Clear());
            }
        }
        
        public PlotModel CurrentValuePlot { get; }
        
        private DelegateCommand _saveToEepromCommand;
        public DelegateCommand SaveToEepromCommand => _saveToEepromCommand ?? (_saveToEepromCommand =
                                                          new DelegateCommand(() => DrumManager.WriteSettingsToEeprom(),
                                                              () => DrumManager != null && DrumManager.IsConnected));

        private DelegateCommand _loadFromEepromCommand;
        public DelegateCommand LoadFromEepromCommand => _loadFromEepromCommand ?? (_loadFromEepromCommand =
                                                            new DelegateCommand(LoadSettingsFromEeprom,
                                                                () => DrumManager != null && DrumManager.IsConnected));

        public DrumManagerViewModel(IDialogCoordinator dialogCoordinator, IEventAggregator eventAggregator) : base(dialogCoordinator)
        {
            _eventAggregator = eventAggregator;
            eventAggregator.GetEvent<PubSubEvent<DrumManager>>().Subscribe(OnDrumManagerChanged);
            eventAggregator.GetEvent<PubSubEvent<MidiTrigger>>().Subscribe(AddTrigger);
            eventAggregator.GetEvent<TriggerActiveChangedEvent>().Subscribe(OnTriggerActiveChanged);

            CurrentValuePlot = new PlotModel
            {
                Title = "Current Analogue Values"
            };
            CurrentValuePlot.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = DateTimeAxis.ToDouble(DateTime.Now),
                Maximum = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(30)),
                IsZoomEnabled = false,
                IsPanEnabled = false
            });
            CurrentValuePlot.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = byte.MinValue,
                Maximum = byte.MaxValue,
                AbsoluteMinimum = byte.MinValue,
                AbsoluteMaximum = byte.MaxValue,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });
            
            ThreadPool.QueueUserWorkItem(wb => GetPinsThreadCallback());
            ThreadPool.QueueUserWorkItem(wb => UpdateUiWithPinValuesThreadCallback());
        }

        private void GetPinsThreadCallback()
        {
            while (true)
            {
                if (DrumManager == null || !PlotCurrentPinValues)
                    continue;
                lock (_pinValuesLock)
                    _pinValues.Add(DateTime.Now, DrumManager.GetAllPinValues());
                Thread.Sleep(1);
            }
        }

        private void UpdateUiWithPinValuesThreadCallback()
        {
            while (true)
            {
                Dictionary<DateTime, List<byte>> pinValuesToShow;
                lock(_pinValuesLock)
                {
                    pinValuesToShow = new Dictionary<DateTime, List<byte>>(_pinValues);
                    _pinValues.Clear();
                }

                if (pinValuesToShow.Count > 0)
                {
                    DateTime timeStamp = pinValuesToShow.Last().Key;
                    var pinsSeries = CurrentValuePlot.Series.OfType<DataPointSeries>().Where(x => x.IsVisible && x.Tag is Pins).ToDictionary(x => (Pins)x.Tag, x => x);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentValuePlot.Axes.Where(x => x.Position == AxisPosition.Bottom).OfType<DateTimeAxis>()
                            .ForEach(x =>
                            {
                                x.Minimum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(-15));
                                x.Maximum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(5));
                            });

                        foreach (var value in pinValuesToShow)
                        {
                            for (int i = 0; i < value.Value.Count; i++)
                            {
                                if (pinsSeries.ContainsKey((Pins)i))
                                    pinsSeries[(Pins)i].Points.Add(new DataPoint(DateTimeAxis.ToDouble(value.Key), value.Value[i]));
                            }
                        }

                        CurrentValuePlot.InvalidatePlot(true);
                    });
                }

                Thread.Sleep(200);
            }
        }

        private void OnDrumManagerChanged(DrumManager drumManager)
        {
            PlotCurrentPinValues = false;
            DrumManager?.Dispose();
            DrumManager = drumManager;
            TriggerCollection = new ObservableCollection<MidiTriggerViewModel>();

            (DrumManager?.Triggers ?? Enumerable.Empty<MidiTrigger>()).ForEach(trigger =>
                _eventAggregator.GetEvent<PubSubEvent<MidiTrigger>>().Publish(trigger));

            SelectedTrigger = null;
        }

        private void AddTrigger(MidiTrigger trigger)
        {
            var toRemove = TriggerCollection.Where(x => x.Trigger.PinNumber == trigger.PinNumber).ToList();
            toRemove.ForEach(x => TriggerCollection.Remove(x));

            TriggerCollection.Add(new MidiTriggerViewModel(_eventAggregator, DrumManager, trigger));
            
            var seriesToRemove = CurrentValuePlot.Series.Where(x => x.Tag as Pins? == trigger.PinNumber).ToList();
            seriesToRemove.ForEach(x => CurrentValuePlot.Series.Remove(x));

            var newSeries = new LineSeries
            {
                Title = trigger.PinNumber.ToString(),
                Tag = trigger.PinNumber
            };

            int insertIndex = 0;
            for (int i = CurrentValuePlot.Series.Count - 1; i >= 0; i--)
            {
                Pins currentPin = (Pins) CurrentValuePlot.Series[i].Tag;

                if (currentPin < trigger.PinNumber)
                {
                    insertIndex = i + 1;
                    break;
                }
            }

            CurrentValuePlot.Series.Insert(insertIndex, newSeries);

            //public active / inactive
            _eventAggregator.GetEvent<TriggerActiveChangedEvent>()
                .Publish(new KeyValuePair<Pins, bool>(trigger.PinNumber, trigger.Type != TriggerType.Disabled));
        }
        
        private void OnTriggerActiveChanged(KeyValuePair<Pins, bool> keyValuePair)
        {
            var series = CurrentValuePlot.Series.Single(x => ((Pins) x.Tag) == keyValuePair.Key);
            series.IsVisible = keyValuePair.Value;
            CurrentValuePlot.InvalidatePlot(true);
        }
        
        private void LoadSettingsFromEeprom()
        {
            PlotCurrentPinValues = false;
            DrumManager.LoadSettingsFromEeprom();
            TriggerCollection.ForEach(x => x.RaisePropertiesChanged());
            TriggerCollection.ForEach(trigger =>
                _eventAggregator.GetEvent<TriggerActiveChangedEvent>().Publish(
                    new KeyValuePair<Pins, bool>(trigger.Trigger.PinNumber,
                        trigger.TriggerType != TriggerType.Disabled)));
        }
    }
}
