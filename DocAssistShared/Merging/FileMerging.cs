using System;
using System.Collections.Generic;

namespace DocAssistShared.Merging
{
    public static class FileMerging
    {
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
        /// <return>The list of merged pairs</return>
        public static IEnumerable<Tuple<FileUnit, FileUnit>> Merge(IEnumerable<FileUnit> lhs, IEnumerable<FileUnit> rhs)
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
                        yield return new Tuple<FileUnit, FileUnit>(l, sub);
                        lvalid = lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Containing:
                        if (rsubs.Count > 0)
                        {
                            sub = rsubs.Peek();
                            System.Diagnostics.Debug.Assert(r.VirtualPath.Contains(sub.VirtualPath));
                            yield return new Tuple<FileUnit, FileUnit>(sub, r);
                        }
                        lsubs.Push(r);
                        rvalid = renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Equal:
                        yield return new Tuple<FileUnit, FileUnit>(l, r);
                        lvalid = lenum.MoveNext();
                        rvalid = renum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Contained:
                        if (lsubs.Count > 0)
                        {
                            sub = lsubs.Peek();
                            System.Diagnostics.Debug.Assert(l.VirtualPath.Contains(sub.VirtualPath));
                            yield return new Tuple<FileUnit, FileUnit>(l, sub);
                        }
                        rsubs.Push(l);
                        lvalid = lenum.MoveNext();
                        break;
                    case FileUnit.CompareResults.Greater:
                        ClearTo(lsubs, l);
                        sub = FindSub(rsubs, s => Compare(s, r) == FileUnit.CompareResults.Contained);
                        yield return new Tuple<FileUnit, FileUnit>(sub, r);
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
