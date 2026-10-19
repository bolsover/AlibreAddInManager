using AlibreAddOnManager.Models;

namespace AlibreAddOnManager
{
    public class InstalledAddOn
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public AddOnStatus Status { get; set; }
        public string Folder { get; set; }
        public AdcFile Adc { get; set; }
        public string Problem { get; set; }
        public TrackedAddOn Tracked { get; set; }

        public string DllTypeText => Adc == null ? "" : AdcFile.DllTypeName(Adc.DllType);

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case AddOnStatus.Self: return "This add-on";
                    case AddOnStatus.Disabled: return Tracked != null ? "Disabled (tracked)" : "Disabled";
                    default: return Status.ToString();
                }
            }
        }
    }
}
