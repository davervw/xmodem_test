using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using xmodem_test;
using System;
using System.Net.Sockets;
using System.Net;

namespace xmodem_unit_tests
{
    [TestClass]
    public class NetTest
    {
        static IPAddress ip = IPAddress.Parse("127.0.0.1");
        const ushort port = 28175;
        const int size = 1000;

        [TestMethod]
        public void Net_Receive_Binary()
        {
            var listen_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            listen_sock.Bind(new IPEndPoint(ip, port));
            listen_sock.Listen();

            // start sender in another thread
            Thread send_thread = new Thread(Thread_Net_Send_Binary);
            send_thread.Name = "sx";
            send_thread.Start();

            var sock = listen_sock.Accept();
            listen_sock.Close();
            listen_sock = null;

            var receiver = new SimpleNetworkStream(sock);
            var rx = new XmodemReceive(receiver);

            byte[] received;
            var result = rx.Receive(out received);

            Thread.Sleep(1000);
            receiver.Close();

            Assert.IsTrue(result);
            int wholePacketSize = ((size + 127) / 128) * 128;
            Assert.AreEqual(wholePacketSize, received.Length);
            for (int i = 0; i < size; ++i)
                Assert.AreEqual(received[i], (byte)i);
            for (int i= size+1; i< wholePacketSize; ++i)
                Assert.AreEqual(received[i], 0x1A);

            Debug.WriteLine($"result={result} received={BytesToString(received)}");
        }

        static void Thread_Net_Send_Binary(object context)
        {
            var send_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            send_sock.Connect(new IPEndPoint(ip, port));
            var sender = new SimpleNetworkStream(send_sock);
            var sx = new XmodemSend(sender);
            var bytes = new List<byte>();
            for (int i = 0; i < size; ++i)
                bytes.Add((byte)i);
            var result = sx.Send(bytes.ToArray());
            sender.Close();
        }

        static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
