using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.IO.Compression;
using Microsoft.WindowsAzure;

namespace WCFServiceWebRole1
{
   
    public class Service1 : IService1
    {

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public static CloudBlobContainer connectContainer(string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer newContainer = blobClient.GetContainerReference(containerName);
            newContainer.CreateIfNotExists();
            newContainer.SetPermissions(new BlobContainerPermissions() {PublicAccess = BlobContainerPublicAccessType.Container});
            return newContainer;
        }

        public string ListFoldersInRootFolder()
        {
            var dirs = connectContainer("zycontainer").ListBlobs(null, false, BlobListingDetails.None);

           //filter all blobs, leave directory
            var folders = dirs.Where(d => d as CloudBlobDirectory != null).ToList();

            List<string> folderNameList = new List<string>();
            foreach (var item in folders)
            {
                CloudBlobDirectory directory = (CloudBlobDirectory)item;
                string folderName = directory.Prefix.ToString();
                /*to cut the last char of foldername
                if you don't cut, the return value of folder will be "X/Y/"
                 after you cut the last char, the return value of folder will bebe "X/Y",which looks like more beautiful*/
                string clearFolderName = folderName.Remove(folderName.Length - 1, 1);

                folderNameList.Add(clearFolderName);
            }
            return JsonConvert.SerializeObject(folderNameList);
        }

