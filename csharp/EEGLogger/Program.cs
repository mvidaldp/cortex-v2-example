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
        private static Dictionary<string, ArrayList> wholeData = new Dictionary<string, ArrayList>();
        

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
            //dse.AddStreams("eeg");
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
            WriteDataToFile();
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
        private static void WriteDataToFile()
        {
            header.RemoveAt(0); // remove timestamp header element
            
            foreach (var element in wholeData)
            {
                byte[] timeStamp = Encoding.UTF8.GetBytes(element.Key + ",");
                OutFileStream?.Write(timeStamp, 0, timeStamp.Length);
                foreach (var col in header)
                {
                    // TODO: Fix data storing on the right columns. Not working
                    byte[] val;
                    foreach (var value in element.Value)
                    {
                        KeyValuePair<string, string> current = (KeyValuePair<string, string>) value;
                        if (col.ToString() == current.Key)
                            val = Encoding.UTF8.GetBytes(current.Value + ",");
                        else
                        {
                            val = Encoding.UTF8.GetBytes( ",");
                            OutFileStream?.Write(val, 0, val.Length);
                            break;
                        }
                        OutFileStream?.Write(val, 0, val.Length);
                    }
                    
                    
                }
                // Last element
                byte[] breakLine = Encoding.UTF8.GetBytes("\n");
                OutFileStream?.Write(breakLine, 0, breakLine.Length);
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
            ArrayList dataPairs = new ArrayList();
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
                KeyValuePair<string, string> current = new KeyValuePair<string, string>(col, row);
                dataPairs.Add(current);
                i++;
            }
            if (wholeData.ContainsKey(time))
                wholeData[time].AddRange(dataPairs);
            else
                wholeData.Add(time, dataPairs);

        }

    }
}
