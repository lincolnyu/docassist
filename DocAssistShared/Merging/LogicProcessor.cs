using QLogger.FileSystemHelpers;
using System.IO;
using static DocAssistShared.Merging.FileMerger;

namespace DocAssistShared.Merging
{
    public class LogicProcessor
    {
        /// <summary>
        ///  The result of FF, FT, TF, TT
        /// </summary>
        public enum Operators
        {
            FFFF, 
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
            And = Operators.FFFT,
            Or = Operators.FTTT,
            Xor = Operators.FTTF
        }

        /// <summary>
        ///  
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
        ///     B immediately holds A                            &lt;IF    &lt;=IP   
        ///     B is an unimmediate parent directory of A        &lt;IF    &lt;=P
        ///     A immediately holds B                            &lt;=IP   &lt;IF
        ///     A is an unimmediate parent directory of A        &lt;=P    &lt;IF
        /// </remarks>
        public enum PresenceLevels
        {
            Parent,
            ImmediateParent,
            Directory,
            File,
        }

        public LogicProcessor(Operators op, PresenceLevels leftLevel, PresenceLevels rightLevel, string sourceBase, string targetBase)
        {
            Operator = op;
            LeftLevel = leftLevel;
            RightLevel = rightLevel;
            SourceBase = sourceBase;
            TargetBase = targetBase;
        }

        public LogicProcessor(CommonOperators op, PresenceLevels leftLevel, PresenceLevels rightLevel, string sourceBase, string targetBase)
            : this((Operators)op, leftLevel, rightLevel,sourceBase, targetBase)
        {
        }

        public Operators Operator { get; }
        public PresenceLevels LeftLevel { get; }
        public PresenceLevels RightLevel { get; }

        public string SourceBase { get; }
        public string TargetBase { get; }

        public void Process(FileUnit lhs, FileUnit rhs)
        {
            var lisfile = File.Exists(lhs.Path);
            var risfile = File.Exists(rhs.Path);
            bool l, r;
            if (lisfile && risfile)
            {
                System.Diagnostics.Debug.Assert(rhs.VirtualPath == lhs.VirtualPath);
                l = r = true;
            }
            else
            {
                if (lhs.VirtualPath.StartsWith(rhs.VirtualPath))
                {
                    if (lhs.VirtualPath.GetParentDirectory() == rhs.VirtualPath)
                    {
                        r = RightLevel <= PresenceLevels.ImmediateParent;
                    }
                    else
                    {
                        r = RightLevel == PresenceLevels.Parent;
                    }
                    l = LeftLevel < PresenceLevels.File || lisfile;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(rhs.VirtualPath.StartsWith(lhs.VirtualPath));
                    if (rhs.VirtualPath.GetParentDirectory() == lhs.VirtualPath)
                    {
                        l = LeftLevel <= PresenceLevels.ImmediateParent;
                    }
                    else
                    {
                        l = LeftLevel == PresenceLevels.Parent;
                    }
                    r = RightLevel < PresenceLevels.File || risfile;
                }
            }
        }
    }
}
