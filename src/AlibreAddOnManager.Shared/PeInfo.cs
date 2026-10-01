using System.Collections.Generic;

namespace AlibreAddOnManager
{
    public class PeInfo
    {
        public const ushort MachineI386 = 0x014C;
        public const ushort MachineAmd64 = 0x8664;

        private const uint ComImageFlagsIlOnly = 0x00000001;
        private const uint ComImageFlags32BitRequired = 0x00000002;

        public ushort Machine { get; set; }
        public bool IsPe32Plus { get; set; }
        public bool HasClrHeader { get; set; }
        public uint CorFlags { get; set; }
        public List<string> ExportNames { get; } = new List<string>();

        public bool IlOnly => (CorFlags & ComImageFlagsIlOnly) != 0;
        public bool Requires32Bit => (CorFlags & ComImageFlags32BitRequired) != 0;

        public string MachineName
        {
            get
            {
                switch (Machine)
                {
                    case MachineAmd64: return "x64";
                    case MachineI386: return HasClrHeader && IlOnly && !Requires32Bit ? "AnyCPU" : "x86";
                    case 0xAA64: return "ARM64";
                    default: return "0x" + Machine.ToString("X4");
                }
            }
        }

        /// <summary>
        /// Can 64-bit Alibre load this DLL as the given flavour?
        /// Native: x64 with no CLR header. Managed: CLR header and either x64, or
        /// an IL-only AnyCPU image (reported as machine 0x14C) without 32BITREQUIRED.
        /// </summary>
        public bool IsLoadableBy64BitHost(AddOnDllType type, out string reason)
        {
            reason = null;
            if (type == AddOnDllType.Native)
            {
                if (HasClrHeader) { reason = "the .adc declares a native DLL but it is a .NET assembly"; return false; }
                if (Machine != MachineAmd64) { reason = $"native DLL is {MachineName}; Alibre is 64-bit and needs x64"; return false; }
                return true;
            }

            if (type == AddOnDllType.Managed)
            {
                if (!HasClrHeader) { reason = "the .adc declares a managed DLL but it has no .NET (CLR) header"; return false; }
                if (Machine == MachineAmd64) return true;
                if (Machine == MachineI386 && IlOnly && !Requires32Bit) return true;
                reason = $"managed DLL is {MachineName}; it must be AnyCPU or x64";
                return false;
            }

            reason = "unrecognised DLL type in the .adc";
            return false;
        }
    }
}
