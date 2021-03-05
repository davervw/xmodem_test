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
using System.Net;

namespace xmodem_test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IPAddress ip = null;
            ushort port;
            int bps;
            bool isReceive = args.Length > 0 && args[0].ToLower() == "rx";
            bool isSend = args.Length > 0 && args[0].ToLower() == "sx";
            bool isNet = args.Length > 1 && IPAddress.TryParse(args[1], out ip);
            bool isSerial = args.Length > 1 && args[1].ToLower().StartsWith("com");
            string Filename = (args.Length > 3) ? args[3] : null;
            bool isConnect = args.Length > 4 && args[4].ToLower() == "connect";
            bool isListen = args.Length > 4 && args[4].ToLower() == "listen";

            if (args.Length == 5
                && (isSend || isReceive)
                && isNet
                && ushort.TryParse(args[2], out port)
                && (isConnect || isListen))
            {
                SimpleNetworkStream stream;
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                if (isConnect)
                    sock.Connect(ip, port);
                else
                {
                    Socket listener = sock;
                    IPEndPoint endpoint = new IPEndPoint(ip, port);
                    listener.Bind(endpoint);
                    listener.Listen();
                    Console.WriteLine($"Listening at {endpoint}");
                    sock = listener.Accept();
                    listener.Close();
                }
                Console.WriteLine($"Connected");
                stream = new SimpleNetworkStream(sock);
                if (isSend)
                {
                    var sender = new XmodemSend(stream);
                    var isSuccess = sender.Send(Filename);
                    stream.Close();
                }
                else if (isReceive)
                {
                    var receiver = new XmodemReceive(stream);
                    var isSuccess = receiver.Receive(Filename);
                    stream.Close();
                }
            }
            else if (args.Length == 4
                && (isSend || isReceive)
                && isSerial
                && int.TryParse(args[2], out bps)
                && !File.Exists(Filename))
            {
                SerialPort serial = new SerialPort(args[1], bps, Parity.None, 8, StopBits.One);
                serial.Open();
                var stream = new SimpleSerialStream(serial);
                if (isSend)
                {
                    var sender = new XmodemSend(stream);
                    var isSuccess = sender.Send(Filename);
                    stream.Close();
                }
                else if (isReceive)
                {
                    var receiver = new XmodemReceive(stream);
                    var isSuccess = receiver.Receive(Filename);
                    stream.Close();
                }
            }
            else
                Console.Error.WriteLine("Usage Syntax Error.");
        }
    }
}
