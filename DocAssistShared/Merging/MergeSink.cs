using System;
using System.IO;

namespace DocAssistShared.Merging
{
    public class MergeSink
    {
        /// <summary>
        ///  Modes in which file conflicts are handled
        /// </summary>
        public enum ConflictHandlingModes
        {
            AlwaysTakeLeft,
            AlwaysTakeRight,
            AlwaysTakeNewer,
            AlwaysTakeLarger,
            AlwaysKeepBoth,
            Prompt,
        }

        public enum DirectorySelectionModes
        {
            Neither,
            LowDirectory,
            HighDirectory,
        }

        public enum FileDirSelectionModes
        {
            Neither,
            File,
            Directory
        }

        public delegate Tuple<string, string> PromptOnConflictDelegate(FileUnit left, FileUnit right, string target);

        public MergeSink(ConflictHandlingModes conflictHandlingMode, DirectorySelectionModes directorySelectionMode, FileDirSelectionModes fileDirSelectionMode, string targetDir)
        {
            System.Diagnostics.Debug.Assert(conflictHandlingMode != ConflictHandlingModes.Prompt);
            ConflictHandlingMode = conflictHandlingMode;
            DirectorySelectionMode = directorySelectionMode;
            FileDirSelectionMode = fileDirSelectionMode;
            TargetBase = targetDir;
        }

        public MergeSink(PromptOnConflictDelegate prompt, DirectorySelectionModes directorySelectionMode, FileDirSelectionModes fileDirSelectionMode, string targetDir)
        {
            PromptOnConflict = prompt;
            ConflictHandlingMode = ConflictHandlingModes.Prompt;
            DirectorySelectionMode = directorySelectionMode;
            FileDirSelectionMode = fileDirSelectionMode;
            TargetBase = targetDir;
        }

        public ConflictHandlingModes ConflictHandlingMode { get; }
        public PromptOnConflictDelegate PromptOnConflict { get; }
        public DirectorySelectionModes DirectorySelectionMode { get; }
        public FileDirSelectionModes FileDirSelectionMode { get; }
        public string TargetBase { get; }

        public void Output(FileUnit left, FileUnit right)
        {
            var lisfile = File.Exists(left.OriginalPath);
            var risfile = File.Exists(right.OriginalPath);
            if (lisfile && risfile)
            {
                System.Diagnostics.Debug.Assert(left.VirtualPath == right.VirtualPath);
                switch (ConflictHandlingMode)
                {
                    case ConflictHandlingModes.AlwaysTakeLeft:
                        Copy(left);
                        break;
                    case ConflictHandlingModes.AlwaysTakeRight:
                        Copy(right);
                        break;
                    case ConflictHandlingModes.AlwaysTakeLarger:
                        var llen = File.OpenRead(left.OriginalPath).Length;
                        var rlen = File.OpenRead(right.OriginalPath).Length;
                        if (llen < rlen)
                        {
                            Copy(right);
                        }
                        else
                        {
                            Copy(left);
                        }
                        break;
                    case ConflictHandlingModes.AlwaysTakeNewer:
                        var ltime = File.GetLastWriteTimeUtc(left.OriginalPath);
                        var rtime = File.GetLastWriteTimeUtc(right.OriginalPath);
                        if (ltime < rtime)
                        {
                            Copy(right);
                        }
                        else
                        {
                            Copy(left);
                        }
                        break;
                    case ConflictHandlingModes.AlwaysKeepBoth:
                        CopyBoth(left, right);
                        break;
                    case ConflictHandlingModes.Prompt:
                        var lr = PromptOnConflict(left, right, TargetBase);
                        if (lr.Item1 != null)
                        {
                            CopyTo(left, lr.Item1);
                        }
                        if (lr.Item2 != null)
                        {
                            CopyTo(right, lr.Item2);
                        }
                        break;
                }
            }
            else if (lisfile)
            {
                System.Diagnostics.Debug.Assert(left.VirtualPath.Contains(right.VirtualPath));
                switch (FileDirSelectionMode)
                {
                    case FileDirSelectionModes.Directory:
                        CreateDir(right);
                        break;
                    case FileDirSelectionModes.File:
                        Copy(left);
                        break;
                }
            }
            else if (risfile)
            {
                System.Diagnostics.Debug.Assert(right.VirtualPath.Contains(left.VirtualPath));
                switch (FileDirSelectionMode)
                {
                    case FileDirSelectionModes.Directory:
                        CreateDir(left);
                        break;
                    case FileDirSelectionModes.File:
                        Copy(right);
                        break;
                }
            }
            else
            {
                var lcontr = left.VirtualPath.Contains(right.VirtualPath);
                var rcontl = right.VirtualPath.Contains(left.VirtualPath);
                System.Diagnostics.Debug.Assert(lcontr || rcontl);
                switch (DirectorySelectionMode)
                {
                    case DirectorySelectionModes.HighDirectory:
                        if (lcontr) CreateDir(left);
                        else CreateDir(right);
                        break;
                    case DirectorySelectionModes.LowDirectory:
                        if (lcontr) CreateDir(right);
                        else CreateDir(left);
                        break;
                }
            }
        }

        private void CreateDir(FileUnit fu)
        {
            var target = Path.Combine(TargetBase, fu.VirtualPath);
            Directory.CreateDirectory(target);
        }

        private void Copy(FileUnit fu)
        {
            var target = Path.Combine(TargetBase, fu.VirtualPath);
            CopyTo(fu, target);
        }

        private static void CopyTo(FileUnit fu, string target)
        {
            if (fu.OriginalPath != target)
            {
                File.Copy(fu.OriginalPath, target, true);
            }
        }

        private void CopyBoth(FileUnit left, FileUnit right)
        {
            var ltarget = Path.Combine(TargetBase, left.VirtualPath, "#l");
            var rtarget = Path.Combine(TargetBase, right.VirtualPath, "#r");
            CopyTo(left, ltarget);
            CopyTo(right, rtarget);
        }
    }
}
