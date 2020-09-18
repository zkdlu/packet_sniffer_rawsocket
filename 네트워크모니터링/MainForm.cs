using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace 네트워크모니터링
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        Thread thread = null;
        private void MainForm_Load(object sender, EventArgs e)
        {
            splitContainer4.IsSplitterFixed = true;
            ThreadStart ts = new ThreadStart(AsyncSocket);
            thread = new Thread(ts);
        }

        bool flag = false;
        int count = 0;

        private void btn_start_Click(object sender, EventArgs e)
        {
            if (thread.ThreadState != ThreadState.Running)
            {
                flag = true;
                thread.Start();
            }
            else
            {
                MessageBox.Show("이미 스레드가 실행중입니다");
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            flag = false;
        }

        private IPAddress GetLocalIP()
        {
            IPAddress ipaddr = IPAddress.Any;
            foreach (IPAddress addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipaddr = addr;
                }
            }
            return ipaddr;
        }

        private delegate void ItemAddDele(Control con, object msg);
        private void ItemAdd(Control con, object msg)
        {
            if(con.InvokeRequired)
            {
                con.Invoke(new ItemAddDele(ItemAdd), new object[] { con, msg });
            }
            else
            {
                if(con is ListBox)
                {
                    (con as ListBox).Items.Add(msg);
                }
            }
        }

        private delegate void ItemSelectDele(Control con, int count);
        private void ItemSelect(Control con, int count)
        {
            if(con.InvokeRequired)
            {
                con.Invoke(new ItemSelectDele(ItemSelect), new object[] { con, count });
            }
            else
            {
                if(con is ListBox)
                {
                    ListBox lbox = con as ListBox;
                    lbox.SelectedIndex = count;
                }
            }
        }

        private delegate void ItemClearDele(Control con);
        private void ItemClear(Control con)
        {
            if(con.InvokeRequired)
            {
                con.Invoke(new ItemClearDele(ItemClear), con);
            }
            else
            {
                (con as ListBox).Items.Clear();
            }
        }

        private void AsyncSocket()
        {
            Socket lisSock = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            IPAddress ipaddr = GetLocalIP();
            IPEndPoint ipep = new IPEndPoint(ipaddr, 0);
            
            lisSock.Bind(ipep);
            lisSock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
            lisSock.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes(1), null);

            while (flag)
            {
                try
                {
                    byte[] buffer = new byte[lisSock.ReceiveBufferSize];
                    int size = lisSock.Receive(buffer);
                    count++;

                    WrapPacket packet = new WrapPacket(buffer, size, count);
                    ItemAdd(lbox_packet, packet);
                }
                catch
                {
                    ItemAdd(lbox_packet, "Error PacketError PacketError PacketError PacketError PacketError PacketError PacketError PacketError PacketError PacketError PacketError Packet");
                }
            }
            lisSock.Close();
            lisSock = null;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            thread.Abort();
            flag = false;
            Application.Exit();
        }

        private void lbox_packet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbox_packet.SelectedIndex == -1)
            {
                return;
            }
            lv_packet.Items.Clear();
            tv_packet.Nodes.Clear();
            WrapPacket packet = lbox_packet.SelectedItem as WrapPacket;

            ViewTree(packet);
            ViewHex(packet);
            ViewAsci(packet);
        }

        private void ViewAsci(WrapPacket packet)
        {
            int count = packet.PacketCount;
            byte[] buffer = packet.PacketData;
            StringBuilder sbuilder = new StringBuilder();

            ItemClear(lbox_asci);
            for(int i = 0; i < count; i++)
            {
                char c = Convert.ToChar(buffer[i]);
                if(c == 0)
                {
                    sbuilder.Append(".");
                }
                else
                {
                    sbuilder.Append(c);
                }
                if( (i + 1) % 50 == 0)
                {
                    ItemAdd(lbox_asci, sbuilder.ToString());
                    sbuilder.Clear();
                }
            }

        }

        private void ViewTree(WrapPacket packet)
        {
            TreeNode ipnode = MakeIPNode(packet.IPHdr);
            tv_packet.Nodes.Add(ipnode);

            TcpHeader tcphdr = packet.TcpHdr;
            if(tcphdr != null)
            {
                TreeNode tcpnode = MakeTcpNode(tcphdr);
                tv_packet.Nodes.Add(tcpnode);
            }

            UdpHeader udphdr = packet.UdpHdr;
            if(udphdr != null)
            {
                TreeNode udpnode = MakeUdpNode(udphdr);
                tv_packet.Nodes.Add(udpnode);
            }
        }

        private TreeNode MakeUdpNode(UdpHeader udphdr)
        {
            TreeNode udpnode = new TreeNode(string.Format("User Datagram Protocol, Src Port: {0}, Dst Port: {1}", udphdr.SrcPort, udphdr.DstPort));
            udpnode.Nodes.Add(string.Format("Source Port: {0}", udphdr.SrcPort));
            udpnode.Nodes.Add(string.Format("Destination Port: {0}", udphdr.DstPort));
            udpnode.Nodes.Add(string.Format("Length: {0}", udphdr.Length));
            udpnode.Nodes.Add(string.Format("Checksum: 0x{0:X}", udphdr.Checksum));

            return udpnode;
        }

        private TreeNode MakeTcpNode(TcpHeader tcphdr)
        {
            TreeNode tcpnode = new TreeNode(string.Format("Transmissino Control Protocol, Src Port: {0}, Dst Port: {1}, Seq: {2}, Ack: {3}", tcphdr.SrcPort, tcphdr.DstPort, tcphdr.SeqNum, tcphdr.AckNum));
            tcpnode.Nodes.Add(string.Format("Source Port: {0}", tcphdr.SrcPort));
            tcpnode.Nodes.Add(string.Format("Destination Port: {0}", tcphdr.DstPort));
            tcpnode.Nodes.Add(string.Format("Sequence Number: {0}", tcphdr.SeqNum));
            tcpnode.Nodes.Add(string.Format("Acknowledgement number: {0}", tcphdr.AckNum));
            tcpnode.Nodes.Add(string.Format("Header Length: {0} bytes", tcphdr.HeaderLen));
            tcpnode.Nodes.Add(string.Format("Flags: {0}", tcphdr.Flags));
            tcpnode.Nodes.Add(string.Format("Window size value: {0}", tcphdr.WindowSize));
            tcpnode.Nodes.Add(string.Format("Checksum: 0x{0:X}", tcphdr.CheckSum));
            tcpnode.Nodes.Add(string.Format("Urgent Pointer: {0}", tcphdr.UrgentPointer));

            return tcpnode;
        }

        private TreeNode MakeIPNode(IPHeader iphdr)
        {
            TreeNode ipnode = new TreeNode(string.Format("Internet Protocol Version {0}, Src: {1,15}, Dst: {2,15}", iphdr.Version, iphdr.Source, iphdr.Destination));
            ipnode.Nodes.Add(string.Format("Version : {0}", iphdr.Version));
            ipnode.Nodes.Add(string.Format("Header Length : {0} bytes", iphdr.HeaderLen));
            ipnode.Nodes.Add(string.Format("Total Length : {0}", iphdr.TotalLength));
            ipnode.Nodes.Add(string.Format("Identification : 0x{0:X} ({0})", iphdr.Identification));
            ipnode.Nodes.Add(string.Format("Fragment Offset : {0}", iphdr.FragOffset));
            ipnode.Nodes.Add(string.Format("Time To Live : {0}", iphdr.TimeToLive));
            ipnode.Nodes.Add(string.Format("Protocol : {0} ({1})", iphdr.ProtocolString, iphdr.Protocol));
            ipnode.Nodes.Add(string.Format("Header Checksum : 0x{0:X}", iphdr.Checksum));
            ipnode.Nodes.Add(string.Format("Source: {0:15}", iphdr.Source));
            ipnode.Nodes.Add(string.Format("Destination: {0:15}", iphdr.Destination));

            return ipnode;
        }

        private void ViewHex(WrapPacket packet)
        {
            int count = packet.PacketCount;
            byte[] buffer = packet.PacketData;
            StringBuilder sblocal = new StringBuilder();
            int num = 0;

            for (int i = 0; i < count; i++)
            {
                string hex = Convert.ToString(buffer[i], 16);
                hex = hex.ToUpper();
                if (hex.Length == 1)
                {
                    sblocal.Append("0");
                    sblocal.Append(hex);
                }
                else
                {
                    sblocal.Append(hex);
                }
                sblocal.Append(" ");
                if( (i + 1) % 16 == 0)
                {
                    num++;
                    List<string> list = new List<string>();
                    string[] elems = sblocal.ToString().Split(' ');
                    list.Add(num.ToString());
                    list.AddRange(elems);
                    sblocal = sblocal.Remove(0, sblocal.Length);
                    ListViewItem lvi = new ListViewItem(list.ToArray());
                    lv_packet.Items.Add(lvi);
                }
            }
        }

        
    }
}   
