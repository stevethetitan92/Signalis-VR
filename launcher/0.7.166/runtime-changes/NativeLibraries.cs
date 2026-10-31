using System;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SignalisVrTracking
{
    // Modules deliberately remain loaded for process lifetime, as in the tested bridge.
    internal static class NativeLibraries
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetModuleFileNameW(IntPtr module, StringBuilder path, int size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string path, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
        private static IntPtr runtime, bridge;
        private static readonly object gate = new object();
        internal static string LibraryDirectory { get; private set; }
        internal const string RuntimeHash = "54AD7FE4CDB4D88FA818DBF8EB3D1F8CA2AE3B34D2FF115F87191D7D88E0A009";
        internal const string BridgeHash = "FFDD68F068729102C6032B81B8732BF7AED5F9B4399CE4BE71FB2A690BF96645";
        internal const string VirtualDesktopHash = "827AD85F3606A4DC4A8F5561A8CA69E4C6C1B5D2B9CD3315A461B9270B08242C";
        private static readonly string selectedRuntime = RuntimeName(Environment.GetEnvironmentVariable("SIGNALIS_VR_RUNTIME"));
        internal static bool VirtualDesktopMode { get { return selectedRuntime == "VirtualDesktop"; } }
        internal static float ConfiguredDisplayHz(string value) {
            if(String.IsNullOrWhiteSpace(value))return 0;
            float hz;
            if(!float.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out hz)||float.IsNaN(hz)||float.IsInfinity(hz)||hz<60||hz>144)throw new InvalidOperationException("Invalid SIGNALIS_VR_REFRESH_HZ. Use the launcher -RefreshRate option.");
            return hz;
        }
        internal static string RuntimeName(string value) {
            if(String.IsNullOrWhiteSpace(value)||String.Equals(value,"SteamVR",StringComparison.OrdinalIgnoreCase))return "SteamVR";
            if(String.Equals(value,"VirtualDesktop",StringComparison.OrdinalIgnoreCase))return "VirtualDesktop";
            throw new InvalidOperationException("Unknown SIGNALIS_VR_RUNTIME. Use the SteamVR or VirtualDesktop launcher.");
        }
        internal static string RuntimeFile(string directory,string mode) {
            return RuntimeName(mode)=="VirtualDesktop" ? Path.Combine(directory,"VirtualDesktop","openvr_api.dll") : Path.Combine(directory,"openvr_api.dll");
        }

        internal static string DirectoryForExecutable(string executable)
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executable)), "UserLibs", "SignalisVrTracking");
        }
        internal static void ValidateFile(string path, string expectedHash)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("VR dependency missing: " + path + ". Restore the matching VR runtime libraries before starting the game.");
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                if (!String.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("VR dependency mismatch: " + path + ". Expected SHA256 " + expectedHash + ", found " + actual + ". Use the matching packaged libraries and restart SIGNALIS.");
            }
        }
        internal static void Initialize()
        {
            lock (gate)
            {
                if (runtime != IntPtr.Zero && bridge != IntPtr.Zero) return;
                if (IntPtr.Size != 8) throw new PlatformNotSupportedException("SIGNALIS VR requires the 64-bit game process.");
                var buffer = new StringBuilder(32768);
                uint length = GetModuleFileNameW(IntPtr.Zero, buffer, buffer.Capacity);
                if (length == 0 || length >= buffer.Capacity) throw new InvalidOperationException("Cannot locate the running game executable.");
                LibraryDirectory = DirectoryForExecutable(buffer.ToString());
                string runtimePath = RuntimeFile(LibraryDirectory, selectedRuntime);
                string bridgePath = Path.Combine(LibraryDirectory, "SignalisVrRenderBridge.dll");
                // Check the entire pair before loading either file. No SteamVR connection here.
                if(VirtualDesktopMode) {
                    var manifest=Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
                    if(String.IsNullOrEmpty(manifest)||!Path.IsPathRooted(manifest)||!File.Exists(manifest))throw new InvalidOperationException("Virtual Desktop mode requires the VDXR manifest. Start with Launch VirtualDesktop.cmd.");
                }
                ValidateFile(runtimePath, VirtualDesktopMode ? VirtualDesktopHash : RuntimeHash);
                ValidateFile(bridgePath, BridgeHash);
                if (runtime == IntPtr.Zero) runtime = Load(runtimePath);
                if (bridge == IntPtr.Zero) bridge = Load(bridgePath);
                MelonLoader.MelonLogger.Msg("VR RUNTIME: " + selectedRuntime + "; library=" + runtimePath + (VirtualDesktopMode ? "; VDXR/OpenComposite timing estimates excluded from GPU diagnostics." : "; SteamVR/OpenVR."));
            }
        }
        private static IntPtr Load(string path)
        {
            // Absolute target, dependencies from its directory and Windows default directories.
            IntPtr module = LoadLibraryExW(path, IntPtr.Zero, 0x100 | 0x1000);
            if (module == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("Cannot load VR dependency " + path + ": Windows " + error + " (" + new Win32Exception(error).Message + "). Check the matching 64-bit package and required Visual C++ runtime.");
            }
            return module;
        }
        internal static T Bind<T>(bool isBridge, string entry) where T : class
        {
            Initialize();
            IntPtr pointer = GetProcAddress(isBridge ? bridge : runtime, entry);
            if (pointer == IntPtr.Zero) throw new EntryPointNotFoundException("VR dependency lacks required export " + entry + ". Restore the matching package and restart.");
            return (T)(object)Marshal.GetDelegateForFunctionPointer(pointer, typeof(T));
        }
    }
}

