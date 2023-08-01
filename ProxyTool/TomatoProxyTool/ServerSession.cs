using SagaLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace TomatoProxyTool
{
    public class ServerSession : Client
    {
        public enum SESSION_STATE
        {
            CONNECTED, DISCONNECTED
        }

        public SESSION_STATE state;
        public ProxyClient Client;
        private Socket sock;
        private string host;
        private int port;
        private bool mapserver = false;

        private Dictionary<ushort, Packet> commandTable;

        public ServerSession(string host, int port, ProxyClient client, bool mapserver)
        {
            this.commandTable = new Dictionary<ushort, Packet>();
            this.commandTable.Add(0xFFFF, new Packets.Server.SendUniversal());
            this.Client = client;

            Socket newSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.sock = newSock;
            this.host = host;
            this.mapserver = mapserver;
            this.port = port;
            this.Connect();
        }

        public void Connect()
        {
            bool Connected = false;
            int times = 5;
            do
            {
                if (times < 0)
                {
                    MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += "\r\nCannot connect to the server, please check the configuration!"; }));
                    return;
                }
                try
                {
                    sock.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(host), port));
                    Connected = true;
                }
                catch (Exception ex)
                {
                    MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += ex.ToString(); }));
                    MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += "\r\nFailed... Trying again in 5sec"; }));
                    System.Threading.Thread.Sleep(5000);
                    Connected = false;
                }
                times--;
            }
            while (!Connected);

            MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += "\r\nSuccessfully connected to server"; }));
            this.state = SESSION_STATE.CONNECTED;
            try
            {
                this.netIO = new NetIO(sock, this.commandTable, this);
                this.netIO.SetMode(NetIO.Mode.Client);
                this.netIO.FirstLevelLength = this.Client.firstlevel;
                Packet p = new Packet(8);
                p.data[7] = 0x10;
                this.netIO.SendPacket(p, true, true);
            }
            catch (Exception ex)
            {
                MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += ex.ToString(); }));
            }
        }

        public void OnSendUniversal(Packets.Server.SendUniversal p)
        {
            OnSendUniversal(p, false);
        }

        public void OnSendUniversal(Packets.Server.SendUniversal p, bool s)
        {
            try
            {
                /*ClientManager.LeaveCriticalArea();
                while (!Client.netIO.Crypt.IsReady)
                {
                    System.Threading.Thread.Sleep(100);
                }
                ClientManager.EnterCriticalArea();*/
                if (!PacketContainer.Instance.aesKey.Contains(Client.netIO.Crypt.AESKey))
                    PacketContainer.Instance.aesKey.Add(Client.netIO.Crypt.AESKey);

                Packets.Server.SendUniversal p1 = new TomatoProxyTool.Packets.Server.SendUniversal();
                p1.data = new byte[p.data.Length];
                p.data.CopyTo(p1.data, 0);

                this.Client.packetContainerServer.Add(p1);

                byte ms = 0;
                if (mapserver) ms = 1; else ms = 0;

                MainUI.Instance.Invoke(new Action(() =>
                {
                    MainUI.Instance.PacketsList.Items.Add(string.Format("0x{0:X4},{1},Server,{2},{3}", p.ID, p.data.Length, this.Client.packetContainerServer.IndexOf(p1), ms));
                    if (!MainUI.Instance.ProxyIDList.Keys.Contains(p.ID))
                    {
                        MainUI.Instance.PacketsList.Items[MainUI.Instance.PacketsList.Items.Count - 1] = "*" + MainUI.Instance.PacketsList.Items[MainUI.Instance.PacketsList.Items.Count - 1];
                        MainUI.Instance.listBox1.Items.Add(string.Format("0x{0:X4},{1},Server,{2},{3}", p.ID, p.data.Length, this.Client.packetContainerServer.IndexOf(p1), ms));
                    }
                    if (p.ID == 0x020D)
                    {
                        uint charid = p.GetUInt(6);
                        byte size;
                        byte[] buf;
                        size = p.GetByte(10);
                        buf = p.GetBytes(size, 11);
                        string res = Global.Unicode.GetString(buf);
                        res = res.Replace("\0", "");
                        if (charid < 100000000)
                            MainUI.Instance.listBox2.Items.Add(string.Format("{0},{1},Server,{2},{3}", res, p.data.Length, this.Client.packetContainerServer.IndexOf(p1), ms));
                    }
                }));
                if (MainUI.Instance.autofollow.Checked)
                    MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketsList.TopIndex = MainUI.Instance.PacketsList.Items.Count - 1; }));
                if (MainUI.Instance.checkBox3.Checked)
                    System.Threading.Thread.Sleep(300);
                if (s != true || MainUI.Instance.nextproxy != true)
                {
                    while (MainUI.Instance.checkBox4.Checked)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                if (MainUI.Instance.nextproxy == true) MainUI.Instance.nextproxy = false;
                if (p.ID == 0x33 && !mapserver)
                    Handle0x33(p);
                if (p.ID == 0x34)
                    MainUI.Instance.login = true;
                Client.netIO.SendPacket(p);
            }
            catch (Exception ex)
            {
                MainUI.Instance.Invoke(new Action(() => { MainUI.Instance.PacketInfoBox.Text += ex.ToString(); }));
            }
        }

        public override void OnConnect()
        {
        }

        public override void OnDisconnect()
        {
            if (this.state != SESSION_STATE.DISCONNECTED)
            {
                this.state = SESSION_STATE.DISCONNECTED;
                //this.Client.netIO.Disconnect();
            }
        }

        private void Handle0x33(Packet p)
        {
            if (!MainUI.Instance.login)
            {
                string sss = System.Text.Encoding.ASCII.GetString(p.GetBytes(83, 0));
                string name = System.Text.Encoding.ASCII.GetString(p.GetBytes((ushort)(p.GetByte((ushort)(2)) - 1), (ushort)3));
                int ipindex = p.GetByte(2) + 3;
                string ipport = System.Text.Encoding.ASCII.GetString(p.GetBytes(p.GetByte((ushort)(ipindex)), (ushort)ipindex));
                string ip = ipport.Substring(2, ipport.IndexOf(":") - 2);
                int port = int.Parse(ipport.Substring(ipport.IndexOf(":") + 1, ipport.IndexOf(",") - ipport.IndexOf(":") - 1));
                string si = "";
                MainUI.Instance.Invoke(new Action(() => { si = MainUI.Instance.comboBox1.SelectedItem.ToString(); }));
                if (si == "日服")
                {
                    if (name == "Freesia")
                    {
                        ProxyClientManager.Instance.IP = "211.13.229.64";
                        ProxyClientManager.Instance.port = port;
                    }
                }
                else
                {
                    ProxyClientManager.Instance.IP = ip;
                    ProxyClientManager.Instance.port = port;
                }

                byte[] buf = System.Text.Encoding.UTF8.GetBytes("\03GOF\0AT127.0.0.1:12000,127.0.0.1:12000,127.0.0.1:12000,127.0.0.1:12000\0");
                p.data = buf;
            }
            else
            {
                byte serverid = p.GetByte(2);
                byte size = p.GetByte(3);
                size--;
                string ip = System.Text.Encoding.ASCII.GetString(p.GetBytes(size, 4));
                int port = p.GetInt((ushort)(5 + size));
                if (MainUI.pm != null)
                {
                    MainUI.pm.ready = false;
                    MainUI.pm.Stop();
                }
                MainUI.pm = new ProxyClientManager();
                MainUI.pm.firstlvlen = 2;
                MainUI.pm.mapServer = true;
                MainUI.pm.IP = ip;
                MainUI.pm.port = port;
                MainUI.pm.packetContainerServer = PacketContainer.Instance.packetsMap;
                MainUI.pm.packetContainerClient = PacketContainer.Instance.packetsClient;
                MainUI.pm.packets = PacketContainer.Instance.packets2;
                int localport = port;
                while (!MainUI.pm.StartNetwork(localport))
                    localport++;
                MainUI.pm.ready = true;
                byte[] buf = System.Text.Encoding.UTF8.GetBytes("127.0.0.001");
                p.data = new byte[buf.Length + 9];
                p.ID = 0x33;
                p.PutByte(serverid, 2);
                p.PutByte((byte)(buf.Length + 1), 3);
                p.PutBytes(buf, 4);
                p.PutInt(localport, (ushort)(5 + buf.Length));
                //Client.netIO.SendPacket(p);
            }
        }
    }
}