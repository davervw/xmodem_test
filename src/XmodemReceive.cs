// xmodem_test - XmodemReceive.cs
//
//////////////////////////////////////////////////////////////////////////////////
//
// MIT License
//
// xmodem_test - tests for Xmodem protocol
// Copyright (c) 2020 - 2021 by David R. Van Wagner
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

using System.Collections.Generic;
using System.IO;

namespace xmodem_test
{
    public partial class XmodemReceive : XmodemBase
    {
        protected List<byte> payloadReceived;
        protected XmodemPacketReceiver packet;

        public XmodemReceive(SimpleStream stream) : base(stream)
        {
            ErrorHandler = new XmodemReceiveErrorNullHandler();
        }

        public XmodemReceiveErrorHandler ErrorHandler { get; set; }

        public byte[] Received { get => payloadReceived?.ToArray(); }

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

            bool isSuccess = (payloadReceived != null);
            return isSuccess;
        }

        protected void InitializeReceive()
        {
            BlockNum = 1;
            Errors = 0;
            ReceiveDone = false;
            payloadReceived = new List<byte>();
            packet = null;

            bool appearsStarted = Stream.DataAvailable();
            if (!appearsStarted)
                NotifySenderToStartSending();
        }

        protected void NotifySenderToStartSending() => SendNAK();

        protected void HandlePacket()
        {
            if (packet?.Bytes == null)
                HandleProtocolFailed();
            else
            {
                switch (packet.Bytes[0])
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
            if (packet.Bytes[1] == BlockNum)
                HandleGoodBlock();
            else if (packet.Bytes[1] == BlockNum - 1)
                HandleRepeatedBlock();
            else
                HandleUnexpectedBlock();
        }

        protected void HandleProtocolFailed()
        {
            if (!ProtocolFailedEventHandledExternally())
            {
                payloadReceived = null;
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
                payloadReceived.AddRange(packet.Bytes.GetRange(3, 128));
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
            byte ReceivedBlockNum = packet.Bytes[1];
            byte ExpectedBlockNum = (byte)BlockNum;

            $"< [BLK#{ReceivedBlockNum} unexpected.  Should have been {ExpectedBlockNum}]"
                .Log();

            if (!UnexpectedBlockEventHandledExternally())
                SendNAK();
        }

        protected void HandleCancel()
        {
            if (!CancelEventHandledExternally())
            {
                SendACK();
                payloadReceived = null;
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
                File.WriteAllBytes(Path.GetFileName(filename), payloadReceived.ToArray());
            return result;
        }

        void ReadPacket()
        {
            packet = new XmodemPacketReceiver(this);
            packet.Read();
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

        bool EndOfTransmissionEventHandledExternally() => ErrorHandler.EndOfTransmissionEventHandled();
        bool GoodBlockEventHandledExternally() => ErrorHandler.GoodBlockEventHandled();
        bool RepeatedBlockEventHandledExternally() => ErrorHandler.RepeatedBlockEventHandled();
        bool CancelEventHandledExternally() => ErrorHandler.CancelEventHandled();
        bool ProtocolFailedEventHandledExternally() => ErrorHandler.ProtocolFailedEventHandled();
        bool UnexpectedBlockEventHandledExternally() => ErrorHandler.UnexpectedBlockEventHandled();
    }
}
