using System;
using System.IO;

namespace SaveTheLongDark
{
    internal static class Program
    {
        private static DateTime _lastChanged = DateTime.Now;
        private static void Main(string[] args)
        {
            
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
                if (DateTime.Now - _lastChanged < TimeSpan.FromSeconds(5))
                    return;

                try
                {
                    _lastChanged = DateTime.Now;
                    var message = saver.Save(eventArgs.FullPath, _lastChanged);
                    Console.Write(message);
                }
                catch (Exception e)
                {
                    Console.Write("\r{0:HHmm} cannot be saved: {1}\n", _lastChanged, e.Message.ToLower());
                }
                Console.Write("--> ");
            };
            Console.CancelKeyPress += (sender, eventArgs) => saver.SerializeState();

            while (true)
            {
                Console.Write("--> ");
                var line = Console.ReadLine();
                if (line == null || line.Trim() == "exit")
                {
                    saver.SerializeState();
                    return;
                }

                line = line.Trim();
                if (line == "list")
                {
                    Console.Write(saver.List());
                }
                else if (line.StartsWith("milestone "))
                {
                    var note = line.Substring("milestone ".Length);
                    var message = saver.Milestone(note);
                    Console.Write(message);
                }
                else if (line.StartsWith("restore "))
                {
                    try
                    {
                        var index = int.Parse(line.Substring("restore ".Length));
                        _lastChanged = DateTime.Now;
                        var message = saver.Restore(index, Path.Combine(args[0], args[1]));
                        Console.Write(message);
                    }
                    catch (Exception e)
                    {
                        Console.Write("\r{0} failed: {1}\n", line, e.Message.ToLower());
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
