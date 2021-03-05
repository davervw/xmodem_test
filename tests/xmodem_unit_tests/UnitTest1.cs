using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using xmodem_test;
using System;

namespace xmodem_unit_tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Bidi_Receive_Binary_1000()
        {
            var sender = new SimpleBidirectionalByteStream();
            var rx = new XmodemReceive(sender.GetOtherEnd());

            // start sender in another thread
            Thread send_thread = new Thread(Thread_Bidi_Send_Binary_1000);
            send_thread.Name = "sx";
            send_thread.Start(sender);

            byte[] received;
            var result = rx.Receive(out received);

            Thread.Sleep(1000);
            sender.Close();

            Assert.IsTrue(result);
            Assert.AreEqual(1024, received.Length);
            for (int i = 0; i < 1000; ++i)
                Assert.AreEqual(received[i], (byte)i);
            for (int i= 1001; i< 1024; ++i)
                Assert.AreEqual(received[i], 0x1A);

            Debug.WriteLine($"result={result} received={BytesToString(received)}");
        }

        static void Thread_Bidi_Send_Binary_1000(object context)
        {
            var sender = (SimpleBidirectionalByteStream)context;
            var sx = new XmodemSend(sender);
            var bytes = new List<byte>();
            for (int i = 0; i < 1000; ++i)
                bytes.Add((byte)i);
            var result = sx.Send(bytes.ToArray());
        }

        static string BytesToString(byte[] bytes) => BitConverter.ToString(bytes).Replace('-', ' ');
    }
}
