// xmodem_test - SimpleNetworkStream.cs
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
using System.Net.Sockets;

namespace xmodem_test
{
    public class SimpleNetworkStream : SimpleStream
    {
        Socket sock;
        NetworkStream stream;

        public SimpleNetworkStream(Socket sock)
        {
            this.sock = sock;
            stream = new NetworkStream(sock);
        }

        public override bool DataAvailable()
        {
            return stream.DataAvailable;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            stream.Close();
            //sock.Disconnect(false);
            stream = null;
            sock = null;
        }
    }
}
