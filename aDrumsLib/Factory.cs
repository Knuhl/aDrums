using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aDrumsLib
{
    sealed class Factory
    {
        public static ISerialPort getSerialPort()
        {

            return new SimulatedSerialPort();

            return new winSerialPort();
        }

        public static string[] GetPortNames() => GetPortNamesEnumerable().ToArray();

        private static IEnumerable<string> GetPortNamesEnumerable()
        {
            yield return new SimulatedSerialPort().PortName;
            foreach (var winSerialPortName in System.IO.Ports.SerialPort.GetPortNames())
                yield return winSerialPortName;
        }
    }
}
