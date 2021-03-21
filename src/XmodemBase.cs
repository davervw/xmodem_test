using System;
using System.Collections.Generic;
using System.Threading;

namespace xmodem_test
{
    public abstract class XmodemBase
    {
        public const byte SOH = 0x01; // ^A
        public const byte EOT = 0x04; // ^D
        public const byte ACK = 0x06; // ^F
        public const byte NAK = 0x15; // ^U
        public const byte CAN = 0x18; // ^X
        public const byte SUB = 0x1A; // ^Z

        public SimpleStream Stream { get; protected set; }
        public byte BlockNum { get; protected set; }
        public int Errors { get; protected set; }
        public int TotalErrors { get; protected set; }

        protected XmodemBase(SimpleStream stream)
        {
            Stream = stream;
            BlockNum = 1;
            Errors = 0;
            TotalErrors = 0;
        }

        protected void ReadUntilNoDataAvailableAfterMilliseconds(int milliseconds_minimum)
        {
            Thread.Sleep(milliseconds_minimum);

            if (Stream.DataAvailable())
            {
                var ignored_bytes = new List<byte>();

                byte[] buffer = new byte[256];
                while (Stream.DataAvailable())
                {
                    int read_len = Stream.Read(buffer, 0, buffer.Length);
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
