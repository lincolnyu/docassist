using QLogger.FileSystemHelpers;
using System.IO;

namespace DocAssistShared.Merging
{
    public class LogicFilter
    {
        /// <summary>
        ///  The result of TT, TF, FT, FF 
        /// </summary>
        public enum Operators
        {
            FFFF = 0, 
            FFFT,
            FFTF,
            FFTT,
            FTFF,
            FTFT,
            FTTF,
            FTTT,
            TFFF,
            TFFT,
            TFTF,
            TFTT,
            TTFF,
            TTFT,
            TTTF,
            TTTT
        }

        public enum CommonOperators
        {
            And = Operators.TFFF,
            Or = Operators.TTTF,
            Xor = Operators.FTTF
        }

        /// <summary>
        ///  Possible presence levels of each side
        /// </summary>
        /// <remarks>
        ///  Possible scenarios A vs B
        ///   A and B are both the same file                       T         T
        ///   A is a file, B is directory
        ///     B immediately holds A                              T       &lt;=IP 
        ///     B is not an immediate directory                    T       &lt;=P
        ///   A is a directory, B is a file
        ///     A immediately holds B                            &lt;=IP     T
        ///     A is not an immediate directory                  &lt;=P      T
        ///   A and B are both directory
        ///     B immediately holds A                            !file     &lt;=IP   
        ///     B is an unimmediate parent directory of A        !file     &lt;=P
        ///     A immediately holds B                            &lt;=IP   !file
        ///     A is an unimmediate parent directory of A        &lt;=P    !file
        /// </remarks>
        public enum PresenceLevels
        {
            ParentOrDir,
            ParentOrFile,
            ImmediateParentOrDir,
            ImmediateParentOrFile
        }

        public delegate bool FileValidDelegate(FileUnit fu);
        public delegate void OutputDelegate(FileUnit left, FileUnit right);

        public LogicFilter(Operators op, PresenceLevels leftLevel, PresenceLevels rightLevel, OutputDelegate output, FileValidDelegate fileValid)
        {
            Operator = op;
            LeftLevel = leftLevel;
            RightLevel = rightLevel;
            Output = output;
            FileValid = fileValid;
        }

        public LogicFilter(Operators op, PresenceLevels leftLevel, PresenceLevels rightLevel, OutputDelegate output) : this(op, leftLevel, rightLevel, output, fu => fu != null && File.Exists(fu.OriginalPath))
        {
        }

        public LogicFilter(CommonOperators op, PresenceLevels leftLevel, PresenceLevels rightLevel, OutputDelegate output, FileValidDelegate fileValid)
            : this((Operators)op, leftLevel, rightLevel, output, fileValid)
        {
        }

        public LogicFilter(CommonOperators op, PresenceLevels leftLevel, PresenceLevels rightLevel, OutputDelegate output)
           : this((Operators)op, leftLevel, rightLevel, output)
        {
        }

        public Operators Operator { get; }
        public PresenceLevels LeftLevel { get; }
        public PresenceLevels RightLevel { get; }

        public OutputDelegate Output { get; }

        public FileValidDelegate FileValid { get; }

        private static bool LevelIsFile(PresenceLevels level) => level == PresenceLevels.ImmediateParentOrFile || level == PresenceLevels.ParentOrFile;

        private static bool LevelIsImmediate(PresenceLevels level) => level == PresenceLevels.ImmediateParentOrFile
            || level == PresenceLevels.ImmediateParentOrDir;

        public void Process(FileUnit lhs, FileUnit rhs)
        {
            System.Diagnostics.Debug.Assert(lhs != null || rhs != null);
            var lisfile = FileValid(lhs);
            var risfile = FileValid(rhs);
            bool l, r;
            if (lisfile && risfile)
            {
                System.Diagnostics.Debug.Assert(rhs.VirtualPath == lhs.VirtualPath);
                l = r = true;
            }
            else
            {
                if (lhs != null && (rhs == null || lhs.VirtualPath.StartsWith(rhs.VirtualPath)))
                {
                    if (rhs == null)
                    {
                        r = false;
                    }
                    else if (lhs.VirtualPath.GetParentDirectory().TrimEnd(Path.DirectorySeparatorChar) == rhs.VirtualPath)
                    {
                        r = true;
                    }
                    else
                    {
                        r = !LevelIsImmediate(RightLevel);
                    }
                    if (LevelIsFile(LeftLevel) && !lisfile) return;
                    l = true;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(rhs != null && (lhs == null || rhs.VirtualPath.StartsWith(lhs.VirtualPath)));
                    if (lhs == null)
                    {
                        l = false;
                    }
                    else if (rhs.VirtualPath.GetParentDirectory().TrimEnd(Path.DirectorySeparatorChar) == lhs.VirtualPath)
                    {
                        l = true;
                    }
                    else
                    {
                        l = !LevelIsImmediate(LeftLevel);
                    }
                    if (LevelIsFile(RightLevel) && !risfile) return;
                    r = true;
                }
            }
            var output = Calculate(Operator, l, r);
            if (output)
            {
                Output(lhs, rhs);
            }
        }

        private static bool Calculate(Operators op, bool left, bool right)
        {
            var shift = left ? 1 : 0;
            if (right) shift |= 2;
            var mask = 1 << shift;
            return ((int)op & mask) != 0;
        }
    }
}
