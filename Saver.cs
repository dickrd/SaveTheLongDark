using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace SaveTheLongDark
{
    public class Saver
    {
        private State CurrentState { get; }
        private string OutputPath { get; }

        public Saver(string slot)
        {
            OutputPath = $"./Saves/{slot}" + "/{0}";
            try
            {
                Directory.CreateDirectory(string.Format(OutputPath, ""));
                var stream = File.OpenRead(string.Format(OutputPath, "state"));
                var formater = new BinaryFormatter();
                CurrentState = formater.Deserialize(stream) as State;
            }
            catch (IOException)
            {
                CurrentState = null;
            }
            if (CurrentState == null)
                CurrentState = new State()
                {
                    LastIndex = 0,
                    LastBranch = "a",
                    Items = new SortedDictionary<int, Item>()
                };
        }
        
        public string Save(string src, DateTime when)
        {
            Thread.Sleep(3000);
            var item = new Item()
            {
                Index = CurrentState.LastIndex + 1,
                Branch = CurrentState.LastBranch,
                FilePath = string.Format(OutputPath, when.ToString("yyyyMMddHHmmss")),
                CreationTime = when
            };
            File.Copy(src, item.FilePath);
            CurrentState.Items.Add(item.Index, item);
            CurrentState.LastIndex = item.Index;
                
            return $"\r    *  {item.CreationTime:HH:mm}  {item.Index}{item.Branch}\n";
        }

        public string Restore(int index, string dst)
        {
            File.Copy(CurrentState.Items[index].FilePath, dst, true);
            var branchLength = CurrentState.LastBranch.Length;
            var newBranch = new StringBuilder();
            var increased = false;
            for (var i = branchLength - 1; i >= 0; i--)
            {
                if (CurrentState.LastBranch[i] < 'z')
                {
                    newBranch.Insert(0, (char)(CurrentState.LastBranch[i] + 1));
                    increased = true;
                }
                else
                {
                    newBranch.Insert(0, 'a');
                }
            }
            if (!increased)
            {
                newBranch.Insert(0, 'a');
            }
            CurrentState.LastBranch = newBranch.ToString();
            
            return $"\r{index}{CurrentState.Items[index].Branch} restored.\n";
        }

        public string List()
        {
            var list = new StringBuilder();
            foreach (var item in CurrentState.Items)
            {
                list.AppendFormat("\r    *  {2:HH:mm}  {0}{1}\n", item.Key, item.Value.Branch, item.Value.CreationTime);
            }

            return list.ToString();
        }
    }

    [Serializable]
    internal class State
    {
        public int LastIndex { get; set; }
        public string LastBranch { get; set; }
        public SortedDictionary<int, Item> Items { get; set; }
    }

    internal class Item
    {
        public int Index { get; set; }
        public string Branch { get; set; }
        public string FilePath { get; set; }
        public DateTime CreationTime { get; set; }
    }
}