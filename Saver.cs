using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                using (stream)
                {
                    CurrentState = formater.Deserialize(stream) as State;
                }
            }
            catch (Exception)
            {
                CurrentState = null;
            }

            if (CurrentState != null) 
                return;
            CurrentState = new State()
            {
                CurrentIndex = 0,
                Items = new SortedDictionary<int, Item>()
            };
            CurrentState.Items.Add(0, new Item()
            {
                Index = 0,
                Branch = "a",
                FilePath = "",
                CreationTime = DateTime.Now,
                Children = new HashSet<int>(),
                Note = null
            });
        }
        
        public string Save(string src, DateTime when)
        {
            Thread.Sleep(3000);
            var currentItem = CurrentState.Items[CurrentState.CurrentIndex];
            var branch = currentItem.Branch;
            if (currentItem.Children.Count > 0)
            {
                // moving current branch to next branch
                branch = CurrentState.NextBranch;
                
                // calculate next branch
                var branchLength = branch.Length;
                var newBranch = new StringBuilder();
                var increased = false;
                for (var i = branchLength - 1; i >= 0; i--)
                {
                    if (!increased && branch[i] < 'z')
                    {
                        newBranch.Insert(0, (char)(branch[i] + 1));
                        increased = true;
                        continue;
                    }

                    newBranch.Insert(0, !increased ? 'a' : branch[i]);
                }
                if (!increased)
                    newBranch.Insert(0, 'a');
                
                CurrentState.NextBranch = newBranch.ToString();
            }
            
            var newItem = new Item()
            {
                Index = CurrentState.Items.Last().Key + 1,
                Branch = branch,
                FilePath = string.Format(OutputPath, when.ToString("yyyyMMddHHmmss")),
                CreationTime = when,
                Children = new HashSet<int>(),
                Note = null
            };
            File.Copy(src, newItem.FilePath);
            CurrentState.Items.Add(newItem.Index, newItem);
            CurrentState.CurrentIndex = newItem.Index;
            currentItem.Children.Add(newItem.Index);
            
            var stream = File.OpenWrite(string.Format(OutputPath, "state"));
            var formater = new BinaryFormatter();
            using (stream)
            {
                formater.Serialize(stream, CurrentState);
            }

            return $"\r    *  {newItem.CreationTime:HH:mm}  {newItem.Index}{newItem.Branch}\n";
        }

        public string Restore(int index, string dst)
        {
            if (index == 0)
                throw new IndexOutOfRangeException("no such save.");
            
            var item = CurrentState.Items[index];
            File.Copy(item.FilePath, dst, true);
            CurrentState.CurrentIndex = index;
            
            return $"\r{index}{item.Branch} restored.\n";
        }

        public string Milestone(string note)
        {
            var item = CurrentState.Items[CurrentState.CurrentIndex];
            item.Note = note;
            return $"\r    *  {item.CreationTime:HH:mm}  {item.Index}{item.Branch}\n    └─ {item.Note}\n";
        }

        public string List()
        {
            var list = new StringBuilder();
            foreach (var kv in CurrentState.Items)
            {
                if (kv.Key == 0)
                    continue;

                list.AppendFormat("\r    *  {2:HH:mm}  {0}{1}\n", kv.Key, kv.Value.Branch, kv.Value.CreationTime);
                if (kv.Value.Note != null)
                    list.AppendFormat("\r    └─ {0}\n", kv.Value.Note);
            }

            return list.ToString();
        }
    }

    [Serializable]
    internal class State
    {
        public int CurrentIndex { get; set; }
        public string NextBranch { get; set; }
        public SortedDictionary<int, Item> Items { get; set; }
    }

    [Serializable]
    internal class Item
    {
        public int Index { get; set; }
        public string Branch { get; set; }
        public string FilePath { get; set; }
        public DateTime CreationTime { get; set; }
        public HashSet<int> Children { get; set; }
        public string Note { get; set; }
    }
}