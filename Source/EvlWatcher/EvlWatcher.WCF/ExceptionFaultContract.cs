using System.Runtime.Serialization;

namespace EvlWatcher.WCF
{

    public enum ExceptionFaultContractCode
    {
        clientNotAdministrator = 01
    } 

    [DataContract]
    public class ExceptionFaultContract
    {
        /// <summary>
        /// Fault Code List
        /// 
        /// 01 - Client is not an Administrator, Client will be stopped!
        /// 
        /// </summary>


        [DataMember]
        public ExceptionFaultContractCode Code { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool CanTerminate { get; set; }

        public ExceptionFaultContract(ExceptionFaultContractCode _code, string _message, bool _canTerminate = false, string _description = "")
        {
            Code = _code;
            Message = _message;
            Description = _description;
            CanTerminate = _canTerminate;
        }
    }
}
