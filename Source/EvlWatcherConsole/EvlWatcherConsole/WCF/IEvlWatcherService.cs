using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Net;

namespace EvlWatcherConsole.WCF
{
    [ServiceContract]
    public interface IEvlWatcherService
    {
        [OperationContract]
        bool GetIsRunning();
        [OperationContract]
        IPAddress[] GetTemporarilyBannedIPs();
        [OperationContract]
        IPAddress[] GetPermanentlyBannedIPs();
        [OperationContract]
        void SetPermanentBan(IPAddress address);
        [OperationContract]
        void ClearPermanentBan(IPAddress address);
        [OperationContract]
        void AddWhiteListEntry(string filter);
        [OperationContract]
        void RemoveWhiteListEntry(string filter);
        [OperationContract]
        string[] GetWhiteListEntries();
        [OperationContract]
        KeyValuePair<DateTime, string>[] GetProtocolSince(DateTime time);
        [OperationContract]
        Dictionary<string, Dictionary<string, string>> GetTaskConfigurableProperties();
        [OperationContract]
        void SetTaskProperty(string task, string property, int value);
        [OperationContract]
        int GetTaskProperty(string task, string property);
    }
}