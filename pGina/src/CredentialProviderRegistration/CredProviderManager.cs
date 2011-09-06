﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;

using log4net;

namespace pGina.CredentialProvider.Registration
{

    public abstract class CredProviderManager
    {
        public Settings CpInfo { get; set; }

        public CredProviderManager()
        {
            CpInfo = new Settings();
        }

        public static CredProviderManager GetManager()
        {
            if (Abstractions.Windows.OsInfo.IsWindows())
            {
                if (Abstractions.Windows.OsInfo.IsVistaOrLater())
                    return new DefaultCredProviderManager();
                else
                    return new GinaCredProviderManager();
            }
            else
            {
                throw new Exception("Must be executed on a Windows OS.");
            }
        }

        public void ExecuteDefaultAction()
        {
            switch (CpInfo.OpMode)
            {
                case OperationMode.INSTALL:
                    this.Install();
                    break;
                case OperationMode.UNINSTALL:
                    this.Uninstall();
                    break;
                case OperationMode.DISABLE:
                    this.Disable();
                    break;
                case OperationMode.ENABLE:
                    this.Enable();
                    break;
            }
        }

        public abstract void Install();
        public abstract void Uninstall();
        public abstract void Disable();
        public abstract void Enable();

        public abstract bool Registered();
        public abstract bool Registered6432();
        public abstract bool Enabled();
        public abstract bool Enabled6432();
    }

    public class GinaCredProviderManager : CredProviderManager
    {

        public override void Install()
        {
            throw new NotImplementedException();
        }

        public override void Uninstall()
        {
            throw new NotImplementedException();
        }

        public override void Disable()
        {
            throw new NotImplementedException();
        }

        public override void Enable()
        {
            throw new NotImplementedException();
        }

        public override bool Registered()
        {
            throw new NotImplementedException();
        }

        public override bool Registered6432()
        {
            throw new NotImplementedException();
        }

        public override bool Enabled()
        {
            throw new NotImplementedException();
        }

        public override bool Enabled6432()
        {
            throw new NotImplementedException();
        }
    }

    public class DefaultCredProviderManager : CredProviderManager
    {
        /*
         [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{781A7B48-79A7-4fcf-92CC-A6977171F1A8}]
         @="pGinaCredentialProvider"

         [HKEY_CLASSES_ROOT\CLSID\{781A7B48-79A7-4fcf-92CC-A6977171F1A8}]
         @="pGinaCredentialProvider"

         [HKEY_CLASSES_ROOT\CLSID\{781A7B48-79A7-4fcf-92CC-A6977171F1A8}\InprocServer32]
         @="SampleCredUICredentialProvider.dll"
         "ThreadingModel"="Apartment"
        */

        private ILog m_logger = LogManager.GetLogger("DefaultCredProviderManager");

        static readonly string PROVIDER_KEY_BASE = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers";
        static readonly string CLSID_BASE = @"CLSID";
        static readonly string PROVIDER_KEY_BASE_6432 = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers";

        // The registry keys
        string ProviderKey 
        { 
            get { return string.Format(@"{0}\{{{1}}}", PROVIDER_KEY_BASE, CpInfo.ProviderGuid.ToString()); }
        }
        string ClsidRoot
        {
            get { return string.Format(@"{0}\{{{1}}}", CLSID_BASE, CpInfo.ProviderGuid.ToString()); }
        }
        string ClsidInProc
        {
            get { return string.Format(@"{0}\InprocServer32", this.ClsidRoot); }
        }
        string ProviderKey6432
        {
            get { return string.Format(@"{0}\{{{1}}}", PROVIDER_KEY_BASE_6432, CpInfo.ProviderGuid.ToString()); }
        }

        public DefaultCredProviderManager()
        {
            // Defaults for pGina Credential Provider
            this.CpInfo.ProviderGuid = new Guid("{D0BEFEFB-3D2C-44DA-BBAD-3B2D04557246}");
            this.CpInfo.ShortName = "pGinaCredentialProvider";
        }

