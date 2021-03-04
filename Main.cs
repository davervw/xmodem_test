// xmodem_test - Main.cs
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
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xmodem_test
{
    class Program
    {
        static void Main(string[] args)
        {
            //SerialPort serial = new SerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            //serial.Open();
            //var stream = new SimpleSerialStream(serial);

            var sender = new BidirectionalByteStream();
            var rx = new Xmodem(sender.GetOtherEnd());

            // start sender in another thread
            Thread send_thread = new Thread(SendThread);
            send_thread.Name = "sx";
            send_thread.Start(sender);

            byte[] received;
            var result = rx.Receive(out received);

            Thread.Sleep(1000);
            sender.Close();
            Console.WriteLine($"result={result} received={BytesToString(received)}");
        }

        static void SendThread(object context)
        {
            var sender = (BidirectionalByteStream)context;
            var sx = new Xmodem(sender);
            var bytes = new List<byte>();
            for (int i = 0; i < 1000; ++i)
                bytes.Add((byte)i);
            var result = sx.Send(bytes.ToArray());
        }

        static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
