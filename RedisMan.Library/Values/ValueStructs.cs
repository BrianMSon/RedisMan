using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RedisMan.Library.Values;

public interface RedisValue
{
    public ValueType Type { get; }
    public string? Value { get; set; }
    public static ValueType GetValueType(char firstByte)
    {
        return firstByte switch
        {
            '+' => ValueType.String,
            '-' => ValueType.Error,
            ':' => ValueType.Integer,
            '$' => ValueType.BulkString,
            '*' => ValueType.Array,
            _ => ValueType.None
        };
    }

    public static RedisValue FromByte(char firstByte)
    {
        return firstByte switch
        {
            '+' => new RedisString(),
            '-' => new RedisError(),
            ':' => new RedisInteger(),
            '$' => new RedisBulkString(),
            '*' => new RedisArray(),
            _ => new RedisNull()
        };
    }
}

public class RedisNull : RedisValue
{
    public string? Value {
        get { return "Null"; }
        set { } 
    }
    public ValueType Type { get { return ValueType.Null; }  }
}

public class RedisString : RedisValue
{
    public ValueType Type { get { return ValueType.String; } }
    public string? Value { get; set; } = "";
}


public class RedisBulkString : RedisValue
{
    public ValueType Type { get { return ValueType.BulkString; } }
    public int Length { get; set; }
    public string? Value { get; set; } = "";
}

public class RedisError : RedisValue
{
    public ValueType Type { get { return ValueType.Error; } }
    public string? Value { get; set; } = "";
}

public class RedisInteger : RedisValue
{
    public ValueType Type { get { return ValueType.Integer; } }
    private string _value = "0";
    private int _intValue;
    public string? Value { 
        get { return _value; }
        set { 
            _value = value;  
            if (!int.TryParse(_value, out _intValue))
            {
                _intValue = 0;
            }
        }
    }
    public int IntegerValue { get =>_intValue;  }
}


public class RedisArray : RedisValue
{
    public RedisArray()
    {

    }

    public void Add(RedisValue value)
    {
        Values.Add(value);
    }

    public ValueType Type { get { return ValueType.Array; } }
    public int Length { get; set; }
    public List<RedisValue> Values { get; set; }

    public string Value { 
        get { return $"{Length}"; }  
        set { }  
    }
}





