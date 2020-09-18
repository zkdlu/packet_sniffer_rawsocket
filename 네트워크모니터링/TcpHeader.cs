using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace 네트워크모니터링
{
    public class TcpHeader
    {
        public ushort SrcPort { get; private set; }
        public ushort DstPort { get; private set; }
        public uint SeqNum { get; private set; }
        public uint AckNum { get; private set; }

        public ushort HLen_Reserve_Flag { get; private set; }
        public byte HeaderLen { get; private set; }
        public byte Reserve { get; private set; }
        public bool URGFlag { get; private set; }
        public bool ACKFlag { get; private set; }
        public bool PSHFlag { get; private set; }
        public bool RSTFlag { get; private set; }
        public bool SYNFlag { get; private set; }
        public bool FINFlag { get; private set; }
        public string Flags
        {
            get
            {
                StringBuilder sbuilder = new StringBuilder("(");
                if (URGFlag) sbuilder.Append(" URG ");
                if (ACKFlag) sbuilder.Append(" ACK ");
                if (PSHFlag) sbuilder.Append(" PSH ");
                if (RSTFlag) sbuilder.Append(" RST ");
                if (SYNFlag) sbuilder.Append(" SYN ");
                if (FINFlag) sbuilder.Append(" FIN ");
                sbuilder.Append(")");
                return sbuilder.ToString();
            }
        }

        public ushort WindowSize { get; private set; }
        public short CheckSum { get; private set; }
        public ushort UrgentPointer { get; private set; }
        public byte[] Options { get; private set; }
        public byte[] Data { get; private set; }

        public TcpHeader(ushort srcport, ushort dstport, uint seqnum, uint acknum,
            byte hlen, byte reserve, bool urg, bool ack, bool psh, bool rst, bool syn, bool fin, ushort winsize,
            short checksum, ushort urgentpointer, byte[] options)
        {
            SrcPort = srcport;
            DstPort = dstport;
            SeqNum = seqnum;
            AckNum = acknum;
            HeaderLen = hlen;
            Reserve = reserve;
            URGFlag = urg;
            ACKFlag = ack;
            PSHFlag = psh;
            RSTFlag = rst;
            SYNFlag = syn;
            FINFlag = fin;
            WindowSize = winsize;
            CheckSum = checksum;
            UrgentPointer = urgentpointer;
            Options = options;
        }

        public static TcpHeader Parse(IPHeader iphdr)
        {
            MemoryStream ms = new MemoryStream(iphdr.Data);
            BinaryReader br = new BinaryReader(ms);

            ushort srcport = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort dstport = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());

            uint seqnum = (uint)IPAddress.NetworkToHostOrder(br.ReadInt32());
            uint acknum = (uint)IPAddress.NetworkToHostOrder(br.ReadInt32());
            ushort hlen_reserve_flag = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            byte hlen = (byte)((hlen_reserve_flag >> 12) * 4);
            byte reserve = (byte)((hlen_reserve_flag >> 6) & 0x3F);

            bool urg = (hlen_reserve_flag & 0x20) == 0x20;
            bool ack = (hlen_reserve_flag & 0x10) == 0x10;
            bool psh = (hlen_reserve_flag & 0x8) == 0x8;
            bool rst = (hlen_reserve_flag & 0x4) == 0x4;
            bool syn = (hlen_reserve_flag & 0x2) == 0x2;
            bool fin = (hlen_reserve_flag & 0x1) == 0x1;

            ushort winsize = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            short checksum = IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort urgentPointer = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());

            TcpHeader tcphdr = new TcpHeader(srcport, dstport, seqnum, acknum,
                hlen, reserve, urg, ack, psh, rst, syn, fin, winsize, checksum, urgentPointer, null);

            if (hlen > 20)
            {
                byte[] options = new byte[hlen - 20];
                br.Read(options, 0, options.Length);
                tcphdr = null;
                tcphdr = new TcpHeader(srcport, dstport, seqnum, acknum,
                hlen, reserve, urg, ack, psh, rst, syn, fin, winsize, checksum, urgentPointer, options);
            }
            byte[] data = new byte[iphdr.Data.Length - hlen];
            br.Read(data, 0, data.Length);
            tcphdr.Data = data;

            br.Close();
            ms.Close();
            return tcphdr;
        }
    }
}
