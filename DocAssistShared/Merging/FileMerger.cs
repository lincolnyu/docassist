using System.Collections.Generic;

namespace DocAssistShared.Merging
{
    public static class FileMerger
    {
        public class FileUnit
        {
            public enum CompareResults
            {
                Greater = -2,
                Contained,
                Equal,
                Containing,
                Less
            }

            public FileUnit(string path, string virtualPath)
            {
                Path = path;
                VirtualPath = virtualPath;
            }

            public FileUnit(string path)
            {
                Path = path;
                VirtualPath = path;
            }

            public string Path { get; }
            public string VirtualPath { get; }

            public virtual CompareResults CompareTo(FileUnit other)
            {
                if (VirtualPath == other.VirtualPath) return CompareResults.Equal;
                if (VirtualPath.StartsWith(other.VirtualPath)) return CompareResults.Containing;
                if (other.VirtualPath.StartsWith(VirtualPath)) return CompareResults.Contained;
                return VirtualPath.CompareTo(other.VirtualPath) > 0 ? CompareResults.Greater : CompareResults.Less;
            }
        }

        public delegate void Process(FileUnit lhs, FileUnit rhs);

        private static FileUnit.CompareResults Compare(FileUnit lhs, FileUnit rhs)
        {
            System.Diagnostics.Debug.Assert(lhs != null || rhs != null);
            if (lhs == null) return FileUnit.CompareResults.Greater;
            if (rhs == null) return FileUnit.CompareResults.Less;
            return lhs.CompareTo(rhs);
        }

        public static void Merge(IEnumerable<FileUnit> lhs, IEnumerable<FileUnit> rhs, Process process)
        {
            var lenum = lhs.GetEnumerator();
            var renum = rhs.GetEnumerator();
            var lvalid = lenum.MoveNext();
            var rvalid = renum.MoveNext();
            while (true)
            {
                var l = lvalid ? lenum.Current : null;
                var r = rvalid ? renum.Current : null;
                if (l == null && r == null) break;
                var c = Compare(l, r);
                switch (c)
                {
                    case FileUnit.CompareResults.Less:
                        process(l, null);
                        lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Containing:
                        process(l, r);
                        lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Equal:
                        process(l, r);
                        lvalid = lenum.MoveNext();
                        rvalid = renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Contained:
                        process(l, r);
                        renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Greater:
                        process(null, r);
                        renum.MoveNext();
                        break;
                }
            }
        }
    }
}
