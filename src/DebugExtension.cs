using System.Diagnostics;

namespace xmodem_test
{
    public static class DebugExtension
    {
        public static void Log(this string msg)
        {
            Debug.WriteLine($"xmodem_test: {msg}");
        }
    }
}
