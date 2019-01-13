using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Net;

namespace EvlWatcher.WCF
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
    }
}
