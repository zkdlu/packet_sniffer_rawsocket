using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 네트워크모니터링
{
    public class WrapPacket
    {
        private int count;
        public byte[] PacketData
        {
            get;
            private set;
        }

        public int PacketCount
        {
            get;
            private set;
        }

        public IPHeader IPHdr
        {
            get
            {
                return IPHeader.Parse(PacketData);
            }
        }

        public Protocol IPProtocol
        {
            get
            {
                return (Protocol)IPHdr.Protocol;
            }
        }

        public TcpHeader TcpHdr
        {
            get
            {
                if (IPProtocol == Protocol.TCP)
                {
                    return TcpHeader.Parse(IPHdr);
                }
                return null;
            }
        }

        public UdpHeader UdpHdr
        {
            get
            {
                if (IPProtocol == Protocol.UDP)
                {
                    return UdpHeader.Parse(IPHdr);
                }
                return null;
            }
        }

        public WrapPacket(byte[] packet, int plen, int count)
        {
            PacketData = packet;
            PacketCount = plen;
            this.count = count;
        }

        public override string ToString()
        {
            return string.Format("\t{0}\t\t\t\t {1:15}\t\t\t\t {2:15}\t\t\t\t\t {3:5}\t\t\t{4:5}\t\t", count.ToString(), IPHdr.Source.ToString(), IPHdr.Destination.ToString(), IPHdr.ProtocolString, PacketCount);
        }
    }
}
