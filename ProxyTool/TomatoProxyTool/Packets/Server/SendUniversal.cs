using SagaLib;
using System;

namespace TomatoProxyTool.Packets.Server
{
    [Serializable]
    public class SendUniversal : Packet
    {
        public SendUniversal()
        {
            this.size = 8;
            this.offset = 8;
        }
        public override bool isStaticSize()
        {
            return false;
        }
        public override Packet New()
        {
            return (SagaLib.Packet)new TomatoProxyTool.Packets.Server.SendUniversal();
        }
        public override void Parse(SagaLib.Client client)
        {
            ((ServerSession)(client)).OnSendUniversal(this);
        }
    }
}
