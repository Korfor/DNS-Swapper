﻿using System;
using System.Collections.Generic;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace DNS_Swapper
{
    public class NetworkManagement
    {
        // Method to prepare the WMI query connection options.
        public static ConnectionOptions PrepareOptions()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.Default;
            options.EnablePrivileges = true;
            return options;
        }

        // Method to prepare WMI query management scope.
        public static ManagementScope PrepareScope(string machineName, ConnectionOptions options, string path)
        {
            ManagementScope scope = new ManagementScope();
            scope.Path = new ManagementPath(@"\\" + machineName + path);
            scope.Options = options;
            scope.Connect();
            return scope;
        }

        /// <summary>
        /// Set's a new IP Address and it's Submask of the local machine
        /// </summary>
        /// <param name="ip_address">The IP Address</param>
        /// <param name="subnet_mask">The Submask IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void setIP(string ip_address, string subnet_mask)
        {
            ConnectionOptions options = PrepareOptions();
            ManagementScope scope = PrepareScope(Environment.MachineName, options, @"\root\CIMV2");
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            mc.Scope = scope;
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    try
                    {
                        ManagementBaseObject setIP;
                        ManagementBaseObject newIP = mo.GetMethodParameters("EnableStatic");

                        newIP["IPAddress"] = new string[] { ip_address };
                        newIP["SubnetMask"] = new string[] { subnet_mask };

                        setIP = mo.InvokeMethod("EnableStatic", newIP, null);
                    }
                    catch (Exception)
                    {
                        throw;
                    }


                }
            }
        }
        /// <summary>
        /// Set's a new Gateway address of the local machine
        /// </summary>
        /// <param name="gateway">The Gateway IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void setGateway(string gateway)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    try
                    {
                        ManagementBaseObject setGateway;
                        ManagementBaseObject newGateway =
                            objMO.GetMethodParameters("SetGateways");

                        newGateway["DefaultIPGateway"] = new string[] { gateway };
                        newGateway["GatewayCostMetric"] = new int[] { 1 };

                        setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="NIC">NIC address</param>
        /// <param name="DNS">DNS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public static void setDNS(string NIC, string DNS)
        {
            ConnectionOptions options = PrepareOptions();
            ManagementScope scope = PrepareScope(Environment.MachineName, options, @"\root\CIMV2");
            ManagementPath managementPath = new ManagementPath("Win32_NetworkAdapterConfiguration");
            ObjectGetOptions objectGetOptions = new ObjectGetOptions();
            ManagementClass mc = new ManagementClass(scope, managementPath, objectGetOptions);
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    if (mo["Caption"].ToString().Contains(NIC))
                    {
                        try
                        {
                            ManagementBaseObject newDNS = mo.GetMethodParameters("SetDNSServerSearchOrder");
                            newDNS["DNSServerSearchOrder"] = DNS.Split(',');
                            ManagementBaseObject setDNS = mo.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            Console.ReadKey();
                            throw;
                        }
                    }
                }
            }
        }

        public static IPAddress getDNS()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                    foreach (IPAddress dnsAdress in dnsAddresses)
                    {
                        return dnsAdress;
                    }
                }
            }
            throw new InvalidOperationException("Unable to find DNS Address");
        }


        /// <summary>
        /// Set's WINS of the local machine
        /// </summary>
        /// <param name="NIC">NIC Address</param>
        /// <param name="priWINS">Primary WINS server address</param>
        /// <param name="secWINS">Secondary WINS server address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void setWINS(string NIC, string priWINS, string secWINS)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Caption"].Equals(NIC))
                    {
                        try
                        {
                            ManagementBaseObject setWINS;
                            ManagementBaseObject wins =
                            objMO.GetMethodParameters("SetWINSServer");
                            wins.SetPropertyValue("WINSPrimaryServer", priWINS);
                            wins.SetPropertyValue("WINSSecondaryServer", secWINS);

                            setWINS = objMO.InvokeMethod("SetWINSServer", wins, null);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
            }
        }


        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern UInt32 DnsFlushResolverCache();

        public static void FlushDNSCache() //This can be named whatever name you want and is the function you will call
        {
            UInt32 result = DnsFlushResolverCache();
        }
    }

    public class Adapters
    {
        public IEnumerable<String> net_adapters()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                yield return nic.Id;
            }
            yield break;
        }
    }
}