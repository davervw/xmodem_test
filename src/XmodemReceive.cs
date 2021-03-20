// xmodem_test - XmodemReceive.cs
//
//////////////////////////////////////////////////////////////////////////////////
//
// MIT License
//
// xmodem_test - tests for Xmodem protocol
// Copyright(c) 2020 - 2021 by David R. Van Wagner
// davevw.com
// github.com/davervw
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace xmodem_test
{
    public class XmodemReceive : XmodemBase
    {
        public XmodemReceive(SimpleStream stream): base(stream)
        {
        }

        public bool Receive(out byte[] received)
        {
            try
            {
                ReadUntilNoDataAvailableAfterMilliseconds(0);
                blockNum = 1;
                errors = 0;
                var bytes = new List<byte>();
                SendNAK();
                while (true)
                {
                    var packet = ReadPacket();
                    if (packet == null)
                    {
                        received = null;
                        return false;
                    }
                    else if (packet[0] == EOT)
                    {
                        SendACK();
                        received = bytes.ToArray();
                        return true;
                    }
                    if (packet[1] == blockNum)
                    {
                        bytes.AddRange(packet.GetRange(3, 128));
                        SendACK();
                        ++blockNum;
                    }
                    else if (packet[1] == blockNum - 1)
                        SendACK();
                    else
                        SendNAK();
                }
            }
            finally
            {
                $"errors={errors} total_errors={total_errors}"
                    .Log();
            }
        }

        public bool Receive(string filename)
        {
            byte[] bytes;
            bool result = Receive(out bytes);
            if (result)
                File.WriteAllBytes(Path.GetFileName(filename), bytes);
            return result;
        }

        List<byte> ReadPacket()
        {
            var packet = new List<byte>();
            var byte_buffer = new byte[1];
            DateTime timeout = NextTimeout();
            while (true)
            {
                if (stream.DataAvailable())
                {
                    if (stream.Read(byte_buffer, 0, 1) != 1)
                        throw new EndOfStreamException();
                    if (packet.Count == 0 && (byte_buffer[0] == CAN || byte_buffer[0] == EOT))
                    {
                        if (byte_buffer[0] == CAN)
                            "< [CAN]".Log();
                        else if (byte_buffer[0] == EOT)
                            "< [EOT]".Log();
                        else
                        {
                            $"< [?? {byte_buffer[0]:x2}]".Log();
                            ReadUntilNoDataAvailableAfterMilliseconds(500);
                        }
                        packet.Add(byte_buffer[0]);
                        total_errors += errors;
                        errors = 0;
                        return packet;
                    }
                    if (packet.Count > 0 || byte_buffer[0] == SOH)
                    {
                        packet.Add(byte_buffer[0]);
                        if (packet.Count < 132 && stream.DataAvailable())
                        {
                            var more = new byte[132 - packet.Count];
                            int count = stream.Read(more, 0, more.Length);
                            if (count == 0)
                                throw new EndOfStreamException();
                            packet.AddRange(more);
                        }
                        if (packet.Count == 132 && IsValidPacket(packet))
                        {
                            total_errors += errors;
                            errors = 0;
                            $"< [#{blockNum}]: {BytesToString(packet.ToArray())}"
                                .Log();
                            return packet;
                        }
                    }
                    else
                    {
                        $"< [?? {byte_buffer[0]:x2}]"
                            .Log();
                        ReadUntilNoDataAvailableAfterMilliseconds(500);
                        if (++errors >= 10)
                        {
                            $"< [ERRORS] {BytesToString(packet.ToArray())}"
                                .Log();
                            return null;
                        }
                    }
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        "< [TIMEOUT]"
                            .Log();
                        if (++errors >= 10)
                        {
                            $"< [ERRORS] {BytesToString(packet.ToArray())}"
                                .Log();
                            return null;
                        }
                        SendNAK();
                        packet.Clear();
                        timeout = NextTimeout();
                    }
                    Thread.Sleep(20);
                }
            }
        }

        bool IsValidPacket(List<byte> packet)
        {
            if (packet.Count != 132)
                return false;
            if (packet[0] != SOH)
                return false;
            if (packet[1] != (byte)~packet[2])
                return false;

            byte checksum = 0;
            for (int i = 0; i < 128; ++i)
                checksum += packet[3 + i];

            return (packet[131] == checksum);
        }

        void SendACK()
        {
            var buffer = new byte[] { ACK };
            "> [ACK]".Log();
            stream.Write(buffer, 0, buffer.Length);
        }

        void SendNAK()
        {
            ReadUntilNoDataAvailableAfterMilliseconds(500);
            var buffer = new byte[] { NAK };
            "> [NAK]".Log();
            stream.Write(buffer, 0, buffer.Length);
        }

        DateTime NextTimeout() => DateTime.Now.Add(new TimeSpan(0, 0, seconds: 10));
    }
}
