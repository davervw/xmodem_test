using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using xmodem_test;
using System;

namespace xmodem_unit_tests
{
    [TestClass]
    public class BidiTest
    {
        const int size = 1000;

        [TestMethod]
        public void Bidi_Receive_Binary()
        {
            var sender = new SimpleBidirectionalByteStream();
            var rx = new XmodemReceive(sender.GetOtherEnd());

            // start sender in another thread
            Thread send_thread = new Thread(Thread_Bidi_Send_Binary);
            send_thread.Name = "sx";
            send_thread.Start(sender);

            var result = rx.Receive();
            var received = rx.Received;

            int msdelayToWaitForSender = 1000;
            Thread.Sleep(msdelayToWaitForSender);
            sender.Close();

            Assert.IsTrue(result);
            int wholePacketSize = ((size + 127) / 128) * 128;
            Assert.AreEqual(wholePacketSize, received.Length);
            for (int i = 0; i < size; ++i)
                Assert.AreEqual(received[i], (byte)i);
            for (int i= size+1; i< wholePacketSize; ++i)
                Assert.AreEqual(received[i], 0x1A);

            $"result={result} received={BytesToString(received)}"
                .Log();
        }

        static void Thread_Bidi_Send_Binary(object context)
        {
            var sender = (SimpleBidirectionalByteStream)context;
            var sx = new XmodemSend(sender);
            var bytes = new List<byte>();
            for (int i = 0; i < size; ++i)
                bytes.Add((byte)i);
            var result = sx.Send(bytes.ToArray());
        }

        static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
