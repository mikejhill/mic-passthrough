using System;
using System.Runtime.InteropServices;

namespace MicPassthrough
{
    /// <summary>
    /// Windows COM interface for setting default audio devices.
    /// This is the ONLY way to programmatically set Windows default devices.
    /// Registry approach does NOT work - Windows ignores those values.
    /// </summary>
    public static class PolicyConfigClient
    {
        /// <summary>
        /// Sets a device as the Windows default device for the specified role and flow.
        /// Uses undocumented IPolicyConfigVista COM interface.
        /// </summary>
        public static bool SetDefaultDevice(string deviceId, ERole role = ERole.eConsole)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                IPolicyConfig policyConfig = null;
                
                // Try Windows 10/11 interface first
                try
                {
                    policyConfig = (IPolicyConfig)new CPolicyConfigClient();
                }
                catch
                {
                    // Fall back to Windows 7/8 interface
                    policyConfig = (IPolicyConfig)new CPolicyConfigVistaClient();
                }

                if (policyConfig == null)
                {
                    return false;
                }

                // Set as default for all audio roles to ensure it takes effect
                policyConfig.SetDefaultEndpoint(deviceId, role);
                
                Marshal.ReleaseComObject(policyConfig);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Audio endpoint role (Console = general use, Multimedia = media, Communications = VoIP)
    /// </summary>
    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2,
        ERole_enum_count = 3
    }

    /// <summary>
    /// Data flow direction (Render = speakers/output, Capture = microphones/input)
    /// </summary>
    public enum EDataFlow
    {
        eRender = 0,    // Playback devices (speakers, headphones)
        eCapture = 1,   // Recording devices (microphones)
        eAll = 2        // All devices
    }

    [ComImport, Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
    internal class CPolicyConfigVistaClient
    {
    }

    [ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    internal class CPolicyConfigClient
    {
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    internal interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string pszDeviceName, IntPtr ppFormat);

        [PreserveSig]
        int GetDeviceFormat(string pszDeviceName, bool bDefault, IntPtr ppFormat);

        [PreserveSig]
        int ResetDeviceFormat(string pszDeviceName);

        [PreserveSig]
        int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr mixFormat);

        [PreserveSig]
        int GetProcessingPeriod(string pszDeviceName, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);

        [PreserveSig]
        int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);

        [PreserveSig]
        int GetShareMode(string pszDeviceName, IntPtr pMode);

        [PreserveSig]
        int SetShareMode(string pszDeviceName, IntPtr mode);

        [PreserveSig]
        int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetDefaultEndpoint(string pszDeviceName, ERole role);

        [PreserveSig]
        int SetEndpointVisibility(string pszDeviceName, bool bVisible);
    }

    /// <summary>
    /// Windows 10+ version of IPolicyConfig with proper EDataFlow support
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    internal interface IPolicyConfig2
    {
        [PreserveSig]
        int GetMixFormat(string pszDeviceName, IntPtr ppFormat);

        [PreserveSig]
        int GetDeviceFormat(string pszDeviceName, bool bDefault, IntPtr ppFormat);

        [PreserveSig]
        int ResetDeviceFormat(string pszDeviceName);

        [PreserveSig]
        int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr mixFormat);

        [PreserveSig]
        int GetProcessingPeriod(string pszDeviceName, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);

        [PreserveSig]
        int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);

        [PreserveSig]
        int GetShareMode(string pszDeviceName, IntPtr pMode);

        [PreserveSig]
        int SetShareMode(string pszDeviceName, IntPtr mode);

        [PreserveSig]
        int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);

        [PreserveSig]
        int SetDefaultEndpoint(string pszDeviceName, ERole role);

        [PreserveSig]
        int SetEndpointVisibility(string pszDeviceName, bool bVisible);
    }
}
