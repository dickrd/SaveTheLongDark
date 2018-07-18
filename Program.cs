using System;
using System.IO;

namespace SaveTheLongDark
{
    internal static class Program
    {
        private static DateTime _lastChanged = DateTime.Now;
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Write("usage: savethelongdark <save_directory> <save_file>");
                return;
            }

            Saver saver;
            try
            {
                Console.Write("\r==> loading state...\n");
                saver = new Saver(args[1]);
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    try
                    {
                        saver.SerializeState();
                    }
                    catch (Exception e)
                    {
                        Console.Write("\rstate not saved: {0}\n", e.Message.ToLower());
                    }
                };
                var watcher = new FileSystemWatcher
                {
                    Path = args[0],
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = args[1]
                };
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
                Console.Write(saver.GenerateItemInfo());
            }
            catch (Exception e)
            {
                Console.Write("\rstate loading failed: {0}", e.Message.ToLower());
                return;
            }

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
                else if (line.StartsWith("keep "))
                {
                    try
                    {
                        var keep = int.Parse(line.Substring("keep ".Length));
                        var message = saver.ClearOldSave(keep);
                        Console.Write(message);
                    }
                    catch (Exception e)
                    {
                        Console.Write("\r{0} failed: {1}\n", line, e.Message.ToLower());
                    }
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
                else if (line == "help")
                {
                    Console.Write("\r    list\t\tlist all saves\n" +
                                  "    milestone <event>\tadd milestone to a save\n" +
                                  "    keep <count>\tkeep only latest <count> saves\n" +
                                  "    restore <id>\trestore <id> as current save\n");
                }
                else
                {
                    Console.Write("\r{0} is not regconised. try: help\n", line);
                }
            }
        }
    }
}
