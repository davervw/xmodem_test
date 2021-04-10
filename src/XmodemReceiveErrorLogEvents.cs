using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmodem_test
{
    public class XmodemReceiveErrorLogEvents : XmodemReceiveErrorHandler
    {
        public bool EndOfTransmissionEventHandled() => LogEventAndReturnFalse("End of Transmission");
        public bool GoodBlockEventHandled() => LogEventAndReturnFalse("Good Block");
        public bool RepeatedBlockEventHandled() => LogEventAndReturnFalse("Repeated Block");
        public bool UnexpectedBlockEventHandled() => LogEventAndReturnFalse("Unexpected Block");
        public bool CancelEventHandled() => LogEventAndReturnFalse("Cancel Event");
        public bool InvalidPacketEventHandled() => LogEventAndReturnFalse("Invalid Packet");
        public bool TimeoutEventHandledHandled() => LogEventAndReturnFalse("Timeout Event");
        public bool ProtocolFailedEventHandled() => LogEventAndReturnFalse("Protocol Failed");

        private bool LogEventAndReturnFalse(string message)
        {
            $"EVENT: {message}"
                .Log();
            return false;
        }
    }
}
