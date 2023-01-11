 # RedisMan (Redis Manager)
 
Terminal client for Redis with the following features:

- Autocomplete
- Built-in Documentation
- Warning before running dangerous commands
- Universal  command to open keys based on type: `VIEW set-key`.
- Export  key value: `EXPORT HGETALL hash-key`
- Deserializer modifier for commands `HGETALL hash-key #:snappy`
- Pipe commands to shell `LRANGE list-key 0 100 | sort.exe`

![Demo](https://github.com/cosmez/RedisMan/blob/main/.img/demo.gif)

```text
Usage:
  RedisMan.Terminal [options]

Options:
  -h, --host <host>        host/ip address to conect to. [default: 127.0.0.1]
  -p, -u, --port <port>    port to connect to. [default: 6379]
  -c, --command <command>  Command to Execute
  --username <username>    username to authenticate.  
  --password <password>    password to authenticate.  
  --version                Show version information
  -?, -h, --help           Show help and usage information
```