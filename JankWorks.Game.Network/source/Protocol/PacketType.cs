﻿
namespace JankWorks.Game.Network.Protocol
{
    enum PacketType : byte
    {
        Refused = 0,

        Ping,

        Pong,

        Connect,

        Disconnect,

        Challenge,

        Authenticate,

        Authorised,

        Denied,        

        RconRequest,

        RconResponse,

        RconAuthenticate,

        RconAuthorised,

        RconDenied,

        Sync,

        KeepAlive,

        ClientState,

        HostState,

        Metrics,

        Message,

        Data
    }
}