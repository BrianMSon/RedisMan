using Mono.Options;
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
            string hostname, password;
            int port;
            bool shouldShowHelp = false;


            var options = new OptionSet {
                { "h|hostname=", "the name of someone to greet.", value => { hostname = value; } },
                { "p|port=", "the number of times to repeat the greeting.", (int p) => port = p },
                { "a|password=", "increase debug message verbosity", value => { password = value; } },
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

            Console.WriteLine();
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
