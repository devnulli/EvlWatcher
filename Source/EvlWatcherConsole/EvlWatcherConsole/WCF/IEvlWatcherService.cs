using System.ServiceModel;
using System.Net;

namespace EvlWatcherConsole.WCF
{
    [ServiceContract]
    public interface IEvlWatcherService
    {
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        bool GetIsRunning();
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        IPAddress[] GetTemporarilyBannedIPs();
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        IPAddress[] GetPermanentlyBannedIPs();
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        void SetPermanentBan(IPAddress address);
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        void ClearPermanentBan(IPAddress address);
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        void AddWhiteListEntry(string filter);
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        void RemoveWhiteListEntry(string filter);
        [OperationContract]
        [FaultContract(typeof(ExceptionFaultContract))]
        string[] GetWhiteListEntries();
    }
}
