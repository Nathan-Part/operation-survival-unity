using System.Collections.Generic;

namespace Game.Data.Editor.Import
{
    public class ImportReport
    {
        public readonly List<string> Created = new();
        public readonly List<string> Updated = new();
        public readonly List<string> Unchanged = new();
        public readonly List<string> Orphaned = new();
        public readonly List<string> Warnings = new();

        public void Merge(ImportReport other)
        {
            Created.AddRange(other.Created);
            Updated.AddRange(other.Updated);
            Unchanged.AddRange(other.Unchanged);
            Orphaned.AddRange(other.Orphaned);
            Warnings.AddRange(other.Warnings);
        }

        public string Summary() =>
            $"Created: {Created.Count}, Updated: {Updated.Count}, Unchanged: {Unchanged.Count}, " +
            $"Orphaned: {Orphaned.Count}, Warnings: {Warnings.Count}" +
            (Warnings.Count > 0 ? "\n\nWarnings:\n" + string.Join("\n", Warnings) : "") +
            (Orphaned.Count > 0 ? "\n\nOrphaned (not deleted, review manually):\n" + string.Join("\n", Orphaned) : "");
    }
}
