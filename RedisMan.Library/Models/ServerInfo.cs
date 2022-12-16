using RedisMan.Library.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RedisMan.Library.Models;

public class KeySpaceDB
{
    public string DBName { get; set; } = "";
    public int Keys { get; set; }
    public int Expires { get; set; }
    public int AvgTtl { get; set; }
}

public class ServerInfo
{
    public List<KeySpaceDB> KeySpace { get; set; } = new List<KeySpaceDB>();
    public bool IsAvaible { get; set; } = false;
    //Redis Information
    [ServerInfoName("redis_version")]
    public int RedisVersion { get; set; }
    [ServerInfoName("redis_mode")]
    public string RedisMode { get; set; } = "";
    [ServerInfoName("process_id")]
    public int ProcessID { get; set; }
    [ServerInfoName("tcp_port")]
    public int TcpPort { get; set; }

    //Operating System
    [ServerInfoName("os")]
    public string Os { get; set; } = "";
    [ServerInfoName("arch_bits")]
    public int ArchBits { get; set; }


    //Connection Details
    [ServerInfoName("connected_clients")]
    public int ConnectedClients { get; set; }

    //Memory Details
    [ServerInfoName("used_memory")]
    public int UsedMemory { get; set; }
    [ServerInfoName("used_memory_human")]
    public string UsedMemoryHuman { get; set; } = "";

    [ServerInfoName("total_system_memory")]
    public int TotalSystemMemory { get; set; }
    [ServerInfoName("total_system_memory_human")]
    public string TotalSystemMemoryHuman { get; set; } = "";
}

[AttributeUsage(AttributeTargets.Property)]
internal class ServerInfoNameAttribute : Attribute
{
    public string Name { get; set; }

    public ServerInfoNameAttribute(string name)
    {
        Name = name;
    }
}