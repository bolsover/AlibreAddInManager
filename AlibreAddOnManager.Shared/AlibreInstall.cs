namespace AlibreAddOnManager
{
    public class AlibreInstall
    {
        public string Folder { get; set; }
        public string ExePath { get; set; }
        public string Version { get; set; }
        public int Build { get; set; }

        public override string ToString() => $"Alibre Design {Version}";
    }
}
