// xmodem_test - Xmodem.cs
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
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace xmodem_test
{
    class Xmodem
    {
        const byte SOH = 0x01; // ^A
        const byte EOT = 0x04; // ^D
        const byte ACK = 0x06; // ^F
        const byte NAK = 0x15; // ^U
        const byte CAN = 0x18; // ^X
        const byte SUB = 0x1A; // ^Z

        SimpleStream stream;
        int offset = 0;
        byte blockNum = 1;
        byte[] bytes;
        int errors = 0;
        int total_errors = 0;

        public Xmodem(SimpleStream stream)
        {
            this.stream = stream;
        }

        public bool Send(string filename)
        {
            return Send(File.ReadAllBytes(filename));
        }

        public bool Send(byte[] bytes)
        {
            this.bytes = bytes;
            if (!WaitForNAK())
                return false;
            while (offset < bytes.Length)
            {
                SendBlock();
                if (!HandleSendResponse())
                    return false;
            }
            return true;
        }

        public bool Receive(out byte[] received)
        {
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
                if (packet[1] == blockNum)
                {
                    bytes.AddRange(packet.GetRange(3, 128));
                    SendACK();
                    ++blockNum;
                }
                else if (packet[1] == blockNum - 1)
                    SendACK();
                else if (packet[1] == EOT)
                {
                    SendACK();
                    received = bytes.ToArray();
                    return true;
                }
                else
                    SendNAK();
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
                            Debug.WriteLine("< [CAN]");
                        else if (byte_buffer[0] == EOT)
                            Debug.WriteLine("< [EOT]");
                        else
                            Debug.WriteLine($"< [?? {byte_buffer[0]:x2}]");
                        for (int i = 0; i < 132; ++i)
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
                            return packet;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"< [?? {byte_buffer[0]:x2}]");
                        if (++errors >= 10)
                            return null;
                    }
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        Debug.WriteLine("< [TIMEOUT]");
                        if (++errors >= 10)
                            return null;
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

        void SendEOT()
        {
            var buffer = new byte[] { EOT };
            Debug.WriteLine("> [EOT]");
            stream.Write(buffer, 0, buffer.Length);
        }

        void SendACK()
        {
            var buffer = new byte[] { ACK };
            Debug.WriteLine("> [ACK]");
            stream.Write(buffer, 0, buffer.Length);
        }

        void SendNAK()
        {
            var buffer = new byte[] { NAK };
            Debug.WriteLine("> [NAK]");
            stream.Write(buffer, 0, buffer.Length);
        }

        DateTime NextTimeout() => DateTime.Now.Add(new TimeSpan(0, 0, seconds: 10));

        bool WaitForNAK()
        {
            var buffer = new byte[1];
            DateTime timeout = NextTimeout();
            while (true)
            {
                if (stream.DataAvailable())
                {
                    if (stream.Read(buffer, 0, 1) != 1)
                        throw new EndOfStreamException();

                    if (buffer[0] == NAK)
                    {
                        Debug.WriteLine("< [NAK] OK");
                        total_errors += errors;
                        errors = 0;
                        return true;
                    }
                    else
                        Debug.WriteLine($"< [?? {buffer[0]:X2}]");
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        Debug.WriteLine("< [TIMEOUT]");
                        timeout = NextTimeout();
                    }
                    Thread.Sleep(20); // be nice to CPU
                }
            }
        }

        bool HandleSendResponse()
        {
            bool isLastBlock = (offset >= bytes.Length);
            bool sentEOT = false;
            var buffer = new byte[1];
            DateTime timeout = NextTimeout();
            while (true)
            {
                if (stream.DataAvailable())
                {
                    if (stream.Read(buffer, 0, 1) != 1)
                        throw new EndOfStreamException();

                    if (buffer[0] == ACK)
                    {
                        Debug.WriteLine("< [ACK]");
                        total_errors += errors;
                        errors = 0;
                        if (isLastBlock && !sentEOT)
                        {
                            SendEOT(); // don't leave until acknowledged
                            sentEOT = true;
                        }
                        else
                            return true;
                    }
                    else if (buffer[0] == NAK)
                    {
                        sentEOT = false;
                        Debug.WriteLine("< [NAK]");
                        if (++errors >= 10)
                            return false;
                        if (!ResendBlock())
                            return false;
                    }
                    else if (buffer[0] == CAN)
                    {
                        Debug.WriteLine("< [CAN]");
                        return false;
                    }
                    else
                    {
                        Debug.WriteLine($"< [?? {buffer[0]:X2}]");
                    }
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        Debug.WriteLine("< [TIMEOUT]");
                        timeout = NextTimeout();
                    }
                    Thread.Sleep(20); // be nice to CPU
                }
            }
        }

        bool SendBlock()
        {
            var packet = new List<byte>();
            packet.Add(SOH);
            packet.Add(blockNum);
            packet.Add((byte)~blockNum);
            int size = 128;
            byte checksum = 0;
            for (int i = 0; i < size; ++i)
            {
                byte value = (offset + i < bytes.Length) ? bytes[offset + i] : SUB;
                packet.Add(value);
                checksum += value;
            }
            packet.Add(checksum);
            var packet_bytes = packet.ToArray();

            Debug.WriteLine($"> [#{blockNum}]: {BytesToString(packet_bytes)}");
            stream.Write(packet_bytes, 0, packet_bytes.Length);

            offset += size;
            if (offset > bytes.Length)
                offset = bytes.Length;
            ++blockNum;
            return true;
        }

        bool ResendBlock()
        {
            --blockNum;
            if (offset > 0)
            {
                if (offset == bytes.Length)
                    offset = offset - (offset & 0x7F);
                else
                    offset -= 128;
            }
            else
                blockNum = 1;

            return SendBlock();
        }

        static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
