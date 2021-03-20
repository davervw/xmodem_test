using System;
using System.Collections.Generic;
using System.Threading;

namespace xmodem_test
{
    public abstract class XmodemBase
    {
        protected const byte SOH = 0x01; // ^A
        protected const byte EOT = 0x04; // ^D
        protected const byte ACK = 0x06; // ^F
        protected const byte NAK = 0x15; // ^U
        protected const byte CAN = 0x18; // ^X
        protected const byte SUB = 0x1A; // ^Z

        protected SimpleStream stream;
        protected byte blockNum = 1;
        protected int errors = 0;
        protected int total_errors = 0;

        protected XmodemBase(SimpleStream stream)
        {
            this.stream = stream;
        }

        protected void ReadUntilNoDataAvailableForMilliseconds(int milliseconds_minimum)
        {
            Thread.Sleep(milliseconds_minimum);

            if (stream.DataAvailable())
            {
                var ignored_bytes = new List<byte>();

                byte[] buffer = new byte[256];
                while (stream.DataAvailable())
                {
                    int read_len = stream.Read(buffer, 0, buffer.Length);
                    if (read_len > 0)
                        ignored_bytes.AddRange(new List<byte>(buffer).GetRange(0, read_len));
                }

                if (ignored_bytes.Count > 0)
                {
                    var ignored_hex = BytesToString(ignored_bytes.ToArray());
                    $"< [IGNORED: {ignored_hex}]"
                        .Log();
                }
            }
        }

        protected static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
