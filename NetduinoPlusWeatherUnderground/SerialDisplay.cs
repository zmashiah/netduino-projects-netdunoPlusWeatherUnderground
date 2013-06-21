using System;
using System.IO.Ports;
using System.Text;

namespace ZakieM.tools.HW.Serial.Display
{
    /// <summary>
    /// This class wraps the SerialPort with protocol used with
    /// 4D systems serial displays to set each communication inside
    /// square brackets
    /// </summary>
    public class SerialDisplay : SerialPort
    {
        public SerialDisplay(string comPort, int baud) : 
                    base(comPort, baud, Parity.None, 8, StopBits.One)
        {}

        public void SerializeString(string s)
        {
            byte[] buff = Encoding.UTF8.GetBytes(s + "\r\n");

            this.Write(buff, 0, buff.Length);
        }
    }
}
