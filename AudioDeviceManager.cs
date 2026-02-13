using System.Runtime.InteropServices;

namespace AudioSwitcher;

// COM Interop for Windows Core Audio API
internal static class AudioDeviceManager
{
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid IID_IPolicyConfig = new("F8679F50-850A-41CF-9C72-430F290290C8");

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        int GetDevice(string pwstrId, out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IMMNotificationClient pClient);
        int UnregisterEndpointNotificationCallback(IMMNotificationClient pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        int GetCount(out int pcDevices);
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId(out IntPtr ppstrId);
        int GetState(out int pdwState);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        int GetCount(out int cProps);
        int GetAt(int iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant propvar);
        int Commit();
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string pstrDeviceId, IntPtr ppFormat);
        [PreserveSig]
        int GetDeviceFormat(string pstrDeviceId, bool bDefault, IntPtr ppFormat);
        [PreserveSig]
        int ResetDeviceFormat(string pstrDeviceId);
        [PreserveSig]
        int SetDeviceFormat(string pstrDeviceId, IntPtr pEndpointFormat, IntPtr MixFormat);
        [PreserveSig]
        int GetProcessingPeriod(string pstrDeviceId, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);
        [PreserveSig]
        int SetProcessingPeriod(string pstrDeviceId, IntPtr pmftPeriod);
        [PreserveSig]
        int GetShareMode(string pstrDeviceId, IntPtr pMode);
        [PreserveSig]
        int SetShareMode(string pstrDeviceId, IntPtr mode);
        [PreserveSig]
        int GetPropertyValue(string pstrDeviceId, bool bFxStore, ref PropertyKey key, out PropVariant pv);
        [PreserveSig]
        int SetPropertyValue(string pstrDeviceId, bool bFxStore, ref PropertyKey key, ref PropVariant pv);
        [PreserveSig]
        int SetDefaultEndpoint(string pstrDeviceId, ERole role);
        [PreserveSig]
        int SetEndpointVisibility(string pstrDeviceId, bool bVisible);
    }

    private interface IMMNotificationClient
    {
        void OnDeviceStateChanged(string pwstrDeviceId, int dwNewState);
        void OnDeviceAdded(string pwstrDeviceId);
        void OnDeviceRemoved(string pwstrDeviceId);
        void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId);
        void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public int pid;

        public PropertyKey(Guid fmtid, int pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public short vt;
        [FieldOffset(8)] public IntPtr pwszVal;

        public string? GetStringValue()
        {
            return Marshal.PtrToStringUni(pwszVal);
        }

        public void Clear()
        {
            PropVariantClear(ref this);
        }
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    private enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    private const int DEVICE_STATE_ACTIVE = 0x00000001;
    private static readonly PropertyKey PKEY_Device_FriendlyName = new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);

    public class AudioDevice
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }

    public static List<AudioDevice> GetPlaybackDevices()
    {
        var devices = new List<AudioDevice>();
        
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator);
            if (enumeratorType == null) return devices;

            var enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(enumeratorType);
            if (enumerator == null) return devices;

            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE_ACTIVE, out var collection);
            
            collection.GetCount(out int count);

            for (int i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                
                device.GetId(out IntPtr idPtr);
                string id = Marshal.PtrToStringUni(idPtr) ?? "";
                Marshal.FreeCoTaskMem(idPtr);

                device.OpenPropertyStore(0, out var propertyStore);
                var pk = PKEY_Device_FriendlyName;
                propertyStore.GetValue(ref pk, out var nameVariant);
                string name = nameVariant.GetStringValue() ?? "Unknown Device";
                nameVariant.Clear();

                devices.Add(new AudioDevice { Id = id, Name = name });

                Marshal.ReleaseComObject(propertyStore);
                Marshal.ReleaseComObject(device);
            }

            Marshal.ReleaseComObject(collection);
            Marshal.ReleaseComObject(enumerator);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enumerating devices: {ex.Message}");
        }

        return devices;
    }

    public static AudioDevice? GetDefaultPlaybackDevice()
    {
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator);
            if (enumeratorType == null) return null;

            var enumerator = (IMMDeviceEnumerator?)Activator.CreateInstance(enumeratorType);
            if (enumerator == null) return null;

            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);

            device.GetId(out IntPtr idPtr);
            string id = Marshal.PtrToStringUni(idPtr) ?? "";
            Marshal.FreeCoTaskMem(idPtr);

            device.OpenPropertyStore(0, out var propertyStore);
            var pk = PKEY_Device_FriendlyName;
            propertyStore.GetValue(ref pk, out var nameVariant);
            string name = nameVariant.GetStringValue() ?? "Unknown Device";
            nameVariant.Clear();

            Marshal.ReleaseComObject(propertyStore);
            Marshal.ReleaseComObject(device);
            Marshal.ReleaseComObject(enumerator);

            return new AudioDevice { Id = id, Name = name };
        }
        catch
        {
            return null;
        }
    }

    public static bool SetDefaultPlaybackDevice(string deviceId)
    {
        try
        {
            var policyConfigType = Type.GetTypeFromCLSID(new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9"));
            if (policyConfigType == null) return false;

            var policyConfig = (IPolicyConfig?)Activator.CreateInstance(policyConfigType);
            if (policyConfig == null) return false;

            // Set for all roles
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);

            Marshal.ReleaseComObject(policyConfig);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting default device: {ex.Message}");
            return false;
        }
    }
}