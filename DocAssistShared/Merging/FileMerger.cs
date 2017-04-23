using System;
using System.Collections.Generic;
using System.IO;

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

            public FileUnit() { }

            /// <summary>
            ///  Constructs a file unit
            /// </summary>
            /// <param name="originalPath">The actual absolute path where the file is located</param>
            /// <param name="virtualPath">
            ///  The virtual path (should always be a 'relative kind of path') that is used to compare with other file unit
            /// </param>
            public FileUnit(string originalPath, string virtualPath)
            {
                OriginalPath = originalPath;
                VirtualPath = virtualPath;
            }

            public string OriginalPath { get; }
            public string VirtualPath { get; }

            public virtual CompareResults CompareTo(FileUnit other)
            {
                if (VirtualPath == other.VirtualPath) return CompareResults.Equal;
                if (VirtualPath.StartsWith(other.VirtualPath)) return CompareResults.Containing;
                if (other.VirtualPath.StartsWith(VirtualPath)) return CompareResults.Contained;
                return VirtualPath.CompareTo(other.VirtualPath) > 0 ? CompareResults.Greater : CompareResults.Less;
            }

            public static FileUnit Create(string originalPath, string basePath)
            {
                System.Diagnostics.Debug.Assert(originalPath.StartsWith(basePath));
                var virtualPath = originalPath.Substring(basePath.Length);
                virtualPath = virtualPath.TrimStart(Path.DirectorySeparatorChar);
                return new FileUnit(originalPath, virtualPath);
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

        /// <summary>
        ///  Merges <paramref name="lhs"/> and <paramref name="rhs"/>, assuming they are alphabetically ordered by VirtualPath
        /// </summary>
        /// <param name="lhs">The list of units on the left</param>
        /// <param name="rhs">The list of units on the right</param>
        /// <param name="process">The method that processes the units that should be paired and output</param>
        public static void Merge(IEnumerable<FileUnit> lhs, IEnumerable<FileUnit> rhs, Process process)
        {
            var lenum = lhs.GetEnumerator();
            var renum = rhs.GetEnumerator();
            var lvalid = lenum.MoveNext();
            var rvalid = renum.MoveNext();
            var lsubs = new Stack<FileUnit>();
            var rsubs = new Stack<FileUnit>();
            while (true)
            {
                var l = lvalid ? lenum.Current : null;
                var r = rvalid ? renum.Current : null;
                if (l == null && r == null) break;
                var c = Compare(l, r);
                switch (c)
                {
                    case FileUnit.CompareResults.Less:
                        ClearTo(rsubs, r);
                        var sub = FindSub(lsubs, s => Compare(l, s) == FileUnit.CompareResults.Containing);
                        process(l, sub);
                        lvalid = lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Containing:
                        if (rsubs.Count > 0)
                        {
                            sub = rsubs.Peek();
                            System.Diagnostics.Debug.Assert(r.VirtualPath.Contains(sub.VirtualPath));
                            process(sub, r);
                        }
                        lsubs.Push(r);
                        rvalid = renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Equal:
                        process(l, r);
                        lvalid = lenum.MoveNext();
                        rvalid = renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Contained:
                        if (lsubs.Count > 0)
                        {
                            sub = lsubs.Peek();
                            System.Diagnostics.Debug.Assert(l.VirtualPath.Contains(sub.VirtualPath));
                            process(l, sub);
                        }
                        rsubs.Push(l);
                        lvalid = lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Greater:
                        ClearTo(lsubs, l);
                        sub = FindSub(rsubs, s => Compare(s, r) == FileUnit.CompareResults.Contained);
                        process(sub, r);
                        rvalid = renum.MoveNext();
                        break;
                }
            }
        }

        private static FileUnit FindSub(Stack<FileUnit> subs, Predicate<FileUnit> isSub)
        {
            while (subs.Count > 0)
            {
                var sub = subs.Pop();
                if (isSub(sub))
                {
                    subs.Push(sub);
                    return sub;
                }
            }
            return null;
        }

        private static void ClearTo(Stack<FileUnit> subs, FileUnit fu)
        {
            while (subs.Count > 0)
            {
                var sub = subs.Pop();
                if (fu.VirtualPath.StartsWith(sub.VirtualPath))
                {
                    subs.Push(sub);
                    break;
                }
            }
        }
    }
}
