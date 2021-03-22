using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmodem_test
{
    public class XmodemReceiveErrorNullHandler : XmodemReceiveErrorHandler
    {
        public bool EndOfTransmissionEventHandled() => false;
        public bool GoodBlockEventHandled() => false;
        public bool RepeatedBlockEventHandled() => false;
        public bool UnexpectedBlockEventHandled() => false;
        public bool CancelEventHandled() => false;
        public bool InvalidPacketEventHandled() => false;
        public bool TimeoutEventHandledHandled() => false;
        public bool ProtocolFailedEventHandled() => false;
    }
}
