// xmodem_test - BidirectionalStream.cs
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

namespace xmodem_test
{
    class BidirectionalByteStream : SimpleStream
    {
        List<List<byte>> read_buffers;
        List<List<byte>> write_buffers;

        public BidirectionalByteStream()
        {
            read_buffers = new List<List<byte>>();
            write_buffers = new List<List<byte>>();
        }

        public BidirectionalByteStream GetOtherEnd()
        {
            var stream = new BidirectionalByteStream();
            stream.read_buffers = write_buffers;
            stream.write_buffers = read_buffers;
            return stream;
        }

        public override bool DataAvailable()
        {
            lock (read_buffers)
                return read_buffers.Count > 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (read_buffers)
            {
                if (read_buffers.Count == 0)
                    return 0;
                else if (count < read_buffers[0].Count)
                {
                    Array.Copy(read_buffers[0].ToArray(), 0, buffer, offset, count);
                    read_buffers[0].RemoveRange(0, count);
                    return count;
                }
                else
                {
                    count = read_buffers[0].Count;
                    Array.Copy(read_buffers[0].ToArray(), 0, buffer, offset, count);
                    read_buffers.RemoveAt(0);
                    return count;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock(write_buffers)
            {
                var bytes = new List<byte>();
                for (int i = 0; i < count; ++i)
                    bytes.Add(buffer[offset + i]);
                write_buffers.Add(bytes);
            }
        }

        public override void Close()
        {
            read_buffers = null;
            write_buffers = null;
        }
    }
}
