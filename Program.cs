using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Net;
using System.Text;

namespace DnsTool
{
    internal class Program
    {
        private static string action = "";
        private static string hostname = "";
        private static string ip = "";
        private static string domain = "";

        static void Main(string[] args)
        {            
            Dictionary<string, string> argDict = Helpers.ParseTheArguments(args); // dictionary to hold arguments
            if (argDict == null)
            {
                return;
            }

            // Argument parsing
            if ((args.Length > 0 && argDict.Count == 0) || argDict.ContainsKey("h"))
            {
                help();
                return;
            }
            if (argDict.ContainsKey("action"))
            {
                action = argDict["action"];
            }
            if (argDict.ContainsKey("hostname"))
            {
                hostname = argDict["hostname"];
            }
            if (argDict.ContainsKey("ip"))
            {
                ip = argDict["ip"];
            }
            if (argDict.ContainsKey("domain"))
            {
                domain = argDict["domain"];
            }
            if (domain == "" || hostname == "")
            {
                help();
                return;
            }

            // Get the Root DSE
            string domainPath = GetDefaultNamingContext();
            if (string.IsNullOrEmpty(domainPath))
            {
                Console.WriteLine("[-] Error retrieving RootDSE.");
                return;
            }

            string dnsZonePath = $"LDAP://DC={domain},CN=MicrosoftDNS,DC=DomainDnsZones,{domainPath}";            

            if (action == "add")
            {
                Console.WriteLine("[+] Add DNS record.");
                if (ip == "")
                {
                    help();
                    return;
                }
                createARecord(hostname, ip, dnsZonePath);
            }
            else if (action == "modify")
            {
                Console.WriteLine("[+] Modify DNS record.");
                if (ip == "")
                {
                    help();
                    return;
                }
                modifyARecord(hostname, ip, dnsZonePath);
            }
            else if (action == "delete")
            {
                Console.WriteLine("[+] Delete DNS record.");
                deleteARecord(hostname, dnsZonePath);
            }
            else if (action == "view")
            {
                Console.WriteLine("[+] View DNS record.");
                viewARecord(hostname, dnsZonePath);
            }
            else
            {
                Console.WriteLine("[-] Invalid action. Should be add, modify or delete.");
                return;
            }
        }

        private static void help()
        {
            Console.WriteLine("-action [add|delete|modify|view]\n-hostname <corpws1>\n-ip <1.2.3.4>\n-domain <domain.fqdn>");
        }

        private static bool createARecord(string hostname, string ip, string dnsZonePath)
        {
            try
            {
                DirectoryEntry parent = new DirectoryEntry(dnsZonePath);
                uint zoneSerial = 1;
                uint ttl = 3600; // 1 hour
                byte[] dnsRecordBytes = CreateARecordOctetString(ip, ttl, zoneSerial);
                DirectoryEntry dnsRecord = parent.Children.Add($"DC={hostname}", "dnsNode");
                dnsRecord.Properties["dnsRecord"].Add(dnsRecordBytes);
                dnsRecord.CommitChanges();
            }
            catch (Exception ex)
            {                
                Console.WriteLine($"[-] An error occurred: {ex.Message}");
                return false;
            }
            Console.WriteLine($"[+] Completed.");
            return true;
        }

        private static bool modifyARecord(string hostname, string ip, string dnsZonePath)
        {
            try
            {
                DirectoryEntry parent = new DirectoryEntry(dnsZonePath);
                uint zoneSerial = 1;
                uint ttl = 3600; // 1 hour
                byte[] dnsRecordBytes = CreateARecordOctetString(ip, ttl, zoneSerial);
                DirectoryEntry dnsRecord = parent.Children.Find($"DC={hostname}");
                dnsRecord.Properties["dnsRecord"].Value = dnsRecordBytes;
                dnsRecord.CommitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] An error occurred: {ex.Message}");
                return false;
            }
            Console.WriteLine($"[+] Completed.");
            return true;
        }

        private static bool deleteARecord(string hostname, string dnsZonePath)
        {
            try
            {
                DirectoryEntry parent = new DirectoryEntry(dnsZonePath);
                DirectoryEntry dnsRecord = parent.Children.Find($"DC={hostname}");
                parent.Children.Remove(dnsRecord);
                parent.CommitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] An error occurred: {ex.Message}");
                return false;
            }
            Console.WriteLine($"[+] Completed.");
            return true;
        }

        private static bool viewARecord(string hostname, string dnsZonePath)
        {
            try
            {
                DirectoryEntry parent = new DirectoryEntry(dnsZonePath);
                DirectoryEntry dnsRecord = parent.Children.Find($"DC={hostname}");
                byte[] recordBytes = dnsRecord.Properties["dnsRecord"].Value as byte[];
                if (recordBytes == null || recordBytes.Length < 24)
                {
                    Console.WriteLine($"[-] Invalid DNS record.");
                    return false; // Basic validation for minimum length
                }
                ushort recordType = BitConverter.ToUInt16(recordBytes, 2);
                uint ttl = BitConverter.ToUInt32(recordBytes, 8);
                const int dataOffset = 24;
                int dataLength = recordBytes.Length - dataOffset;
                string parsedData = null; ;
                if (recordType == 1)
                {
                    if (dataLength >= 4)
                    {
                        byte[] ipBytes = new byte[4];
                        Array.Copy(recordBytes, dataOffset, ipBytes, 0, 4);
                        parsedData = new IPAddress(ipBytes).ToString();
                    }
                }
                else if (recordType == 5)
                {
                    parsedData = Encoding.Unicode.GetString(recordBytes, dataOffset, dataLength).TrimEnd('\0');
                }
                Console.WriteLine($"Record Type: {recordType}, TTL: {ttl}, Data: {parsedData}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[-] An error occurred: {ex.Message}");
                return false;
            }
            return true;
        }

        private static byte[] CreateARecordOctetString(string ip, uint ttl, uint serial)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    // Header (everything except the data)
                    writer.Write((short)4);  // DataLength: 4 bytes for the IP
                    writer.Write((short)1);  // Type: 1 for A record
                    writer.Write((byte)5);   // Version: 5
                    writer.Write((byte)240); // Rank: 240 (0xF0)
                    writer.Write((short)0);  // Flags: 0
                    writer.Write(serial);    // Serial: The zone's current serial number
                    writer.Write(IPAddress.HostToNetworkOrder((uint)ttl));  // TTL in seconds (Big-Endian)                    
                    writer.Write((int)0);    // TimeStamp: 0 for static

                    // Data (the IP address bytes)
                    writer.Write(IPAddress.Parse(ip).GetAddressBytes());

                    return ms.ToArray();
                }
            }
        }

        public static string GetDefaultNamingContext()
        {
            try
            {
                // Bind to the RootDSE object of the default domain.
                using (var rootDse = new DirectoryEntry("LDAP://RootDSE"))
                {
                    // Retrieve the 'defaultNamingContext' property.
                    string namingContext = rootDse.Properties["defaultNamingContext"].Value as string;
                    return namingContext;
                }
            }
            catch (Exception ex)
            {
                // This will fail if the machine is not part of a domain.
                Console.WriteLine($"[-] An error occurred: {ex.Message}");
                return null;
            }
        }
    }
}
