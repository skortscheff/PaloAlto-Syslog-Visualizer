using System.Collections.Generic;
using System.Text;

namespace PaloAlto_syslog_visualizer
{
    // One field of the traffic log: its position in the syslog CSV and its column name
    public sealed class LogField
    {
        public readonly int Index;
        public readonly string Name;
        public readonly bool Default;

        public LogField(int index, string name, bool isDefault = false)
        {
            Index = index;
            Name = name;
            Default = isDefault;
        }
    }

    // Traffic log format of PAN-OS, the same order as the CSV export of the Monitor tab
    public static class LogFields
    {
        // fields used by the filters and to recognize a traffic log
        public const int Type = 3, SourceAddress = 7, DestinationAddress = 8, Rule = 11,
            InboundInterface = 18, DestinationPort = 25, Action = 30;
        // up to Action Source, older PAN-OS versions stop around here
        public const int MinimumFields = 54;

        // index 0 is the syslog header, 38 and 43 are FUTURE_USE
        public static readonly LogField[] All =
        {
            new LogField(1, "Receive Time", true),
            new LogField(2, "Serial #"),
            new LogField(3, "Type"),
            new LogField(4, "Threat/Content Type"),
            new LogField(5, "Config Version"),
            new LogField(6, "Generate Time"),
            new LogField(7, "Source Address", true),
            new LogField(8, "Destination Address", true),
            new LogField(9, "NAT Source IP", true),
            new LogField(10, "NAT Destination IP", true),
            new LogField(11, "Rule", true),
            new LogField(12, "Source User"),
            new LogField(13, "Destination User", true),
            new LogField(14, "Application", true),
            new LogField(15, "Virtual System"),
            new LogField(16, "Source Zone", true),
            new LogField(17, "Destination Zone", true),
            new LogField(18, "Inbound Interface", true),
            new LogField(19, "Outbound Interface", true),
            new LogField(20, "Log Action"),
            new LogField(21, "Time Logged"),
            new LogField(22, "Session ID", true),
            new LogField(23, "Repeat Count"),
            new LogField(24, "Source Port", true),
            new LogField(25, "Destination Port", true),
            new LogField(26, "NAT Source Port", true),
            new LogField(27, "NAT Destination Port", true),
            new LogField(28, "Flags", true),
            new LogField(29, "IP Protocol", true),
            new LogField(30, "Action", true),
            new LogField(31, "Bytes", true),
            new LogField(32, "Bytes Sent", true),
            new LogField(33, "Bytes Received", true),
            new LogField(34, "Packets", true),
            new LogField(35, "Start Time"),
            new LogField(36, "Elapsed Time (sec)", true),
            new LogField(37, "Category", true),
            new LogField(39, "Sequence Number"),
            new LogField(40, "Action Flags"),
            new LogField(41, "Source Country"),
            new LogField(42, "Destination Country"),
            new LogField(44, "Packets Sent", true),
            new LogField(45, "Packets Received", true),
            new LogField(46, "Session End Reason", true),
            new LogField(47, "DG Hierarchy Level 1"),
            new LogField(48, "DG Hierarchy Level 2"),
            new LogField(49, "DG Hierarchy Level 3"),
            new LogField(50, "DG Hierarchy Level 4"),
            new LogField(51, "Virtual System Name"),
            new LogField(52, "Device Name"),
            new LogField(53, "Action Source", true),
            new LogField(54, "Source VM UUID"),
            new LogField(55, "Destination VM UUID"),
            new LogField(56, "Tunnel ID/IMSI"),
            new LogField(57, "Monitor Tag/IMEI"),
            new LogField(58, "Parent Session ID"),
            new LogField(59, "Parent Session Start Time"),
            new LogField(60, "Tunnel"),
            new LogField(61, "SCTP Association ID"),
            new LogField(62, "SCTP Chunks"),
            new LogField(63, "SCTP Chunks Sent"),
            new LogField(64, "SCTP Chunks Received"),
            new LogField(65, "Rule UUID"),
            new LogField(66, "HTTP/2 Connection Session ID"),
            new LogField(67, "App Flap Count"),
            new LogField(68, "Policy ID"),
            new LogField(69, "Link Switches"),
            new LogField(70, "SDWAN Cluster"),
            new LogField(71, "SDWAN Device Type"),
            new LogField(72, "SDWAN Cluster Type"),
            new LogField(73, "SDWAN Site Name"),
            new LogField(74, "Dynamic User Group"),
            new LogField(75, "X-Forwarded-For IP"),
            new LogField(76, "Source Device Category"),
            new LogField(77, "Source Device Profile"),
            new LogField(78, "Source Device Model"),
            new LogField(79, "Source Device Vendor"),
            new LogField(80, "Source Device OS Family"),
            new LogField(81, "Source Device OS Version"),
            new LogField(82, "Source Device Host"),
            new LogField(83, "Source Device MAC"),
            new LogField(84, "Destination Device Category"),
            new LogField(85, "Destination Device Profile"),
            new LogField(86, "Destination Device Model"),
            new LogField(87, "Destination Device Vendor"),
            new LogField(88, "Destination Device OS Family"),
            new LogField(89, "Destination Device OS Version"),
            new LogField(90, "Destination Device Host"),
            new LogField(91, "Destination Device MAC"),
            new LogField(92, "Container ID"),
            new LogField(93, "POD Namespace"),
            new LogField(94, "POD Name"),
            new LogField(95, "Source EDL"),
            new LogField(96, "Destination EDL"),
            new LogField(97, "Host ID"),
            new LogField(98, "Endpoint Serial Number"),
            new LogField(99, "Source Dynamic Address Group"),
            new LogField(100, "Destination Dynamic Address Group"),
            new LogField(101, "Session Owner"),
            new LogField(102, "High Resolution Timestamp"),
            new LogField(103, "NSSAI SST"),
            new LogField(104, "NSSAI SD"),
            new LogField(105, "App Subcategory"),
            new LogField(106, "App Category"),
            new LogField(107, "App Technology"),
            new LogField(108, "App Risk"),
            new LogField(109, "App Characteristic"),
            new LogField(110, "App Container"),
            new LogField(111, "Tunneled App"),
            new LogField(112, "App SaaS"),
            new LogField(113, "App Sanctioned State"),
            new LogField(114, "Offloaded"),
            new LogField(115, "TCP RTT Client to Server"),
            new LogField(116, "TCP RTT Server to Client"),
            new LogField(117, "Out of Order Packets Client to Server"),
            new LogField(118, "Out of Order Packets Server to Client"),
            new LogField(119, "TCP Retransmits Client to Server"),
            new LogField(120, "TCP Retransmits Server to Client"),
            new LogField(121, "TCP Zero Window Client to Server"),
            new LogField(122, "TCP Zero Window Server to Client"),
        };

        // Splits a syslog CSV line. PAN-OS quotes the fields that contain commas,
        // e.g. App Characteristic "has-known-vulnerability,tunnel-other-app"
        public static string[] Split(string line)
        {
            List<string> fields = new List<string>(128);
            StringBuilder field = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c != '"')
                        field.Append(c);
                    else if (i + 1 < line.Length && line[i + 1] == '"')
                        field.Append(line[++i]); // "" is an escaped quote
                    else
                        quoted = false;
                }
                else if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                    field.Append(c);
            }
            // the firewall may end the message with a newline
            fields.Add(field.ToString().TrimEnd('\r', '\n', '\0'));
            return fields.ToArray();
        }
    }
}
