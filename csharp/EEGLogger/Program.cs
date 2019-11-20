using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Text;
using CortexAccess;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Timers;

namespace EEGLogger
{
    class Program
    {
        const string OutFilePath = @"EEGLogger.csv";
        private static FileStream OutFileStream;
        private static Dictionary<string, ArrayList> wholeData = new Dictionary<string, ArrayList>();
        private static System.Timers.Timer aTimer;
        

        static void Main(string[] args)
        {
            Console.WriteLine("EEG LOGGER");
            Console.WriteLine("Please wear Headset with good signal!!!");

            // Delete Output file if existed
            if (File.Exists(OutFilePath))
            {
                File.Delete(OutFilePath);
            }
            OutFileStream = new FileStream(OutFilePath, FileMode.Append, FileAccess.Write);
            
            DataStreamExample dse = new DataStreamExample();
            dse.AddStreams("mot");
            dse.AddStreams("pow");
            dse.OnSubscribed += SubscribedOK;
            //dse.OnEEGDataReceived += OnEEGDataReceived;
            dse.OnBandPowerDataReceived += OnBandPowerDataReceived;
            dse.OnMotionDataReceived += OnMotionDataReceived;
            dse.Start();

            Console.WriteLine("Press Esc to exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }

            // Unsubcribe stream
            dse.UnSubscribe();
            Thread.Sleep(5000);

            // Close Session
            dse.CloseSession();
            Thread.Sleep(5000);
            // Close Out Stream
            OutFileStream.Dispose();
        }

        private static void SubscribedOK(object sender, Dictionary<string, JArray> e)
        {
            SetTimer();
            ArrayList header = new ArrayList();
            header.Insert(0, "Timestamp");
            foreach (string key in e.Keys)
            {
                switch (key)
                {
                    case "eeg":
                        // print header
                        header.AddRange(e[key].ToObject<ArrayList>());
                        break;
                    case "mot":
                        // print header
                        header.AddRange(e[key].ToObject<ArrayList>());
                        break;
                    case "pow":
                        // print header
                        header.AddRange(e[key].ToObject<ArrayList>());
                        break;
                }
            }
            WriteHeaderToFile(header);
        }
        
        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(4000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            WriteDataToFile();
        }

        // Write Header to File
        private static void WriteHeaderToFile(ArrayList data)
        {
            int i = 0;
            for (; i < data.Count - 1; i++)
            {
                byte[] val = Encoding.UTF8.GetBytes(data[i].ToString() + ", ");
                if (OutFileStream != null)
                    OutFileStream.Write(val, 0, val.Length);
                else
                    break;
            }
            // Last element
            byte[] lastVal = Encoding.UTF8.GetBytes(data[i].ToString() + "\n");
            OutFileStream?.Write(lastVal, 0, lastVal.Length);
        }
        
        // Write Header to File
        private static void WriteDataToFile()
        {
            foreach (var element in wholeData)
            {
                byte[] time = Encoding.UTF8.GetBytes(element.Key + ",");
                if (OutFileStream != null)
                    OutFileStream.Write(time, 0, time.Length);
                else
                    break;
                int i = 0;
                for (; i < element.Value.Count; i++)
                {
                    byte[] val = Encoding.UTF8.GetBytes(element.Value[i] + ",");
                    OutFileStream?.Write(val, 0, val.Length);
                }
                // Last element
                byte[] breakLine = Encoding.UTF8.GetBytes("\n");
                OutFileStream?.Write(breakLine, 0, breakLine.Length);
            }
        }

        private static void OnEEGDataReceived(object sender, ArrayList eegData)
        {
            StoreData(eegData);
        }
        
        private static void OnBandPowerDataReceived(object sender, ArrayList powerData)
        {
            StoreData(powerData);
        }
        
        private static void OnMotionDataReceived(object sender, ArrayList motionData)
        {
            StoreData(motionData);
        }

        private static void StoreData(ArrayList dataToAdd)
        {
            string time = dataToAdd[0].ToString();
            dataToAdd.RemoveAt(0);
            if (wholeData.ContainsKey(time))
                wholeData[time].AddRange(dataToAdd);
            else
                wholeData.Add(time, dataToAdd);

        }

    }
}
