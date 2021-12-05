using System;
using System.Collections.Generic;

namespace GrpcBrowser.Configuration
{
    public static class ConfiguredGrpcServices
    {
        public static List<Type> ProtoGrpcClients = new List<Type>();
        public static List<Type> CodeFirstGrpcServiceInterfaces = new List<Type>();
    }
}
