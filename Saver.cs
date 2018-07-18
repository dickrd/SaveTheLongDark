using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SaveTheLongDark
{
    public class Saver
    {
        private State CurrentState { get; set; }
        private string OutputPath { get; }

        public Saver(string slot)
        {
            OutputPath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Saved/{slot}" + "/{0}";
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

            if (CurrentState == null) 
                BuildState();
        }

        public string GenerateItemInfo(int index = -1)
        {
            if (index == -1)
                index = CurrentState.CurrentIndex;
            if (index == 0)
                return CurrentState.Items.Count == 1 ? $"\r       (empty)\n" : "";

            var item = CurrentState.Items[index];
            var info = $"\r    *  {item.CreationTime:HH:mm}  {item.Index}{item.Branch}\n";
            if (item.Note != null)
                info += $"\r    └─ {item.Note}\n";
            return info;
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
            SerializeState();

            return GenerateItemInfo(newItem.Index);
        }

        public string Restore(int index, string dst)
        {
            if (index == 0)
                throw new IndexOutOfRangeException("no such save.");
            
            var item = CurrentState.Items[index];
            File.Copy(item.FilePath, dst, true);
            CurrentState.CurrentIndex = index;
            SerializeState();
            
            return $"\r{index}{item.Branch} restored.\n";
        }

        public string Milestone(string note)
        {
            var item = CurrentState.Items[CurrentState.CurrentIndex];
            item.Note = note;
            SerializeState();
            
            return GenerateItemInfo();
        }

        public string List()
        {
            var list = new StringBuilder();
            foreach (var kv in CurrentState.Items)
            {
                list.Append(GenerateItemInfo(kv.Key));
            }

            return list.ToString();
        }

        public string ClearOldSave(int keepCount)
        {
            var deleted = 0;
            foreach (var kv in CurrentState.Items.Reverse())
            {
                if (keepCount > 0 || kv.Key >= CurrentState.CurrentIndex)
                {
                    keepCount--;
                }
                else if (kv.Key == 0)
                {
                    kv.Value.Children = new HashSet<int>();
                }
                else
                {
                    File.Delete(kv.Value.FilePath);
                    CurrentState.Items.Remove(kv.Key);
                    deleted++;
                }
            }
            SerializeState();

            return $"deleted: {deleted}, remaining: {CurrentState.Items.Count - 1}\n";
        }

        public void SerializeState()
        {
            var stream = File.OpenWrite(string.Format(OutputPath, "state"));
            var formater = new BinaryFormatter();
            using (stream)
            {
                formater.Serialize(stream, CurrentState);
            }
        }

        private void BuildState()
        {
            CurrentState = new State()
            {
                CurrentIndex = 0,
                NextBranch = "b",
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
            
            var pattern = new Regex(@"\d{14}");
            foreach (var filePath in Directory.EnumerateFiles(string.Format(OutputPath, "")))
            {
                var match = pattern.Match(filePath);
                if (!match.Success) 
                    continue;
                
                var currentItem = CurrentState.Items[CurrentState.CurrentIndex];
                var newItem = new Item()
                {
                    Index = currentItem.Index + 1,
                    Branch = currentItem.Branch,
                    FilePath = filePath,
                    CreationTime = DateTime.ParseExact(match.Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                    Children = new HashSet<int>(),
                    Note = null
                };
                CurrentState.Items.Add(newItem.Index, newItem);
                CurrentState.CurrentIndex = newItem.Index;
                currentItem.Children.Add(newItem.Index);
            }
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