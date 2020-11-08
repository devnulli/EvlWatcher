using System.Runtime.Serialization;

namespace EvlWatcher.WCF.DTO
{

    public enum ServiceErorCode
    {
        clientNotAdministrator = 01
    } 

    [DataContract]
    public class ServiceFaultDTO
    {
        /// <summary>
        /// Fault Code List
        /// 
        /// 01 - Client is not an Administrator, Client will be stopped!
        /// 
        /// </summary>


        [DataMember]
        public ServiceErorCode Code { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool CanTerminate { get; set; }

        public ServiceFaultDTO(ServiceErorCode _code, string _message, bool _canTerminate = false, string _description = "")
        {
            Code = _code;
            Message = _message;
            Description = _description;
            CanTerminate = _canTerminate;
        }
    }
}
