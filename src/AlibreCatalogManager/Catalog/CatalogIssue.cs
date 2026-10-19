namespace AlibreCatalogManager.Catalog
{
    public sealed class CatalogIssue
    {
        public CatalogIssueSeverity Severity { get; set; }

        /// <summary>The add-on concerned, or null for catalog-wide issues.</summary>
        public CatalogItem Item { get; set; }

        public string Message { get; set; }

        public static CatalogIssue Error(CatalogItem item, string message) =>
            new CatalogIssue { Severity = CatalogIssueSeverity.Error, Item = item, Message = message };

        public static CatalogIssue Warning(CatalogItem item, string message) =>
            new CatalogIssue { Severity = CatalogIssueSeverity.Warning, Item = item, Message = message };

        public static CatalogIssue Info(CatalogItem item, string message) =>
            new CatalogIssue { Severity = CatalogIssueSeverity.Info, Item = item, Message = message };
    }
}
