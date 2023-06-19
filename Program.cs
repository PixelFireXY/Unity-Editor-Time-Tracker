﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Time_tracker_app
{
    class Program
    {
        static DateTime startTime;
        static Process unityProcess = null;

        static string logPath = Path.Combine(Environment.CurrentDirectory, "UsageLogs.txt");

        static void Main(string[] args)
        {
            ExecuteTrackingBehaviour();
        }
        /******************** Tracking behaviour ********************/

        static void ExecuteTrackingBehaviour()
        {
            Console.WriteLine("Waiting for Unity to start...");

            while (unityProcess == null)
            {
                unityProcess = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Equals("Unity"));
                System.Threading.Thread.Sleep(1000); // check every second
            }

            startTime = DateTime.Now;
            Console.WriteLine("Unity started at: " + startTime);

            unityProcess.EnableRaisingEvents = true;
            unityProcess.Exited += (sender, eventArgs) => OnUnityExit();

            Console.WriteLine("Commands available:");
            Console.WriteLine("'session': Shows total time of current session.");
            Console.WriteLine("'today': Shows the total time of today.");
            Console.WriteLine("'total': Shows total time from the log file.");
            Console.WriteLine("'open': Opens the log file's folder.");
            Console.WriteLine("'stop': Stop the tracking.");

            while (unityProcess != null && !unityProcess.HasExited)
            {
                string command = Console.ReadLine();

                if (command.Equals("session"))
                {
                    TimeSpan currentSessionTime = DateTime.Now - startTime;
                    Console.WriteLine("Current session time: " + currentSessionTime.ToString(@"hh\:mm\:ss"));
                }
                else if (command == "today")
                {
                    var todayUsageTime = CalculateUsageTimeForToday();
                    Console.WriteLine($"Total usage time for today: {todayUsageTime:hh\\:mm\\:ss}");
                }
                else if (command.Equals("total"))
                {
                    TimeSpan totalTime = CalculateTotalTimeFromLog();
                    Console.WriteLine("Total time from the log file: " + totalTime.ToString(@"hh\:mm\:ss"));
                }
                else if (command.Equals("open"))
                {
                    string path = Path.GetFullPath("UsageLogs.txt");
                    string folderPath = Path.GetDirectoryName(path);
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else if (command.Equals("stop"))
                {
                    OnUnityExit();
                    Console.WriteLine("Tracking stopped. Press any key to close the console.");
                    _ = Console.ReadKey();
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Invalid command.");
                }
            }

            _ = Console.ReadKey();
        }

        static void OnUnityExit()
        {
            DateTime endTime = DateTime.Now;
            TimeSpan usageTime = endTime - startTime;

            Console.WriteLine("Unity exited at: " + endTime);
            Console.WriteLine("Total time of usage: " + usageTime.ToString(@"hh\:mm\:ss"));

            WriteToLog(startTime, endTime, usageTime);
        }

        static void WriteToLog(DateTime start, DateTime end, TimeSpan usageTime)
        {
            string path = "UsageLogs.txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine("Unity session started at " + start + " and ended at " + end);
                    writer.WriteLine("Total time of usage: " + usageTime.ToString(@"hh\:mm\:ss"));
                    writer.WriteLine("-------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Log correctly saved.");
            }
        }

        /******************** Utils ********************/

        static TimeSpan CalculateTotalTimeFromLog()
        {
            string path = "UsageLogs.txt";

            TimeSpan totalTime = new TimeSpan();

            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Total time of usage: "))
                        {
                            string time = line.Substring("Total time of usage: ".Length).Split(' ')[0];
                            string[] parts = time.Split(':');
                            totalTime += new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                        }
                    }
                }
            }

            return totalTime;
        }

        static TimeSpan CalculateUsageTimeForToday()
        {
            TimeSpan todayUsageTime = new TimeSpan();

            if (File.Exists(logPath))
            {
                using (StreamReader reader = new StreamReader(logPath))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Unity session started at "))
                        {
                            string date = line.Substring("Unity session started at ".Length).Split(' ')[0];
                            DateTime sessionDate;
                            if (DateTime.TryParse(date, out sessionDate) && sessionDate.Date == DateTime.Now.Date)
                            {
                                line = reader.ReadLine();  // Read the next line for the total time of usage
                                string time = line.Substring("Total time of usage: ".Length).Split(' ')[0];
                                string[] parts = time.Split(':');
                                todayUsageTime += new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                            }
                        }
                    }
                }
            }

            return todayUsageTime;
        }
    }
}
