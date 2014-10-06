using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using foobalator;

namespace ClipIndexer
{
    public static class Worker
    {
        // BrianSp: The main thread will block on this handle until this event is set.  Normally get's set on service stop.
        private static ManualResetEvent m_End = new ManualResetEvent(false);

        public static readonly string Name = typeof(Worker).Assembly.GetName().ToString();

        // BrianSp: This is the main entry point for the main thread.  Spins up worker threads and waits for signal to stop.
        public static void Run()
        {
            Log.WriteLine(Name + " started");

            try
            {
                string value = SystemState.Settings.GetValue("Worker.Run.Directories");
                string[] dirs = value.Split('|');

                List<Thread> threads = new List<Thread>();

                foreach (string dir in dirs)
                {
                    Thread thread = new Thread(new ParameterizedThreadStart(IndexThread));
                    thread.Start(dir);
                    threads.Add(thread);
                }

                m_End.WaitOne();

                foreach (Thread thread in threads)
                    thread.Abort();
            }
            finally
            {
                Log.WriteLine(Name + " stopped");
            }
        }

        public static void End()
        {
            m_End.Set();
        }

        public static void IndexThread(object dir)
        {
            while (true)
            {
                Index((string)dir);
                System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }

        public static void Index(string dir)
        {
            Index(dir, TimeSpan.FromDays(1), true);
        }

        public static void Index(string dir, TimeSpan minAge, bool act)
        {
            DateTime now = DateTime.Now;

            string[] files = Directory.GetFiles(dir);
            foreach (string file in files)
            {
                try
                {
                    string name = Path.GetFileName(file);
                    // Expected format is: 'Label' yyyy mm dd 's' hh mm ss lll
                    // Example Back20121125s184423949.jpg
                    // or  Driveway20130817_050715.jpg

                    int i;
                    for (i = 0; i < name.Length; i++)
                    {
                        if (name[i] >= '0' && name[i] <= '9')
                            break;
                    }
                    string label = name.Substring(0, i);
                    int year = int.Parse(name.Substring(i, 4));

                    i += 4;
                    int month = int.Parse(name.Substring(i, 2));

                    i += 2;
                    int day = int.Parse(name.Substring(i, 2));

                    i += 2;
                    if (name[i] == 's' || name[i] == '_')
                        i += 1;

                    int hour = int.Parse(name.Substring(i, 2));

                    i += 2;
                    int minute = int.Parse(name.Substring(i, 2));

                    i += 2;
                    int second = int.Parse(name.Substring(i, 2));

                    i += 2;

                    if (name[i] == 'M')
                        i += 1;

                    int millisecond = 0;
                    if (name[i] != '.')
                    {
                        millisecond = int.Parse(name.Substring(i, 3));
                        i += 3;
                    }

                    string extension = name.Substring(i);
                    if (extension != ".jpg")
                        throw new Exception(string.Format("Expected '.jpg' at location {0}", i));

                    DateTime time = new DateTime(year, month, day, hour, minute, second, millisecond);
                    TimeSpan age = now - time;
                    if (age < minAge)
                        continue;

                    string destDir = Path.Combine(dir, string.Format("{0:D4}-{1:D2}-{2:D2} {3}", year, month, day, label));
                    string destFile = Path.Combine(destDir, name);

                    Log.WriteLine(string.Format("{0} -> {1}", file, destFile));

                    if (act)
                    {
                        if (!Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        File.Move(file, destFile);
                    }
                }
                catch (Exception e)
                {
                    Log.Write(e);
                }
            }
        }
    }
}