        public string ListFilesInSpecificFolder(string cloudDirectoryName)
        {

            CloudBlobDirectory dir = connectContainer("zycontainer").GetDirectoryReference(cloudDirectoryName);
            var list = dir.ListBlobs();
            List<string> fileNameList = new List<string>();

            foreach (var item in list)
            {

                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob file = (CloudBlockBlob)item;
                    fileNameList.Add(file.Name);
                }
                /* allthough we don't upload pageBlob in WCF Test Client,
                  but we could upload pageBlob in Server Explorer->Azure->Storage->Development->Blobs->zycontainer(right cilck this)-->View Blob Container-->Upload Blob
                 so we take the condition of pageblob into consideration*/
                if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob file = (CloudPageBlob)item;
                    fileNameList.Add(file.Name);
                }
                if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory dir1 = (CloudBlobDirectory)item;
                    string foldername1 = dir1.Prefix.ToString();
                    string clearfoldername1 = foldername1.Remove(foldername1.Length - 1, 1);
                    fileNameList.Add(clearfoldername1);
                }
            }
            return JsonConvert.SerializeObject(fileNameList);
        }

        public string UploadFile(string localFilePath, string fileName, string targetDirectory)
        {
         try{
               if (targetDirectory == null)
                {
                    return "You can not upload file to root path, please change the target directory.";
                }
               else
               {
                   CloudBlobDirectory targetDir = (CloudBlobDirectory)connectContainer("zycontainer").GetDirectoryReference(targetDirectory);
                CloudBlockBlob blockBlob = targetDir.GetBlockBlobReference(fileName);
                blockBlob.UploadFromFile(localFilePath,FileMode.Open);
                return "Upload file successfully";
               }
            }
             catch(Exception e)
              {
                return e.Message;
              }
        }

        public string UploadFolder(string containerName, string localStoragePath, string prefixAzureDirectoryName)
        {
            try {
            LocalResource Storage = RoleEnvironment.GetLocalResource("ZYLocalStorage");
            string[] filePaths = Directory.GetFiles(localStoragePath);

            foreach (var item in filePaths)
            {
                if (!Path.GetExtension(item).Equals(".zip"))
                {
                    UploadFile(item, Path.GetFileName(item), prefixAzureDirectoryName);

                }
            }

            var folder = new DirectoryInfo(localStoragePath);
            var subFolders = folder.GetDirectories();

            foreach (var directoryInfo in subFolders)
            {
                UploadFolder("zycontainer", Storage.RootPath + directoryInfo.Name, prefixAzureDirectoryName + "/" + directoryInfo.Name);

            }

            return "Upload Directory seccussfully.";
          }
            catch(Exception e)
            { return e.Message; }
        }


        public string DownloadFile(string cloudDirectoryName, string fileName, string wholeDownloadToPath)
        {
            try
            {
                CloudBlobDirectory cloudDir = (CloudBlobDirectory)connectContainer("zycontainer").GetDirectoryReference(cloudDirectoryName);
                CloudBlockBlob blockBlob = cloudDir.GetBlockBlobReference(fileName);
                blockBlob.DownloadToFile(wholeDownloadToPath, FileMode.Create);
                return "Download file successfully.";
            }
            catch (Exception e)
            { return e.Message; }

        }

        public string DownloadZipFile(string cloudDirectoryName, string fileName, string wholeDownloadToPath)
        {
            DownloadFile(cloudDirectoryName, fileName, wholeDownloadToPath);
            return "Download Zipfile successfully.";
        }


        /* DownloadFolder is not a function which is demanded in the project's requirement,
        but it will be called in itself and CompressFolder(), so I need to write this function
         Therefore, I make this function to a web service by the way*/
        public string DownloadFolder(string folderName, string containerName)
        {
            try
            {
                LocalResource localStorage = RoleEnvironment.GetLocalResource("ZYLocalStorage");
                CloudBlobDirectory dir = connectContainer("zycontainer").GetDirectoryReference(folderName);
                Directory.CreateDirectory(localStorage.RootPath + "zycontainer" + "\\" + folderName);
                var list = dir.ListBlobs();
                foreach (var item in list)
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob file = (CloudBlockBlob)item;
                        string fileNameOfBlockBlob = file.Parent.Uri.MakeRelativeUri(file.Uri).ToString();
                        string filePathOfBlockBlob = Path.Combine(localStorage.RootPath + "zycontainer" + "\\" + folderName, fileNameOfBlockBlob);
                        file.DownloadToFile(filePathOfBlockBlob, FileMode.Create);
                    }

                    if (item.GetType() == typeof(CloudPageBlob))
                    {
                        CloudPageBlob file = (CloudPageBlob)item;
                        string fileNameOfPageBlob = file.Parent.Uri.MakeRelativeUri(file.Uri).ToString();
                        string filePathOfPageBlob = Path.Combine(localStorage.RootPath + "zycontainer" + "\\" + folderName, fileNameOfPageBlob);
                        file.DownloadToFile(filePathOfPageBlob, FileMode.Create);
                    }

                    if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                        CloudBlobDirectory directory = (CloudBlobDirectory)item;
                        DownloadFolder(directory.Prefix.ToString(), containerName);
                    }
                }
                return localStorage.RootPath;

            }
            catch (Exception e)
            { return e.Message; }
        }


        public string UncompressZipFile(string fileName, string cloudDirectoryName)
        {

            try
            {
                LocalResource localStorage = RoleEnvironment.GetLocalResource("ZYLocalStorage");
                string filePath = Path.Combine(localStorage.RootPath, fileName);
                CloudBlobDirectory cloudDir = (CloudBlobDirectory)connectContainer("zycontainer").GetDirectoryReference(cloudDirectoryName);
                CloudBlockBlob cloudZipBlob= cloudDir.GetBlockBlobReference(fileName);
                cloudZipBlob.DownloadToFile(filePath, FileMode.Create);
                string foldername = Path.GetFileNameWithoutExtension(filePath);
                ZipFile.ExtractToDirectory(filePath, localStorage.RootPath);
                UploadFolder("zycontainer", localStorage.RootPath, foldername);
                return "Uncompress successfully.";
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }


        public string CompressFolder(string cloudDirectoryName)
        {
            DownloadFolder(cloudDirectoryName, "zycontainer");

            LocalResource localStorage = RoleEnvironment.GetLocalResource("ZYLocalStorage");
            string wholeDirectoryName = localStorage.RootPath + "zycontainer\\" + cloudDirectoryName + "\\";
            string zipFileFullPath = localStorage.RootPath + "zycontainer\\" + cloudDirectoryName + ".zip";
            ZipFile.CreateFromDirectory(wholeDirectoryName, zipFileFullPath, CompressionLevel.Fastest, true);
            UploadFile(zipFileFullPath, cloudDirectoryName + ".zip", "archives");
            File.Delete(zipFileFullPath);

            return "Comress directory successfully";
        }
  
    }
}
