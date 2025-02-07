﻿// xmodem_test - XmodemSend.cs
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
    public class XmodemSend : XmodemBase
    {
        int offset = 0;
        byte[] bytes;

        public XmodemSend(SimpleStream stream): base(stream)
        {
        }

        public bool Send(string filename)
        {
            return Send(File.ReadAllBytes(filename));
        }

        public bool Send(byte[] bytes)
        {
            try
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
            finally
            {
                Debug.WriteLine($"errors={errors} total_errors={total_errors}");
            }
        }

        void SendEOT()
        {
            var buffer = new byte[] { EOT };
            "> [EOT]".Log();
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
                        "< [NAK] OK".Log();
                        total_errors += errors;
                        errors = 0;
                        return true;
                    }
                    else
                        $"< [?? {buffer[0]:X2}]".Log();
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        "< [TIMEOUT]".Log();
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
                        "< [ACK]".Log();
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
                        "< [NAK]".Log();
                        if (++errors >= 10)
                            return false;
                        if (!ResendBlock())
                            return false;
                    }
                    else if (buffer[0] == CAN)
                    {
                        "< [CAN]".Log();
                        return false;
                    }
                    else
                    {
                        $"< [?? {buffer[0]:X2}]".Log();
                    }
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        "< [TIMEOUT]".Log();
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

            $"> [#{blockNum}]: {BytesToString(packet_bytes)}".Log();
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
    }
}
