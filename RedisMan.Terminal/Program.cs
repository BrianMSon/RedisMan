using Mono.Options;
using Mono.Terminal;
using RedisMan.Library.Protocol;
using System;
using System.Collections.Generic;

namespace RedisMan.Terminal
{
    class Program
    {
        //
        static void Main(string[] args)
        {
            // these variables will be set when the command line is parsed
            bool shouldShowHelp = false;
            var connection = new ConnectionOptions()
            {
                Hostname = "127.0.0.1",
                Port = 6379
            };

            var options = new OptionSet {
                { "h|hostname=", "the name of someone to greet.", value => {
                    if (value != null) connection.Hostname = value;
                } },
                { "p|port=", "the number of times to repeat the greeting.", (int port) => {
                    if (port != 0) connection.Port = port;
                }},
                { "a|password=", "increase debug message verbosity", password => {
                    if (!string.IsNullOrWhiteSpace(password)) connection.Password = password;
                } },
                { "help", "show this message and exit", h => shouldShowHelp = h != null },
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("RedisMan.Terminal.exe: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `RedisMan.Terminal.exe --help' for more information.");
                return;
            }


            if (shouldShowHelp)
            {
                ShowHelp(options);
                return;
            }

            Repl(connection);
        }

        private static void Repl(ConnectionOptions connection)
        {
            LineEditor le = new LineEditor("redis");
            string line;
            // Prompts the user for input
            while ((line = le.Edit($"{connection.Hostname}:{connection.Port}> ", "")) != null)
            {
                Console.WriteLine("Your Input: [{0}]", line) ;
            }
        }


        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: RedisMan.Terminal.exe [OPTIONS]+ message");
            Console.WriteLine("Redis Client.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    }
}
