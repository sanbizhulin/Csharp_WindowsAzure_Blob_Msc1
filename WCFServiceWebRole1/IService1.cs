using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace WCFServiceWebRole1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService1
    {

        [OperationContract]
        string ListFoldersInRootFolder();

        [OperationContract]
        string ListFilesInSpecificFolder(string cloudDirectoryName);
        [OperationContract]
        string UploadFile(string localFilePath, string fileName, string targetDirectory);

        [OperationContract]
        string UncompressZipFile(string fileName, string cloudDirectoryName);

        [OperationContract]
        string CompressFolder(string cloudDirectoryName);

        [OperationContract]
        string DownloadFile(string cloudDirectoryName, string fileName, string wholeDownloadToPath);

        [OperationContract]
        string DownloadZipFile(string cloudDirectoryName, string fileName, string wholeDownloadToPath);
        [OperationContract]
        string UploadFolder(string containerName, string localStoragePath, string prefixAzureDirectoryName);

        [OperationContract]
        string DownloadFolder(string cloudDirectoryName, string containerName);
   
        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);


    }


    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
