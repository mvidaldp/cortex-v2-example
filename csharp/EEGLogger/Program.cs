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
        private static System.Timers.Timer aTimer;
        private static ArrayList header = new ArrayList();
        private static Dictionary<string, ArrayList> headerKeys = new Dictionary<string, ArrayList>();
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> wholeData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        

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
            dse.AddStreams("eeg");
            dse.AddStreams("mot");
            dse.AddStreams("dev");
            dse.AddStreams("pow");
            dse.AddStreams("met");
            dse.AddStreams("com");
            dse.AddStreams("fac");
            dse.AddStreams("sys");
            dse.OnSubscribed += SubscribedOK;
            dse.OnEEGDataReceived += OnEEGDataReceived;
            dse.OnMotionDataReceived += OnMotionDataReceived;
            dse.OnDevDataReceived += OnDevDataReceived;
            dse.OnBandPowerDataReceived += OnBandPowerDataReceived;
            dse.OnPerfDataReceived += OnPerfDataReceived;
            dse.OnMentalDataReceived += OnMentalDataReceived;
            dse.OnFacialDataReceived += OnFacialDataReceived;
            dse.OnSystemDataReceived += OnSystemDataReceived;

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
            header.Insert(0, "Timestamp");
            foreach (string key in e.Keys)
            {
                ArrayList headersList = e[key].ToObject<ArrayList>();
                switch (key)
                {
                    case "eeg":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "mot":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "dev":
                        // in case of device information, the third element is a JArray of electrode names
                        // these values need to be rearranged to be properly stored as the header
                        JArray electrodes = (JArray)headersList[2];
                        headersList.RemoveAt(2);
                        foreach (var electrode in electrodes)
                        {
                            headersList.Add(electrode);
                        }
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "pow":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "met":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "com":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "fac":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                    case "sys":
                        header.AddRange(headersList);
                        headerKeys.Add(key, headersList);
                        break;
                }
            }
            // print header
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
            Dictionary<string, Dictionary<string, Dictionary<string, string>>> toWrite = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(wholeData);
            WriteDataToFile(toWrite);
        }

        // Write Header to File
        private static void WriteHeaderToFile(ArrayList data)
        {
            int i = 0;
            for (; i < data.Count - 1; i++)
            {
                byte[] val = Encoding.UTF8.GetBytes(data[i] + ", ");
                if (OutFileStream != null)
                    OutFileStream.Write(val, 0, val.Length);
                else
                    break;
            }
            // Last element
            byte[] lastVal = Encoding.UTF8.GetBytes(data[i] + "\n");
            OutFileStream?.Write(lastVal, 0, lastVal.Length);
        }
        
        // Write Data to File
        private static void WriteDataToFile(Dictionary<string, Dictionary<string, Dictionary<string, string>>> buffered)
        {
            ArrayList times = new ArrayList();
            foreach (var timeStamp in buffered) // timestamps
            {
                byte[] time = Encoding.UTF8.GetBytes(timeStamp.Key + ",");
                OutFileStream?.Write(time, 0, time.Length);
                foreach (var stream in timeStamp.Value) // streams
                {
                    foreach (var row in stream.Value) // headers
                    {
                        byte[] data = Encoding.UTF8.GetBytes(row.Value + ",");
                        OutFileStream?.Write(data, 0, data.Length);
                    }
                }
                byte[] breakLine = Encoding.UTF8.GetBytes("\n");
                OutFileStream?.Write(breakLine, 0, breakLine.Length);
                times.Add(timeStamp.Key);
            }
            foreach (var time in times)
            {
                wholeData.Remove(time.ToString());
            }
        }

        private static void OnEEGDataReceived(object sender, ArrayList eegData)
        {
            StoreData(eegData, "eeg");
        }
        
        private static void OnMotionDataReceived(object sender, ArrayList motionData)
        {
            StoreData(motionData, "mot");
        }
        
        private static void OnDevDataReceived(object sender, ArrayList deviceData)
        {
            StoreData(deviceData, "dev");
        }
        
        private static void OnBandPowerDataReceived(object sender, ArrayList powerData)
        {
            StoreData(powerData, "pow");
        }
        
        private static void OnPerfDataReceived(object sender, ArrayList performanceData)
        {
            StoreData(performanceData, "met");
        }
        
        private static void OnMentalDataReceived(object sender, ArrayList mentalData)
        {
            StoreData(mentalData, "com");
        }
        
        private static void OnFacialDataReceived(object sender, ArrayList facialData)
        {
            StoreData(facialData, "fac");
        }
        
        private static void OnSystemDataReceived(object sender, ArrayList systemData)
        {
            StoreData(systemData, "sys");
        }

        private static void StoreData(ArrayList dataToAdd, string stream)
        {
            string time = dataToAdd[0].ToString();
            dataToAdd.RemoveAt(0);
            Dictionary<string, string> dataPairs = new Dictionary<string, string>();
            // in case of device information, the third array element is a JArray of electrode contact quality values
            // these values need to be rearranged to stored accordingly
            if (stream == "dev")
            {
                JArray electrodes = (JArray) dataToAdd[2];
                dataToAdd.RemoveAt(2);
                foreach (var electrode in electrodes)
                {
                    dataToAdd.Add(electrode);
                }
            }
            int i = 0;
            foreach (var key in headerKeys[stream])
            {
                string col = key.ToString();
                string row = dataToAdd[i].ToString();
                dataPairs.Add(col, row);
                i++;
            }
            Dictionary<string, Dictionary<string, string>> current = new Dictionary<string, Dictionary<string, string>>{ [stream] = dataPairs };
            if (wholeData.ContainsKey(time))
                wholeData[time][stream] = dataPairs;
            else
            {
                // here we just fill out the whole data dictionary with the full structure
                // Example:
                /*
                 Dictionary<string, Dictionary<string, Dictionary<string, string>>> wholeData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                 Thus =>
                 wholeData = 
                 {
                    [timestamp] =
                    {
                        ["eeg"] =
                        {
                            ["header0"] = value,
                            ["header1"] = value,
                            ["header2"] = value
                            ...
                        }
                        ["mot"]
                        {
                            ...
                        }
                        ["pow"]
                        ...
                    }
                 }
                */
                Dictionary<string, Dictionary<string, string>> father = new Dictionary<string, Dictionary<string, string>>();
                foreach (var pair in headerKeys) 
                {
                   Dictionary<string, string> child = new Dictionary<string, string>();
                   foreach (var value in pair.Value)
                   {
                       child.Add(value.ToString(), null);
                   }
                   father.Add(pair.Key, child);
                }
                wholeData.Add(time, father);
                wholeData[time][stream] = dataPairs;
            }
                
        }

    }
}
