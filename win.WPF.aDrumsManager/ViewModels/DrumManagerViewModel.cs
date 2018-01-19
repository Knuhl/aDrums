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

        private Timer _timer;
        private static readonly TimeSpan TimerPeriod = TimeSpan.FromMilliseconds(100);

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
                {
                    CurrentValuePlot.Series.OfType<DataPointSeries>().ForEach(x => x.Points.Clear());
                    _timer = new Timer(PinValueTimerCallback, CurrentValuePlot.Series, TimeSpan.Zero, TimerPeriod);
                }
                else
                    _timer?.Dispose();
            }
        }
        
        public PlotModel CurrentValuePlot { get; }
        
        private DelegateCommand _saveToEepromCommand;
        public DelegateCommand SaveToEepromCommand => _saveToEepromCommand ?? (_saveToEepromCommand =
                                                          new DelegateCommand(() => DrumManager.WriteSettingsToEEPROM(),
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

            TriggerCollection.Add(new MidiTriggerViewModel(DrumManager, trigger, _eventAggregator));
            
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
            bool isFirstActiveTrigger = CurrentValuePlot.Series.All(x => !x.IsVisible);
            if (isFirstActiveTrigger)
                _timer?.Change(TimeSpan.Zero, TimerPeriod);

            var series = CurrentValuePlot.Series.Single(x => ((Pins) x.Tag) == keyValuePair.Key);
            series.IsVisible = keyValuePair.Value;
            CurrentValuePlot.InvalidatePlot(true);
        }
        
        private void PinValueTimerCallback(object state)
        {
            DateTime timeStamp = DateTime.Now;
            ElementCollection<Series> series = (ElementCollection<Series>) state;
            
            //DG.PPV.DEM 18.01.2018: all pins at once
            var pinsSeries = series.OfType<DataPointSeries>().Where(x => x.IsVisible)
                .ToDictionary(x => (Pins) x.Tag, x => x);

            if (!pinsSeries.Any()) return;
            ThreadPool.QueueUserWorkItem(wb =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    List<byte> pinValues = DrumManager.GetAllPinValues();

                    CurrentValuePlot.Axes.Where(x => x.Position == AxisPosition.Bottom).OfType<DateTimeAxis>()
                        .ForEach(x =>
                        {
                            x.Minimum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(-15));
                            x.Maximum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(5));
                        });

                    for (int i = 0; i < pinValues.Count; i++)
                    {
                        if (pinsSeries.ContainsKey((Pins) i))
                            pinsSeries[(Pins) i].Points
                                .Add(new DataPoint(DateTimeAxis.ToDouble(timeStamp), pinValues[i]));
                    }
                    
                    CurrentValuePlot.InvalidatePlot(true);
                });
            });
            
            //DG.PPV.DEM 18.01.2018: each pin by itself

            //var pins = series.OfType<DataPointSeries>().Where(x => x.IsVisible && x.Tag is Pins).ToList();
            //if (!pins.Any()) return;
            //ThreadPool.QueueUserWorkItem(wb =>
            //{
            //    var values = pins.ToDictionary(x => x, x => DrumManager.GetPinValue((Pins) x.Tag));
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        CurrentValuePlot.Axes.Where(x => x.Position == AxisPosition.Bottom).OfType<DateTimeAxis>()
            //            .ForEach(x =>
            //            {
            //                x.Minimum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(-15));
            //                x.Maximum = DateTimeAxis.ToDouble(timeStamp.AddSeconds(5));
            //            });
            //        values.ForEach(x => x.Key.Points.Add(new DataPoint(DateTimeAxis.ToDouble(timeStamp), x.Value)));
            //        CurrentValuePlot.InvalidatePlot(true);
            //    });
            //});
        }
        
        private void LoadSettingsFromEeprom()
        {
            PlotCurrentPinValues = false;
            DrumManager.LoadSettingsFromEEPROM();
            TriggerCollection.ForEach(x => x.RaisePropertiesChanged());
            TriggerCollection.ForEach(trigger =>
                _eventAggregator.GetEvent<TriggerActiveChangedEvent>().Publish(
                    new KeyValuePair<Pins, bool>(trigger.Trigger.PinNumber,
                        trigger.TriggerType != TriggerType.Disabled)));
        }
    }
}