        public override void Install()
        {
            m_logger.InfoFormat("Installing credential provider {0} {{{1}}}",
                CpInfo.ShortName,
                CpInfo.ProviderGuid.ToString());

            // Copy the DLL
            if (Abstractions.Windows.OsInfo.Is64Bit()) // Are we on a 64 bit OS?
            {
                FileInfo x64Dll = DllUtils.Find64BitDll(CpInfo.Path, CpInfo.ShortName);
                FileInfo x32Dll = DllUtils.Find32BitDll(CpInfo.Path, CpInfo.ShortName);
                string destination64 = String.Format(@"C:\Windows\System32\{0}.dll", CpInfo.ShortName);
                string destination32 = String.Format(@"C:\Windows\Syswow64\{0}.dll", CpInfo.ShortName);

                if (x64Dll == null && x32Dll == null)
                {
                    throw new Exception("No 64 or 32 bit DLL found in: " + CpInfo.Path);
                }

                if (x64Dll != null)
                {
                    m_logger.DebugFormat("Found 64 bit DLL: {0}", x64Dll.FullName);
                    m_logger.DebugFormat("   copying to: {0}", destination64);
                    File.Copy(x64Dll.FullName, destination64, true);
                }
                else
                {
                    m_logger.Error("WARNING: No 64 bit DLL found.");
                }

                if (x32Dll != null)
                {
                    m_logger.DebugFormat("Found 32 bit DLL: {0}", x32Dll.FullName);
                    m_logger.DebugFormat("   copying to: {0}", destination32);

                    File.Copy(x32Dll.FullName, destination32, true);

                    // Write registry key for 32 bit DLL
                    using (RegistryKey key = Registry.LocalMachine.CreateSubKey(this.ProviderKey6432))
                    {
                        key.SetValue("", CpInfo.ShortName);
                    }
                }
                else
                {
                    m_logger.Error("WARNING: No 32 bit DLL found.");
                }
            }
            else
            {
                FileInfo x32Dll = DllUtils.Find32BitDll(CpInfo.Path, CpInfo.ShortName);
                string destination = String.Format(@"C:\Windows\System32\{0}.dll", CpInfo.ShortName);

                if (x32Dll != null)
                {
                    m_logger.DebugFormat("Found 32 bit DLL: {0}", x32Dll.FullName);
                    m_logger.DebugFormat("   copying to: {0}", destination);

                    File.Copy(x32Dll.FullName, destination, true);
                }
                else
                {
                    throw new Exception("No 32 bit DLL found in: " + CpInfo.Path);
                }
            }

            m_logger.Debug("Writing registry entries...");

            // Write registry values
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(this.ProviderKey))
            {
                m_logger.DebugFormat("{0} @=> {1}", key.ToString(), CpInfo.ShortName);
                key.SetValue("", CpInfo.ShortName);
            }
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(this.ClsidRoot))
            {
                m_logger.DebugFormat("{0} @=> {1}", key.ToString(), CpInfo.ShortName);
                key.SetValue("", CpInfo.ShortName);
            }
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(this.ClsidInProc))
            {
                m_logger.DebugFormat("{0} @=> {1}", key.ToString(), CpInfo.ShortName);
                key.SetValue("", CpInfo.ShortName);
                m_logger.DebugFormat("{0} {1} => {2}", key.ToString(), "ThreadingModel", "Apartment");
                key.SetValue("ThreadingModel", "Apartment");
            }
        }

        public override void Uninstall()
        {
            m_logger.InfoFormat("Uninstalling credential provider {0} {{{1}}}",
                CpInfo.ShortName,
                CpInfo.ProviderGuid.ToString());

            string dll = String.Format(@"C:\Windows\System32\{0}.dll", CpInfo.ShortName);
            string dll6432 = String.Format(@"C:\Windows\Syswow64\{0}.dll", CpInfo.ShortName);

            if (File.Exists(dll))
            {
                m_logger.DebugFormat("Deleting: {0}", dll);
                File.Delete(dll);
            }
            if (File.Exists(dll6432))
            {
                m_logger.DebugFormat("Deleting: {0}", dll);
                File.Delete(dll);
            }

            string guid = "{" + CpInfo.ProviderGuid.ToString() + "}";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PROVIDER_KEY_BASE, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Deleting {0}\\{1}", key.ToString(), guid);
                    key.DeleteSubKey(guid, false);
                }
            }

            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(CLSID_BASE, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Deleting {0}\\{1}\\InprocServer32", key.ToString(), guid);
                    key.DeleteSubKey(guid + "\\InprocServer32", false);
                }
            }

            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(CLSID_BASE, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Deleting {0}\\{1}", key.ToString(), guid);
                    key.DeleteSubKey(guid, false);
                }
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(PROVIDER_KEY_BASE_6432, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Deleting {0}\\{1}", key.ToString(), guid);
                    key.DeleteSubKey(guid, false);
                }
            }
        }

        public override void Disable()
        {
            m_logger.InfoFormat("Disabling credential provider: {0} {{{1}}}",
                CpInfo.ShortName,
                CpInfo.ProviderGuid.ToString());

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Writing {0}: {1} => {2}", key.ToString(), "Disabled", 1);
                    key.SetValue("Disabled", 1);
                }
                else
                {
                    m_logger.Error("WARNING: No credential provider registry entry found for that GUID.");
                }
            }

            if (Abstractions.Windows.OsInfo.Is64Bit())
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey6432, true))
                {
                    if (key != null)
                    {
                        m_logger.DebugFormat("Writing {0}: {1} => {2}", key.ToString(), "Disabled", 1);
                        key.SetValue("Disabled", 1);
                    }
                    else
                    {
                        m_logger.Error("WARNING: No 32-bit registry entry found with that GUID.");
                    }
                }
            }
        }

        public override void Enable()
        {
            m_logger.InfoFormat("Enabling credential provider: {0} {{{1}}}",
                CpInfo.ShortName,
                CpInfo.ProviderGuid.ToString());

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey, true))
            {
                if (key != null)
                {
                    m_logger.DebugFormat("Deleting {0}: {1}", key.ToString(), "Disabled");

                    if (key.GetValue("Disabled") != null)
                        key.DeleteValue("Disabled");
                }
                else
                {
                    m_logger.Error("WARNING: Did not find a registry entry for that GUID.");
                }
            }

            if (Abstractions.Windows.OsInfo.Is64Bit())
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey6432, true))
                {
                    if (key != null)
                    {
                        m_logger.DebugFormat("Deleting {0}: {1}", key.ToString(), "Disabled");

                        if (key.GetValue("Disabled") != null)
                            key.DeleteValue("Disabled");
                    }
                    else
                    {
                        m_logger.Error("WARNING: Did not find a (32 bit) registry entry for that GUID.");
                    }
                }
            }
        }

        public override bool Registered()
        {
            bool result = false;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey))
            {
                if (key != null)
                    result = true;
            }
            return result;
        }

        public override bool Enabled()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey))
            {
                object value = key.GetValue("Disabled");
                if (value == null) return true;
                else
                {
                    return (int)value == 0;
                }
            }
        }

        public override bool Registered6432()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey6432))
            {
                return key != null;
            }
        }

        public override bool Enabled6432()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(this.ProviderKey6432))
            {
                object value = key.GetValue("Disabled");
                if (value == null) return true;
                else
                {
                    return (int)value == 0;
                }
            }
        }
    }
}