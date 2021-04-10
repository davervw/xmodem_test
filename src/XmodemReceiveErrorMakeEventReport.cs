using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xmodem_test
{
    public class XmodemReceiveMakeEventReport : XmodemReceiveErrorHandler
    {
        SortedDictionary<string, int> Tracking;

        public XmodemReceiveMakeEventReport()
        {
            Tracking = new SortedDictionary<string, int>();
        }

        public void ReportToLog()
        {
            lock(Tracking)
            {
                "EVENT REPORT:".Log();
                foreach (var pair in Tracking)
                    $"EVENT: {pair.Key} COUNT: {pair.Value}".Log();
                "END EVENTS".Log();
            }
        }

        public bool EndOfTransmissionEventHandled() => TrackEventAndReturnFalse("End of Transmission");
        public bool GoodBlockEventHandled() => TrackEventAndReturnFalse("Good Block");
        public bool RepeatedBlockEventHandled() => TrackEventAndReturnFalse("Repeated Block");
        public bool UnexpectedBlockEventHandled() => TrackEventAndReturnFalse("Unexpected Block");
        public bool CancelEventHandled() => TrackEventAndReturnFalse("Cancel Event");
        public bool InvalidPacketEventHandled() => TrackEventAndReturnFalse("Invalid Packet");
        public bool TimeoutEventHandledHandled() => TrackEventAndReturnFalse("Timeout Event");
        public bool ProtocolFailedEventHandled() => TrackEventAndReturnFalse("Protocol Failed");

        private bool TrackEventAndReturnFalse(string message)
        {
            lock(Tracking)
            {
                if (!Tracking.ContainsKey(message))
                    Tracking.Add(message, 1);    
                else
                    ++Tracking[message];
            }
            return false;
        }
    }
}
