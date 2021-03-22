using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmodem_test
{
    public interface XmodemReceiveErrorHandler
    {
        public bool EndOfTransmissionEventHandled();
        public bool GoodBlockEventHandled();
        public bool RepeatedBlockEventHandled();
        public bool UnexpectedBlockEventHandled();
        public bool CancelEventHandled();
        public bool InvalidPacketEventHandled();
        public bool TimeoutEventHandledHandled();
        public bool ProtocolFailedEventHandled();
    }
}
