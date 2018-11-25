// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Web;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ReactiveMachine.Tools.Taskometer
{
    public class TaskGroup
    {

        internal static long offset = 0;
        internal static long end = 0;

        internal static Dictionary<string, TaskGroup> TaskGroups = new Dictionary<string, TaskGroup>();

        internal List<Interval> Intervals = new List<Interval>();
        internal int seqnum;
        internal bool Enabled;
        internal string Name;

        internal int threadoffset;
        internal List<Interval> temp = new List<Interval>();

        /// <summary>
        /// Represents an individual interval.
        /// </summary>
        public class Interval : IComparable<Interval>
        {
            public long Start;
            public long End;
            public int Tid;
            public int reqno;
            public int filecount;

            public Interval(long start, long end, int reqno, int filecount)
            {
                this.Start = start;
                this.End = end;
                this.Tid = 0;
                this.reqno = reqno;
                this.filecount = filecount;
            }

            public int CompareTo(Interval other)
            {
                return Start.CompareTo(other.Start);
            }
        }

        public static int FindAvailableThread(List<long> threads, long begin, long end)
        {
            for (int i = 0; i < threads.Count; i++)
            {
                if (threads[i] <= begin)
                {
                    threads[i] = end;
                    return i;
                }
            }
            threads.Add(end);
            return threads.Count - 1;
        }

  
        public static bool ReadFromFile(string connectionString, string container, string folder, string pattern)
        {
          
            long minticks = long.MaxValue;
            long maxticks = 0;

            CloudStorageAccount storageaccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageaccount.CreateCloudBlobClient();

            var blobcontainer = blobClient.GetContainerReference(container);

            try
            {
                blobcontainer.FetchAttributes();
                // the container exists if no exception is thrown  
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }


            int taskmetercounter = 0;
            int filecount = 0;

            // read all events in all files
            String readFromFolder;
            if(folder.Contains("%"))
            {
                readFromFolder = Uri.UnescapeDataString(folder);
            }
            else
            {
                readFromFolder = folder;
            }

            Regex deploymentRegex = new Regex("^.*");
            foreach (var x in blobcontainer.ListBlobs(folder))
            {
                var blockblob = x as CloudBlockBlob;
                if (blockblob == null || !deploymentRegex.IsMatch(blockblob.Name))
                    continue;

                var serializer = new JsonSerializer();

                using (var blobstream = blockblob.OpenRead())
                using (var sr = new StreamReader(blobstream))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var content = (EventsBlobFormat)serializer.Deserialize(jsonTextReader, typeof(EventsBlobFormat));

                    bool callerOnly = true;

                    foreach (var e in content.Events)
                        if (!callerOnly || e.opSide == "Caller")
                        {
                            var groupname = $"{e.name}({e.opType})";
                            var reqno = e.name.GetHashCode();
                            var end = TimeSpan.FromMilliseconds(e.time).Ticks;
                            var start = end - TimeSpan.FromMilliseconds(e.duration).Ticks; ;

                            if (!callerOnly)
                                groupname = groupname + $"({e.opSide})";

                            if (minticks > start)
                                minticks = start;
                            if (maxticks < end)
                                maxticks = end;

                            if (string.IsNullOrEmpty(pattern) || groupname.Contains(pattern))
                            {
                                TaskGroup meter = null;
                                if (!TaskGroups.TryGetValue(groupname, out meter))
                                {
                                    meter = new TaskGroup() { Name = groupname, seqnum = taskmetercounter++ };
                                    TaskGroups[groupname] = meter;
                                }

                                meter.temp.Add(new Interval(start, end, reqno, filecount));
                            }
                        }
                }

                // sort intervals and assign thread
                foreach (var meter in TaskGroups.Values)
                {
                    meter.temp.Sort();
                    var threads = new List<long>();

                    foreach (var interval in meter.temp)
                        interval.Tid = meter.threadoffset + FindAvailableThread(threads, interval.Start, interval.End);

                    meter.threadoffset += threads.Count();

                    meter.Intervals.AddRange(meter.temp);
                    meter.temp.Clear();
                }

                filecount++;
            }

            if (minticks > maxticks)
            {
                minticks = 0;
                maxticks = 1;
            }

            TaskGroup.offset = minticks;
            TaskGroup.end = maxticks - minticks;

            return true;
         }


    }
}
