using System.Collections.Generic;

namespace AlibreCatalogManager.Generation
{
    public sealed class GenerationResult
    {
        public string ProjectFolder { get; set; }

        public string ProjectFile { get; set; }

        /// <summary>Files written, relative to <see cref="ProjectFolder"/>.</summary>
        public List<string> Files { get; } = new List<string>();

        /// <summary>The solution the project was added to, or null.</summary>
        public string SolutionUpdated { get; set; }
    }
}
