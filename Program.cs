using System;
using System.IO;
using System.Threading;

namespace SaveTheLongDark
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var lastChanged = DateTime.Now;
            var watcher = new FileSystemWatcher
            {
                Path = args[0],
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = args[1]
            };
            var saver = new Saver(args[1]);
            
            watcher.Changed += (sender, eventArgs) =>
            {
                if (DateTime.Now - lastChanged < TimeSpan.FromSeconds(5))
                    return;
                
                lastChanged = DateTime.Now;
                var message = saver.Save(eventArgs.FullPath, lastChanged);
                Console.Write(message);
                Console.Write("--> ");
            };

            while (true)
            {
                Console.Write("--> ");
                var line = Console.ReadLine();
                if (line == "exit")
                    return;
                else if (line == "list")
                {
                    Console.Write(saver.List());
                }
                else if (line.StartsWith("restore"))
                {
                    try
                    {
                        var index = int.Parse(line.Substring("restore".Length));
                        saver.Restore(index, Path.Combine(args[0], args[1]));
                    }
                    catch (Exception e)
                    {
                        Console.Write("\rfailed to restore: {0}", e);
                    }
                }
                else
                {
                    Console.Write("\r{0} is not regconised. try: help\n", line);
                }
            }
        }
    }
}
