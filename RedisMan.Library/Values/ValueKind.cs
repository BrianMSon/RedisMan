using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisMan.Library.Values;


public enum ValueType
{
    None,
    String,
    Integer,
    BulkString,
    Array,
    Null,
    Error
}
