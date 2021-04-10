using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net;
using System.Threading;

namespace xmodem_unit_tests
{
    [TestClass]
    public class MainProgramTest
    {
        static IPAddress ip = IPAddress.Parse("127.0.0.1");
        const ushort port = 10000;
        const string SendFilename = "send.bin";
        const string RecvFilename = "recv.bin";
        volatile static Thread thread_sx = null;
        volatile static Thread thread_rx = null;
        static byte[] TestData;

        static MainProgramTest()
        {
            const int size = 1000;
            TestData = new byte[size];
            for (int i = 0; i < size; ++i)
                TestData[i] = (byte)i;
        }

        [TestMethod]
        public void TestMainNetSendAndRecv1()
        {
            thread_sx = new Thread(ThreadSxListen);
            thread_sx.Name = "sx";
            thread_sx.Start();

            thread_rx = new Thread(ThreadRxConnect);
            thread_rx.Name = "rx";
            thread_rx.Start();

            while (thread_sx != null || thread_rx != null)
                ;

            Assert.IsTrue(File.Exists(RecvFilename));

            byte[] send_bytes = File.ReadAllBytes(SendFilename);
            byte[] recv_bytes = File.ReadAllBytes(RecvFilename);
            int wholePacketSendBytes = ((send_bytes.Length + 127) / 128) * 128;

            Assert.AreEqual(wholePacketSendBytes, recv_bytes.Length);
            for (int i = 0; i < send_bytes.Length; ++i)
                Assert.AreEqual(send_bytes[i], recv_bytes[i]);
            for (int i = send_bytes.Length + 1; i < wholePacketSendBytes; ++i)
                Assert.AreEqual(0x1A, recv_bytes[i]);
        }

        [TestMethod]
        public void TestMainNetSendAndRecv2()
        {
            thread_sx = new Thread(ThreadSxConnect);
            thread_sx.Name = "sx";
            thread_sx.Start();

            thread_rx = new Thread(ThreadRxListen);
            thread_rx.Name = "rx";
            thread_rx.Start();

            while (thread_sx != null || thread_rx != null)
                ;

            Assert.IsTrue(File.Exists(RecvFilename));

            byte[] send_bytes = File.ReadAllBytes(SendFilename);
            byte[] recv_bytes = File.ReadAllBytes(RecvFilename);
            int wholePacketSendBytes = ((send_bytes.Length + 127) / 128) * 128;

            Assert.AreEqual(wholePacketSendBytes, recv_bytes.Length);
            for (int i = 0; i < send_bytes.Length; ++i)
                Assert.AreEqual(send_bytes[i], recv_bytes[i]);
            for (int i = send_bytes.Length + 1; i < wholePacketSendBytes; ++i)
                Assert.AreEqual(0x1A, recv_bytes[i]);
        }

        static void ThreadSxListen(object context)
        {
            File.WriteAllBytes(SendFilename, TestData);
            xmodem_test.Program.Main($"sx {ip} {port} {SendFilename} listen".Split(' '));
            thread_sx = null;
        }

        static void ThreadRxConnect(object context)
        {
            if (File.Exists(RecvFilename))
                File.Delete(RecvFilename);
            xmodem_test.Program.Main($"rx {ip} {port} {RecvFilename} connect".Split(' '));
            thread_rx = null;
        }

        static void ThreadSxConnect(object context)
        {
            File.WriteAllBytes(SendFilename, TestData);
            xmodem_test.Program.Main($"sx {ip} {port} {SendFilename} connect".Split(' '));
            thread_sx = null;
        }

        static void ThreadRxListen(object context)
        {
            if (File.Exists(RecvFilename))
                File.Delete(RecvFilename);
            xmodem_test.Program.Main($"rx {ip} {port} {RecvFilename} listen".Split(' '));
            thread_rx = null;
        }
    }
}
