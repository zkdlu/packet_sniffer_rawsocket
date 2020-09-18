using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace 네트워크모니터링
{
    public class UdpHeader
    {
        public ushort SrcPort { get; private set; }
        public ushort DstPort { get; private set; }
        public ushort Length { get; private set; }
        public short Checksum { get; private set; }
        public byte[] Data { get; private set; }

        public UdpHeader(ushort srcport, ushort dstport, ushort length, short checksum,
            byte[] data)
        {
            SrcPort = srcport;
            DstPort = dstport;
            Length = length;
            Checksum = checksum;
            Data = data;
        }

        public static UdpHeader Parse(IPHeader iphdr)
        {
            MemoryStream ms = new MemoryStream(iphdr.Data);
            BinaryReader br = new BinaryReader(ms);

            ushort srcport = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort dstport = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort length = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            short checksum = IPAddress.NetworkToHostOrder(br.ReadInt16());

            byte[] data = new byte[0];
            if (length - 8 > 0)
            {
                data = new byte[length - 8];
                br.Read(data, 0, data.Length);
            }

            br.Close();
            ms.Close();
            return new UdpHeader(srcport, dstport, length, checksum, data);
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t", SrcPort, DstPort);
        }

    }
}
