using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using aDrumsLib;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Events;
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

        public CurveType? TriggerCurveType
        {
            get { return TriggerCurve?.CurveType; }
            set
            {
                if (TriggerCurve?.CurveType == value) return;
                TriggerCurve = value == null ? null : new TriggerCurve(value.Value);
                RaisePropertyChanged();
            }
        }

        public TriggerCurve TriggerCurve
        {
            get { return Trigger.Curve; }
            set
            {
                if (Trigger.Curve == value) return;
                if(Trigger.Curve != null)
                    Trigger.Curve.CurvePropertyChanged -= OnCurvePropertyChanged;
                Trigger.Curve = value;
                RaisePropertyChanged();
                BuildPlot();
                if(Trigger.Curve != null)
                    Trigger.Curve.CurvePropertyChanged += OnCurvePropertyChanged;
            }
        }

        public TriggerCurveModification Modifications => Trigger.CurveModification;

        public PlotModel TriggerCurvePlot { get; }

        public MidiTriggerViewModel(IEventAggregator eventAggregator, DrumManager drumManager, MidiTrigger trigger)
        {
            _eventAggregator = eventAggregator;
            _drumManager = drumManager;
            Trigger = trigger;
            
            TriggerCurvePlot = new PlotModel {Title = "Curve"};
            TriggerCurvePlot.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = byte.MinValue,
                Maximum = byte.MaxValue,
                AbsoluteMinimum = byte.MinValue,
                AbsoluteMaximum = byte.MaxValue,
                IsZoomEnabled = true,
                IsPanEnabled = true
            });
            TriggerCurvePlot.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = byte.MinValue,
                Maximum = byte.MaxValue,
                AbsoluteMinimum = byte.MinValue,
                AbsoluteMaximum = byte.MaxValue,
                IsZoomEnabled = true,
                IsPanEnabled = true
            });

            BuildPlot();

            if(Trigger.Curve != null)
                Trigger.Curve.CurvePropertyChanged += OnCurvePropertyChanged;
            if (Modifications != null)
            {
                Modifications.CollectionChanged += OnCurvePropertyChanged;
                //Modifications.Add(new KeyValuePair<byte, byte>(127, 0));
            }
        }
        
        private void OnCurvePropertyChanged(object sender, EventArgs eventArgs) => BuildPlot();

        private void BuildPlot()
        {
            TriggerCurvePlot.Series.Clear();

            if (TriggerCurve == null) return;

            DataPointSeries series;

            switch (TriggerCurveType)
            {
                case CurveType.Linear:
                    series = new FunctionSeries(Linear, byte.MinValue, byte.MaxValue, 1d, "Linear");
                    break;
                case CurveType.Log:
                    series = new FunctionSeries(Log10, byte.MinValue, byte.MaxValue, 1d, "Log");
                    break;
                case CurveType.Exp:
                    series = new FunctionSeries(Exp, byte.MinValue, byte.MaxValue, 1d, "Exp");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            //TODO Modifications
            //foreach (var modification in Modifications)
            //{
            //    var existing = series.Points.Find(x => (byte) x.X == modification.Key);
            //    if (existing.IsDefined())
            //        series.Points.Remove(existing);
            //    series.Points.Add(new DataPoint(modification.Key, modification.Value));
            //}

            TriggerCurvePlot.Series.Add(series);
            
            TriggerCurvePlot.InvalidatePlot(true);
        }

        private double Linear(double x)
            => CoerceValue(x * TriggerCurve.HorizontalStretchPercentage - TriggerCurve.HorizontalShift + TriggerCurve.VerticalShift);
        
        private double Log10(double x)
            => CoerceValue(
                TriggerCurve.VerticalStretchPercentage *
                Math.Log10((x- TriggerCurve.HorizontalShift) / TriggerCurve.HorizontalStretchPercentage) +
                TriggerCurve.VerticalShift);

        private double Exp(double x)
            => CoerceValue(
                TriggerCurve.VerticalStretchPercentage *
                Math.Exp((x- TriggerCurve.HorizontalShift) / TriggerCurve.HorizontalStretchPercentage) +
                TriggerCurve.VerticalShift);

        private static double CoerceValue(double x) => x < 0 ? 0 : x > byte.MaxValue ? byte.MaxValue : x;
    }
}
