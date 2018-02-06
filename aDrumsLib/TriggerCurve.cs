using System;
using System.Collections.Generic;
using System.Linq;

namespace aDrumsLib
{
    public class TriggerCurve
    {
        public CurveType CurveType { get; }
        
        private short _horizontalShift;
        public short HorizontalShift
        {
            get { return _horizontalShift; }
            set
            {
                _horizontalShift = value;
                CurvePropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private short _verticalShift;
        public short VerticalShift
        {
            get { return _verticalShift; }
            set
            {
                _verticalShift = value;
                CurvePropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private ushort _horizontalStretch;
        public ushort HorizontalStretch
        {
            get { return _horizontalStretch; }
            set
            {
                _horizontalStretch = value;
                CurvePropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private ushort _verticalStretch;
        public ushort VerticalStretch
        {
            get { return _verticalStretch; }
            set
            {
                _verticalStretch = value;
                CurvePropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CurvePropertyChanged;

        public const int Size = sizeof(CurveType) + sizeof(short) + sizeof(short) + sizeof(ushort) + sizeof(ushort);

        public IEnumerable<byte> GetBytes()
        {
            yield return (byte) CurveType;
            foreach (var b in BitConverter.GetBytes(HorizontalShift))
                yield return b;
            foreach (var b in BitConverter.GetBytes(VerticalShift))
                yield return b;
            foreach (var b in BitConverter.GetBytes(HorizontalStretch))
                yield return b;
            foreach (var b in BitConverter.GetBytes(VerticalStretch))
                yield return b;
        }

        public byte[] ToByteArray() => GetBytes().ToArray();

        public TriggerCurve(CurveType type)
        {
            CurveType = type;

            ushort stretchMidValue = (ushort) (ushort.MaxValue / 100.0 - 1);
            
            switch (type)
            {
                case CurveType.Linear:
                    HorizontalShift = 0;
                    VerticalShift = 0;
                    HorizontalStretch = stretchMidValue;
                    VerticalStretch = stretchMidValue;
                    break;
                case CurveType.Log:
                    HorizontalShift = 64;
                    VerticalShift = 39;
                    HorizontalStretch = 7746;
                    VerticalStretch = 65435;
                    break;
                case CurveType.Exp:
                    HorizontalShift = 105;
                    VerticalShift = -21;
                    HorizontalStretch = 42215;
                    VerticalStretch = 25894;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public TriggerCurve(byte[] bytes)
        {
            int cur = 0;
            CurveType = (CurveType) bytes[cur++];
            HorizontalShift = BitConverter.ToInt16(bytes, cur);
            cur += sizeof(short);
            VerticalShift = BitConverter.ToInt16(bytes, cur);
            cur += sizeof(short);
            HorizontalStretch = BitConverter.ToUInt16(bytes, cur);
            cur += sizeof(ushort);
            VerticalStretch = BitConverter.ToUInt16(bytes, cur);
            cur += sizeof(ushort);
        }

        public double HorizontalStretchPercentage
        {
            get
            {
                //0  = 0
                //1  = ushort.MaxValue / 10.0
                //10 = ushort.MaxValue
                
                return Math.Round(HorizontalStretch / (ushort.MaxValue / 100.0), 2, MidpointRounding.AwayFromZero);
            }
            set { HorizontalStretch = (ushort) (value * (ushort.MaxValue / 100.0 - 1)); }
        }

        public double VerticalStretchPercentage
        {
            get { return Math.Round(VerticalStretch / (ushort.MaxValue / 100.0), 2, MidpointRounding.AwayFromZero); }
            set { VerticalStretch = (ushort) (value * (ushort.MaxValue / 100.0 - 1)); }
        }
    }
}
