using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace aDrumsLib
{
    public class TriggerCurveModification : ObservableCollection<KeyValuePair<byte, byte>>
    {
        public IEnumerable<byte> GetBytes()
        {
            yield return (byte) (Count * 2);
            foreach (var keyValuePair in Items)
            {
                yield return keyValuePair.Key;
                yield return keyValuePair.Value;
            }
        }

        public byte[] ToByteArray() => GetBytes().ToArray();

        public void ClearAndSetFromBytes(byte[] bytes)
        {
            Clear();

            if (bytes.Length <= 0 || bytes.Length % 2 == 0)
                return;
            if (bytes[0] != bytes.Length - 1)
                throw new ArgumentException("Received Number of Modification-Points did not match Parameters");

            for (int i = 2; i < bytes.Length; i += 2)
                Add(new KeyValuePair<byte, byte>(bytes[i - 1], bytes[i]));
        }

        public static TriggerCurveModification FromBytes(byte[] bytes)
        {
            TriggerCurveModification result = new TriggerCurveModification();
            result.ClearAndSetFromBytes(bytes);
            return result;
        }
    }
}
