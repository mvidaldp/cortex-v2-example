using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CortexAccess
{
    public class DataStreamExample
    {
        private CortexClient _ctxClient;
        private List<string> _streams;
        private string _cortexToken;
        private string _sessionId;

        private HeadsetFinder _headsetFinder;
        private Authorizer _authorizer;
        private SessionCreator _sessionCreator;

        public List<string> Streams
        {
            get
            {
                return _streams;
            }

            set
            {
                _streams = value;
            }
        }

        public string SessionId
        {
            get
            {
                return _sessionId;
            }
        }

        // Event
        public event EventHandler<ArrayList> OnEEGDataReceived; // eeg data
        public event EventHandler<ArrayList> OnMotionDataReceived; // motion data
        public event EventHandler<ArrayList> OnDevDataReceived; // contact quality data
        public event EventHandler<ArrayList> OnBandPowerDataReceived; // band power
        public event EventHandler<ArrayList> OnPerfDataReceived; // performance metric
        public event EventHandler<ArrayList> OnMentalDataReceived; // mental commands
        public event EventHandler<ArrayList> OnFacialDataReceived; // mental commands
        public event EventHandler<ArrayList> OnSystemDataReceived; // mental commands
        public event EventHandler<Dictionary<string, JArray>> OnSubscribed;


        // Constructor
        public DataStreamExample() {
            _authorizer = new Authorizer();
            _headsetFinder = new HeadsetFinder();
            _sessionCreator = new SessionCreator();
            _cortexToken = "";
            _sessionId = "";
            _streams = new List<string>();
            // Event register
            _ctxClient = CortexClient.Instance;
            _ctxClient.OnErrorMsgReceived += MessageErrorRecieved;
            _ctxClient.OnStreamDataReceived += StreamDataReceived;
            _ctxClient.OnSubscribeData += SubscribeDataOK;
            _ctxClient.OnUnSubscribeData += UnSubscribeDataOK;

            _authorizer.OnAuthorized += AuthorizedOK;
            _headsetFinder.OnHeadsetConnected += HeadsetConnectedOK;
            _sessionCreator.OnSessionActivated += SessionActivatedOk;
            _sessionCreator.OnSessionClosed += SessionClosedOK;
        }

        private void SessionClosedOK(object sender, string sessionId)
        {
            Console.WriteLine("The Session " + sessionId + " has closed successfully.");
        }

        private void UnSubscribeDataOK(object sender, MultipleResultEventArgs e)
        {
            foreach (JObject ele in e.SuccessList)
            {
                string streamName = (string)ele["streamName"];
                if (_streams.Contains(streamName))
                {
                    _streams.Remove(streamName);
                }
            }
            foreach (JObject ele in e.FailList)
            {
                string streamName = (string)ele["streamName"];
                int code = (int)ele["code"];
                string errorMessage = (string)ele["message"];
                Console.WriteLine("UnSubscribe stream " + streamName + " unsuccessfully." + " code: " + code + " message: " + errorMessage);
            }
        }

        private void SubscribeDataOK(object sender, MultipleResultEventArgs e)
        {
            foreach (JObject ele in e.FailList)
            {
                string streamName = (string)ele["streamName"];
                int code = (int)ele["code"];
                string errorMessage = (string)ele["message"];
                Console.WriteLine("Subscribe stream " + streamName + " unsuccessfully." + " code: " + code + " message: " + errorMessage);
                if (_streams.Contains(streamName))
                {
                    _streams.Remove(streamName);
                }
            }
            Dictionary<string, JArray> header = new Dictionary<string, JArray>();
            foreach (JObject ele in e.SuccessList)
            {
                string streamName = (string)ele["streamName"];
                JArray cols = (JArray)ele["cols"];
                header.Add(streamName, cols);
            }
            if (header.Count > 0)
            {
                OnSubscribed(this, header);
            }
            else
            {
                Console.WriteLine("No Subscribe Stream Available");
            }
        }

        private void SessionActivatedOk(object sender, string sessionId)
        {
            // subscribe
            _sessionId = sessionId;
            _ctxClient.Subscribe(_cortexToken, _sessionId, Streams);
        }

        private void HeadsetConnectedOK(object sender, string headsetId)
        {
            //Console.WriteLine("HeadsetConnectedOK " + headsetId);
            // CreateSession
            _sessionCreator.Create(_cortexToken, headsetId);
        }

        private void AuthorizedOK(object sender, string cortexToken)
        {
            if (!String.IsNullOrEmpty(cortexToken))
            {
                _cortexToken = cortexToken;
                // find headset
                _headsetFinder.FindHeadset();
            }
        }

        private void StreamDataReceived(object sender, StreamDataEventArgs e)
        {
            Console.WriteLine(e.StreamName + " data received.");
            ArrayList data = e.Data.ToObject<ArrayList>();
            // insert timestamp to datastream
            data.Insert(0, e.Time);
            switch (e.StreamName)
            {
             case "eeg":
                 OnEEGDataReceived(this, data);
                 break;
             case "mot":
                 OnMotionDataReceived(this, data);
                 break;
             case "dev":
                 OnDevDataReceived(this, data);
                 break;
             case "pow":
                 OnBandPowerDataReceived(this, data);
                 break;
             case "met":
                 OnPerfDataReceived(this, data);
                 break;
             case "com":
                 OnMentalDataReceived(this, data);
                 break;
             case "fac":
                 OnFacialDataReceived(this, data);
                 break;
             case "sys":
                 OnSystemDataReceived(this, data);
                 break;
            }
        }
        private void MessageErrorRecieved(object sender, ErrorMsgEventArgs e)
        {
            Console.WriteLine("MessageErrorRecieved :code " + e.Code + " message " + e.MessageError);
        }

        // set Streams
        public void AddStreams(string stream)
        {
            if (!_streams.Contains(stream))
            {
                _streams.Add(stream);
            }
        }
        // start
        public void Start()
        {
            _authorizer.Start();
        }

        // Unsubscribe
        public void UnSubscribe(List<string> streams = null)
        {
            if (streams == null)
            {
                // unsubscribe all data
                _ctxClient.UnSubscribe(_cortexToken, _sessionId, _streams);
            }
            else 
                _ctxClient.UnSubscribe(_cortexToken, _sessionId, streams);
        }
        public void CloseSession()
        {
            _sessionCreator.CloseSession();
        }
    }
}
