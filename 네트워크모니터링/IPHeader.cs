using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace 네트워크모니터링
{
    public enum Protocol
    {
        ICMP = 1, IGMP = 2, TCP = 6, UDP = 17, OSPF = 89
    }
    public class IPHeader
    {
        public byte Version { get; private set; } //버젼
        public byte HeaderLen { get; private set; } //IP헤더길이
        public byte Service { get; private set; } //초기에는 TOS(Type Of Service)라고 불렀다. IEFT에서는 우선순위와 서비스 유형을 사용하는 것으로 수정하였다. 현재는 Differentiated Services(차별화 서비스) 영역으로 부르기도 한다.
        public ushort TotalLength { get; private set; } //전체 길이를 바이트 단위로 나타낸다. 최소20, 최대 65535
        public ushort Identification { get; private set; } //IP패킷을 구분하기 위해 부여하는 번호로 단편화 패킷을 조립할 때 이용

        public bool DontFlag { get; private set; }
        public bool MoreFlag { get; private set; }
        public ushort FragOffset { get; private set; }
        //Flags : 
        //처음 비트는 사용하지 않음, 두번째 비트는 DF, 세번째 비트는 MF, DF가 1이면 단편화 할 수 없음, 0이면 단편화 가능
        //만약 DF가 1인 패킷이 MTU(Maximum Transmit Unit)보다 크면 이를 폐기
        //ICMP 오류 메시지(Type 3, Code 4)를 발신지 호스트에 보낸다.
        //ICMP 오류 메시지(Type 3, Code 4)를 받으면 패킷을 단편화하여 보낸다.
        //DF가 0, MF가 1이면 마지막 단편이 아님
        //MF가 0이면 단편화 패킷이 아니거나 마지막 단편임
        //참고로 하나의 패킷을 단편화하면 이들의 Identification는 같다.
        //Fragmentation Offset : 
        //단편화 패킷이 원래 패킷의 어느 위치의 단편인지를 나타낸다.
        //단위는 8바이트, 예를들어 4000크기의 패킷을 1000씩 네개로 나누어 보내면 
        //단편화 offset이 0, 125, 250 , 375인 단편을 만들어 보낸다
        //단편 패킷이 도착하면 목적지 호스트는 타이머를 가동한다.
        //타이머가 만료하였는데 도착하지 않으며 목적지 호스트는 ICMP오류 (type11, code1)을 보낸다.

        public byte TimeToLive { get; private set; } //경유할 수 있는 최대 라우터 수 ,라우터를 거칠 때마다 1씩 감소한다.
        public byte Protocol { get; private set; } //
        public short Checksum { get; private set; } //패킷이 유효함을 계산하기 위한 필드, 헤더 부분만 계산한다, checksum은 음수 가능
        public IPAddress Source { get; private set; } //발신지 주소
        public IPAddress Destination { get; private set; } //목적지 주소
        public string ProtocolString
        {
            get
            {
                switch (Protocol)
                {
                    case 1: return "ICMP";
                    case 2: return "IGMP";
                    case 6: return "TCP";
                    case 17: return "UDP";
                    case 89: return "OSPF";
                }
                return "Not supported";
            }
        }

        public byte[] Option { get; private set; }
        public byte[] Data { get; private set; }

        public IPHeader(byte version, byte hlen, byte service, ushort tlen, ushort id,
            bool dflag, bool mflag, ushort offset, byte ttl, byte protocol, short checksum, uint src, uint dst, byte[] option)
        {
            Version = version;
            HeaderLen = hlen;
            Service = service;
            TotalLength = tlen;
            Identification = id;
            DontFlag = dflag;
            MoreFlag = mflag;
            FragOffset = offset;
            TimeToLive = ttl;
            Protocol = protocol;
            Checksum = checksum;
            Source = new IPAddress(src);
            Destination = new IPAddress(dst);
            Option = option;
        }

        public static IPHeader Parse(byte[] buf)
        {
            MemoryStream ms = new MemoryStream(buf);
            BinaryReader br = new BinaryReader(ms);
            byte hlen_version = br.ReadByte();
            byte hlen = (byte)((hlen_version & 0xF) * 4);
            byte version = (byte)(hlen_version >> 4);
            byte service = br.ReadByte();

            ushort tlen = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort id = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            ushort frag = (ushort)IPAddress.NetworkToHostOrder(br.ReadInt16());
            bool dontflag = (frag & 0x4000) == 0x4000;
            bool moreflag = (frag & 0x2000) == 0x2000;
            ushort offset = (ushort)((frag & 0x1fff) * 8);

            byte ttl = br.ReadByte();
            byte protocol = br.ReadByte();

            short checksum = IPAddress.NetworkToHostOrder(br.ReadInt16());

            uint src = br.ReadUInt32();
            uint dst = br.ReadUInt32();

            IPHeader iphdr = new IPHeader(version, hlen, service, tlen, id, dontflag, moreflag, offset, ttl, protocol, checksum, src, dst, null);
            if (hlen > 20)
            {
                byte[] option = new byte[hlen - 20];
                br.Read(option, 0, option.Length);
                iphdr = null;
                iphdr = new IPHeader(version, hlen, service, tlen, id, dontflag, moreflag, offset, ttl, protocol, checksum, src, dst, option);
            }
            byte[] data = new byte[tlen - hlen];
            br.Read(data, 0, data.Length);
            iphdr.Data = data;

            br.Close();
            ms.Close();

            return iphdr;
        }

        public override string ToString()
        {
            //StringBuilder sbuilder = new StringBuilder();
            //sbuilder.AppendLine(string.Format("============================================"));
            //sbuilder.AppendLine(string.Format("=========== Ethernet Infomation ============"));
            //sbuilder.AppendLine(string.Format("============================================"));
            //sbuilder.AppendLine(string.Format("==      Source      ->    Destination     =="));
            //sbuilder.Append(string.Format("=={0,15}   ", Source));
            //sbuilder.AppendLine(string.Format("-> {0,15}    ==", Destination));
            //sbuilder.AppendLine(string.Format("============================================"));
            //sbuilder.AppendLine(string.Format("== version               :             IPv{0}", Version));
            //sbuilder.AppendLine(string.Format("== header length         :          {0}Bytes", HeaderLen));
            //sbuilder.AppendLine(string.Format("== type to service       :                {0}", Service));
            //sbuilder.AppendLine(string.Format("== total length          :            {0,5}", TotalLength));
            //sbuilder.AppendLine(string.Format("== idntification         :            {0,5}", Identification));
            //sbuilder.AppendLine(string.Format("== fragment offset field :                {0:X}", FragOffset));
            //sbuilder.AppendLine(string.Format("== time to live          :              {0}", TimeToLive));
            //sbuilder.AppendLine(string.Format("== protocol              :              {0}", ProtocolString));
            //sbuilder.AppendLine(string.Format("== checksum                          {0,6}", Checksum));
            //sbuilder.AppendLine(string.Format("============================================"));

            //return sbuilder.ToString();
            return string.Format("\tSrc:{0,20}\tDst:{1,20}\t{2,5}\t{3,5}\t0x{4:X}", Source, Destination, ProtocolString ,TotalLength,Checksum);
        }
    }
}
