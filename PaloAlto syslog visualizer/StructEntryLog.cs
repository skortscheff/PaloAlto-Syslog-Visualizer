using System.Text;

namespace PaloAlto_syslog_visualizer
{
    // One traffic log in the buffer: the message as received, split again only when it is shown,
    // plus the fields the filters search, so a search doesn't parse every log
    public struct StructEntryLog
    {
        public byte[] raw;
        public string strSourceAddress;
        public string strDestinationAddress;
        public string strDestinationPort;
        public string strAction;
        public string strInboundInterface;
        public string strRuleName;

        public StructEntryLog(byte[] raw, string[] fields)
        {
            this.raw = raw;
            strSourceAddress = fields[LogFields.SourceAddress];
            strDestinationAddress = fields[LogFields.DestinationAddress];
            strDestinationPort = fields[LogFields.DestinationPort];
            strAction = fields[LogFields.Action];
            strInboundInterface = fields[LogFields.InboundInterface];
            strRuleName = fields[LogFields.Rule];
        }

        // all the fields of the log, by their index in LogFields
        public string[] getAll
        {
            get { return LogFields.Split(Encoding.UTF8.GetString(raw)); }
        }
    }
}
