using System.Collections.Generic;
using System.Linq;

namespace aDrumsLib
{
    public sealed class Factory
    {
        public static ISerialPort GetSerialPort(string portName)
        {
            switch (portName)
            {
                case null:
                    return null;
                case SimulatedSerialPort.SimulatedSerialPortName:
                    return new SimulatedSerialPort();
                default:
                    var serialPort = new winSerialPort();
                    serialPort.PortName = portName;
                    return serialPort;
            }
        }

        public static string[] GetPortNames() => GetPortNamesEnumerable().ToArray();

        private static IEnumerable<string> GetPortNamesEnumerable()
        {
            yield return SimulatedSerialPort.SimulatedSerialPortName;
            foreach (var winSerialPortName in System.IO.Ports.SerialPort.GetPortNames())
                yield return winSerialPortName;
        }
    }
}
