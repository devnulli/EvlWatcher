using System.Collections.Generic;
using System.ServiceModel;
using System.Net;
using EvlWatcher.WCF.DTO;

namespace EvlWatcher.WCF
{
    [ServiceContract]
    public interface IEvlWatcherService
    {
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        GlobalConfigDTO GetGlobalConfig();
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        bool GetIsRunning();
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        IPAddress[] GetTemporarilyBannedIPs();
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        IPAddress[] GetPermanentlyBannedIPs();
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        void SetPermanentBan(IPAddress address);
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        void ClearPermanentBan(IPAddress address);
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        void AddWhiteListEntry(string filter);
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        void RemoveWhiteListEntry(string filter);
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        string[] GetWhiteListEntries();
        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        IList<LogEntryDTO> GetConsoleHistory();

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDTO))]
        void SaveGlobalConfig(SeverityLevelDTO logLevel, int consoleBackLog, int checkInterval);
    }
}
