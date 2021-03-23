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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace xmodem_test
{
    public partial class XmodemReceive
    {
        public class XmodemPacketReceiver
        {
            public XmodemReceive xmodemReceiver;
            public List<byte> Bytes { get; protected set; }
            public SimpleStream Stream => xmodemReceiver.Stream;
            public int Errors { get => xmodemReceiver.Errors; set => xmodemReceiver.Errors = value; }
            public int TotalErrors { get => xmodemReceiver.TotalErrors; set => xmodemReceiver.TotalErrors = value; }
            public byte BlockNum { get => xmodemReceiver.BlockNum; set => xmodemReceiver.BlockNum = value; }
            public DateTime Timeout { get; set; }
            public bool ReadDone { get; set; }

            public XmodemPacketReceiver(XmodemReceive xmodemReceiver)
            {
                this.xmodemReceiver = xmodemReceiver;
                Bytes = null;
            }

            public void Read()
            {
                ReadInit();

                while (!ReadDone)
                    HandleDataOrTimeout();
            }

            void ReadInit()
            {
                Bytes = new List<byte>();
                Timeout = NextTimeout();
            }

            void HandleDataOrTimeout()
            {
                if (Stream.DataAvailable())
                    HandleData();
                else
                    CheckForTimeoutOrYield();
            }

            void HandleData()
            {
                bool isFirstByte = (Bytes.Count == 0);
                if (isFirstByte)
                    ReadAndHandleFirstByte();
                else
                    ReadAndHandleMoreBytes();
            }

            void ReadAndHandleFirstByte()
            {
                ReadFirstByte();
                HandleFirstByte();
            }

            void ReadAndHandleMoreBytes()
            {
                ReadMoreBytes();
                HandleMoreBytes();
            }

            void HandleMoreBytes()
            {
                bool packetIsComplete = (Bytes.Count == 132);
                if (packetIsComplete)
                    HandleCompletePacket();
            }

            void HandleCompletePacket()
            {
                if (IsValidPacket())
                    HandleValidPacket();
                else
                    HandleInvalidPacket();
            }

            void HandleValidPacket()
            {
                ResetErrors();
                LogValidPacket();
                ReadDone = true;
            }

            void HandleInvalidPacket()
            {
                LogInvalidPacket();
                if (!AnotherErrorTooMany())
                {
                    if (!InvalidPacketEventHandledExternally())
                    {
                        ReadUntilNoDataAvailableAfterMilliseconds(500);
                        SendNAK();
                    }
                }
            }

            bool AnotherErrorTooMany()
            {
                bool isTooMany = (++Errors >= 10);
                if (isTooMany)
                {
                    LogErrorCount();
                    Bytes = null;
                    ReadDone = true;
                }
                return isTooMany;
            }

            void CheckAnotherErrorTooMany()
            {
                bool check = AnotherErrorTooMany();
            }

            void ReadFirstByte()
            {
                var byte_buffer = new byte[1];
                if (Stream.Read(byte_buffer, 0, 1) != 1)
                    throw new EndOfStreamException();
                Bytes.Clear();
                Bytes.Add(byte_buffer[0]);
            }

            void HandleFirstByte()
            {
                switch (Bytes[0])
                {
                    case CAN:
                        HandleCancel();
                        break;
                    case EOT:
                        HandleEndOfTransmission();
                        break;
                    case SOH:
                        HandleBlockStart();
                        break;
                    default:
                        HandleUnexpectedByte();
                        break;
                }
            }

            void HandleCancel()
            {
                LogCancel();
                ResetErrors();
                ReadDone = true;
            }

            void HandleEndOfTransmission()
            {
                LogEndOfTransmission();
                ResetErrors();
                ReadDone = true;
            }

            void HandleBlockStart()
            {
                ResetErrors();
            }

            void CheckForTimeoutOrYield()
            {
                if (DateTime.Now >= Timeout)
                {
                    LogTimeout();
                    if (!AnotherErrorTooMany())
                    {
                        if (!TimeoutEventHandledExternally())
                            SendNAK();
                        Bytes.Clear();
                        Timeout = NextTimeout();
                    }
                }

                if (!ReadDone)
                    YieldProcessor20Milliseconds();
            }

            void YieldProcessor20Milliseconds()
            {
                Thread.Sleep(20);
            }

            bool IsValidPacket()
            {
                bool isSizeCorrect = (Bytes.Count == 132);
                bool hasStartOfHeader = (Bytes[0] == SOH);
                bool hasBlockNumAndInverse = (Bytes[1] == (byte)~Bytes[2]);
                bool checksumCorrect = (Bytes[131] == ComputeChecksum());

                return isSizeCorrect && hasStartOfHeader && hasBlockNumAndInverse && checksumCorrect;
            }

            byte ComputeChecksum()
            {
                byte checksum = 0;
                for (int i = 0; i < 128; ++i)
                    checksum += Bytes[3 + i];
                return checksum;
            }

            void ResetErrors()
            {
                TotalErrors += Errors;
                Errors = 0;
            }

            void ReadMoreBytes()
            {
                var more = new byte[132 - Bytes.Count];
                int count = Stream.Read(more, 0, more.Length);
                if (count == 0)
                    throw new EndOfStreamException();
                Bytes.AddRange(more);
            }

            void HandleUnexpectedByte()
            {
                var readByte = Bytes[Bytes.Count - 1];
                LogUnexpectedByte(readByte);
                ReadUntilNoDataAvailableAfterMilliseconds(500);
                CheckAnotherErrorTooMany();
            }

            void LogValidPacket()
            {
                $"< [#{BlockNum}]: {BytesToString(Bytes.ToArray())}"
                    .Log();
            }

            void LogInvalidPacket()
            {
                $"< [?? {BytesToString(Bytes.ToArray())}]"
                    .Log();
            }

            void LogErrorCount()
            {
                $"< [ERRORS={Errors}]".Log();
            }

            void LogTimeout()
            {
                "< [TIMEOUT]".Log();
            }

            void LogUnexpectedByte(byte value)
            {
                $"< [?? {value:x2}]"
                    .Log();
            }

            void LogCancel()
            {
                "< [CAN]".Log();
            }

            void LogEndOfTransmission()
            {
                "< [EOT]".Log();
            }

            public void SendNAK() => xmodemReceiver.SendNAK();

            public void ReadUntilNoDataAvailableAfterMilliseconds(int milliseconds) => xmodemReceiver.ReadUntilNoDataAvailableAfterMilliseconds(milliseconds);

            public DateTime NextTimeout() => DateTime.Now.Add(new TimeSpan(0, 0, seconds: 10));

            bool InvalidPacketEventHandledExternally() => xmodemReceiver.ErrorHandler.InvalidPacketEventHandled();

            bool TimeoutEventHandledExternally() => xmodemReceiver.ErrorHandler.TimeoutEventHandledHandled();
        }
    }
}
