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
        protected List<byte> received;
        protected List<byte> packet;

        public XmodemReceive(SimpleStream stream) : base(stream)
        {
            ErrorHandler = new XmodemReceiveErrorNullHandler();
        }

        public XmodemReceiveErrorHandler ErrorHandler { get; set; }

        public byte[] Received { get => received?.ToArray(); }

        public byte[] Packet { get => packet?.ToArray(); }

        public bool ReceiveDone { get; set; }

        public bool Receive()
        {
            try
            {
                return DoReceive();
            }
            finally
            {
                $"errors={Errors} total_errors={TotalErrors}"
                    .Log();
            }
        }

        protected bool DoReceive()
        {
            InitializeReceive();

            while (!ReceiveDone)
            {
                ReadPacket();
                HandlePacket();
            }

            bool isSuccess = (received != null);
            return isSuccess;
        }

        protected void InitializeReceive()
        {
            BlockNum = 1;
            Errors = 0;
            ReceiveDone = false;
            received = new List<byte>();

            bool appearsStarted = Stream.DataAvailable();
            if (!appearsStarted)
                NotifySenderToStartSending();
        }

        protected void NotifySenderToStartSending() => SendNAK();

        protected void HandlePacket()
        {
            if (packet == null)
                HandleProtocolFailed();
            else
            {
                switch (packet[0])
                {
                    case EOT:
                        HandleEndOfTransmission();
                        break;
                    case CAN:
                        HandleCancel();
                        break;
                    case SOH:
                        HandleBlock();
                        break;
                    default:
                        HandleUnexpectedState();
                        break;
                }
            }
        }

        protected void HandleBlock()
        {
            if (packet[1] == BlockNum)
                HandleGoodBlock();
            else if (packet[1] == BlockNum - 1)
                HandleRepeatedBlock();
            else
                HandleUnexpectedBlock();
        }

        protected void HandleProtocolFailed()
        {
            if (!ProtocolFailedEventHandledExternally())
            {
                received = null;
                ReceiveDone = true;
            }
        }

        protected void HandleEndOfTransmission()
        {
            if (!EndOfTransmissionEventHandledExternally())
            {
                SendACK();
                ReceiveDone = true;
            }
        }

        protected void HandleGoodBlock()
        {
            if (!GoodBlockEventHandledExternally())
            {
                received.AddRange(packet.GetRange(3, 128));
                SendACK();
                ++BlockNum;
            }
        }

        protected void HandleRepeatedBlock()
        {
            if (!RepeatedBlockEventHandledExternally())
                SendACK();
        }

        protected void HandleUnexpectedBlock()
        {
            byte ReceivedBlockNum = packet[1];
            byte ExpectedBlockNum = (byte)BlockNum;

            $"< [BLK#{ReceivedBlockNum} unexpected.  Should have been {ExpectedBlockNum}]"
                .Log();

            SendNAK();
        }

        protected void HandleCancel()
        {
            if (!CancelEventHandledExternally())
            {
                SendACK();
                received = null;
                ReceiveDone = true;
            }
        }

        protected void HandleUnexpectedState()
        {
            "< [UNEXPECTED state. Cannot continue]"
                .Log();
            SendCAN();
            ReceiveDone = true;
        }

        public bool Receive(string filename)
        {
            bool result = Receive();
            if (result)
                File.WriteAllBytes(Path.GetFileName(filename), received.ToArray());
            return result;
        }

        void ReadPacket()
        {
            packet = new List<byte>();
            var byte_buffer = new byte[1];
            DateTime timeout = NextTimeout();
            while (true)
            {
                if (Stream.DataAvailable())
                {
                    if (Stream.Read(byte_buffer, 0, 1) != 1)
                        throw new EndOfStreamException();

                    if (packet.Count == 0 && (byte_buffer[0] == CAN || byte_buffer[0] == EOT))
                    {
                        if (byte_buffer[0] == CAN)
                            "< [CAN]".Log();
                        else if (byte_buffer[0] == EOT)
                            "< [EOT]".Log();
                        packet.Add(byte_buffer[0]);
                        TotalErrors += Errors;
                        Errors = 0;
                        return;
                    }

                    if (packet.Count > 0 || byte_buffer[0] == SOH)
                    {
                        packet.Add(byte_buffer[0]);

                        if (packet.Count < 132 && Stream.DataAvailable())
                        {
                            var more = new byte[132 - packet.Count];
                            int count = Stream.Read(more, 0, more.Length);
                            if (count == 0)
                                throw new EndOfStreamException();
                            packet.AddRange(more);
                        }

                        if (packet.Count == 132)
                        {
                            if (IsValidPacket(packet))
                            {
                                TotalErrors += Errors;
                                Errors = 0;
                                $"< [#{BlockNum}]: {BytesToString(packet.ToArray())}"
                                    .Log();
                                return;
                            }
                            else
                            {
                                $"< [?? {BytesToString(packet.ToArray())}]"
                                    .Log();
                                if (++Errors >= 10)
                                {
                                    $"< [ERRORS]"
                                        .Log();
                                    packet = null;
                                    return;
                                }
                                if (!InvalidPacketEventHandledExternally())
                                {
                                    ReadUntilNoDataAvailableAfterMilliseconds(500);
                                    SendNAK();
                                }
                            }
                        }
                    }
                    else
                    {
                        $"< [?? {byte_buffer[0]:x2}]"
                            .Log();
                        ReadUntilNoDataAvailableAfterMilliseconds(500);
                        if (++Errors >= 10)
                        {
                            $"< [ERRORS] {BytesToString(packet.ToArray())}"
                                .Log();
                            packet = null;
                            return;
                        }
                    }
                }
                else
                {
                    if (DateTime.Now >= timeout)
                    {
                        "< [TIMEOUT]"
                            .Log();
                        if (++Errors >= 10)
                        {
                            $"< [ERRORS] {BytesToString(packet.ToArray())}"
                                .Log();
                            packet = null;
                            return;
                        }
                        if (!TimeoutEventHandledExternally())
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
            Stream.Write(buffer, 0, buffer.Length);
        }

        void SendNAK()
        {
            ReadUntilNoDataAvailableAfterMilliseconds(500);
            var buffer = new byte[] { NAK };
            "> [NAK]".Log();
            Stream.Write(buffer, 0, buffer.Length);
        }

        void SendCAN()
        {
            var buffer = new byte[] { CAN };
            "> [CAN]".Log();
            Stream.Write(buffer, 0, buffer.Length);
        }

        DateTime NextTimeout() => DateTime.Now.Add(new TimeSpan(0, 0, seconds: 10));

        bool EndOfTransmissionEventHandledExternally() => ErrorHandler.EndOfTransmissionEventHandled();
        bool GoodBlockEventHandledExternally() => ErrorHandler.GoodBlockEventHandled();
        bool RepeatedBlockEventHandledExternally() => ErrorHandler.RepeatedBlockEventHandled();
        bool CancelEventHandledExternally() => ErrorHandler.CancelEventHandled();
        bool InvalidPacketEventHandledExternally() => ErrorHandler.InvalidPacketEventHandled();
        bool TimeoutEventHandledExternally() => ErrorHandler.TimeoutEventHandledHandled();
        bool ProtocolFailedEventHandledExternally() => ErrorHandler.ProtocolFailedEventHandled();
    }
}
