using FluentFTP;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Roadie.Library.Utility
{
    public static class FtpHelper
    {
        public static int Download(string ftpHost, string ftpDirectory, NetworkCredential credentials, string localPath)
        {
            int result = 0;
            using (FtpClient client = new FtpClient(ftpHost))
            {
                client.Credentials = credentials;
                client.Connect();

                var foundFiles = new List<string>();
                foreach (FtpListItem item in client.GetListing(ftpDirectory, FtpListOption.Recursive))
                {
                    switch (item.Type)
                    {
                        case FtpFileSystemObjectType.File:
                            foundFiles.Add(item.FullName);
                            break;
                    }
                }

                foreach (var file in foundFiles)
                {
                    var filenameWithFolder = file.Replace(ftpDirectory, string.Empty);
                    var fileFolder = Path.GetDirectoryName(filenameWithFolder);
                    var filename = Path.GetFileName(file);

                    var localPathForFile = Path.Combine(localPath, fileFolder);
                    var localFilename = Path.Combine(localPathForFile, filename);
                    Directory.CreateDirectory(localPathForFile);
                    client.DownloadFile(localFilename, file);
                    result++;
                }

                client.Disconnect();
            }
            return result;
        }
    }
}
