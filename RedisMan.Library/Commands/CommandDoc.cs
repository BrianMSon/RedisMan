using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedisMan.Library.Commands;



public class CommandDoc
{
    public string Command { get; set; }
    public string Summary { get; set; }
    public string Arguments { get; set; }
    public string Since { get; set; }
    public string Group { get; set; }
}


