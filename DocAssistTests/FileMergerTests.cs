using DocAssistShared.Merging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DocAssistShared.Merging.FileMerger;

namespace DocAssistTests
{
    [TestClass]
    public class FileMergerTests
    {
        string ABase = @"a:\some\base\dir";
        string BBase = @"b:\b\base";

        string[] ARelPaths =
        {
            @"books\literature\dickens\ataleoftwocities.pdf",
            @"books\literature\dickens\olivertwist.pdf",
            @"books\literature\hugo",
            @"books\literature\shakespear\hamlet.pdf",
            @"books\literature\tolstoy\anna.pdf",
            @"music\classical",
            @"music\classical\bach\concerti\brandenburg.mp3",
            @"music\classical\beethoven\sonatas\moonlight.mp3",
            @"music\classical\beethoven\symphonies\symphony5.mp3",
            @"music\classical\beethoven\symphonies\symphony9.mp3",
            @"music\classical\mozart\symphonies\sym40.ogg",
            @"music\classical\mozart\pianoconcerti\pc20.mp3",
            @"music\classical\mozart\intro.txt",
            @"music\pop\mj\blackorwhite.mp3"
        };

        string[] BRelPaths =
        {
            @"books\literature\shakespear\othello.pdf",
            @"books\literature\tolstoy\warandpeace.pdf",
            @"books\literature\hugo\lesmis.pdf",
            @"books\literature\hugo\notredame.pdf",
            @"music\classical\bach\concerti",
            @"music\classical\bach\masses\massinbminor.mp3",
            @"music\classical\beethoven",
            @"music\classical\beethoven\symphonies",
            @"music\classical\beethoven\symphonies\symphony9.mp3",
            @"music\classical\mozart",
            @"music\classical\mozart\pianoconcerti\pc20.mp3",
            @"music\pop\mj\bad.mp3",
            @"movies\drama\titanic.mp4",
            @"movies\scifi\starwars.mp4"
        };

        Tuple<string,string>[] ExpectedResultParentOrFileOr =
        {
            new Tuple<string, string>(@"books\literature\dickens\ataleoftwocities.pdf", null),
            new Tuple<string, string>(@"books\literature\dickens\olivertwist.pdf", null),
            new Tuple<string, string>(@"books\literature\hugo", @"books\literature\hugo\lesmis.pdf"),
            new Tuple<string, string>(@"books\literature\hugo", @"books\literature\hugo\notredame.pdf"),
            new Tuple<string, string>(@"books\literature\shakespear\hamlet.pdf", null),
            new Tuple<string, string>(null, @"books\literature\shakespear\othello.pdf"),
            new Tuple<string, string>(@"books\literature\tolstoy\anna.pdf", null),
            new Tuple<string, string>(null,  @"books\literature\tolstoy\warandpeace.pdf"),
            new Tuple<string, string>(null,  @"movies\drama\titanic.mp4"),
            new Tuple<string, string>(null, @"movies\scifi\starwars.mp4"),
            new Tuple<string, string>(@"music\classical\bach\concerti\brandenburg.mp3",  @"music\classical\bach\concerti"),
            new Tuple<string, string>(@"music\classical", @"music\classical\bach\masses\massinbminor.mp3"),
            new Tuple<string, string>(@"music\classical\beethoven\sonatas\moonlight.mp3", @"music\classical\beethoven"),
            new Tuple<string, string>(@"music\classical\beethoven\symphonies\symphony5.mp3", @"music\classical\beethoven\symphonies"),
            new Tuple<string, string>(@"music\classical\beethoven\symphonies\symphony9.mp3", @"music\classical\beethoven\symphonies\symphony9.mp3"),
            new Tuple<string, string>(@"music\classical\mozart\intro.txt", @"music\classical\mozart"),
            new Tuple<string, string>(@"music\classical\mozart\pianoconcerti\pc20.mp3", @"music\classical\mozart\pianoconcerti\pc20.mp3"),
            new Tuple<string, string>(@"music\classical\mozart\symphonies\sym40.ogg", @"music\classical\mozart"),
            new Tuple<string, string>(null,  @"music\pop\mj\bad.mp3"),
            new Tuple<string, string>(@"music\pop\mj\blackorwhite.mp3", null),
        };

        Tuple<string, string>[] ExpectedResultImmediateParentOrDirAnd =
        {
            new Tuple<string, string>(@"books\literature\hugo", @"books\literature\hugo\lesmis.pdf"),
            new Tuple<string, string>(@"books\literature\hugo", @"books\literature\hugo\notredame.pdf"),
            new Tuple<string, string>(@"music\classical\bach\concerti\brandenburg.mp3",  @"music\classical\bach\concerti"),
            new Tuple<string, string>(@"music\classical", @"music\classical\beethoven"),
            new Tuple<string, string>(@"music\classical\beethoven\symphonies\symphony5.mp3", @"music\classical\beethoven\symphonies"),
            new Tuple<string, string>(@"music\classical\beethoven\symphonies\symphony9.mp3", @"music\classical\beethoven\symphonies\symphony9.mp3"),
            new Tuple<string, string>(@"music\classical", @"music\classical\mozart"),
            new Tuple<string, string>(@"music\classical\mozart\intro.txt", @"music\classical\mozart"),
            new Tuple<string, string>(@"music\classical\mozart\pianoconcerti\pc20.mp3", @"music\classical\mozart\pianoconcerti\pc20.mp3"),
        };

        private IEnumerable<FileUnit> GenerateUnits(IEnumerable<string> relPaths, string basePath)
        {
            foreach (var relPath in relPaths.OrderBy(x => x))
            {
                var originalPath = Path.Combine(basePath, relPath);
                var virtualPath = relPath;
                yield return new FileUnit(originalPath, virtualPath);
            }
        }

        [TestMethod]
        public void TestParentOrFileOr()
        {
            var aunits = GenerateUnits(ARelPaths, ABase);
            var bunits = GenerateUnits(BRelPaths, BBase);
            var actualResult = new List<Tuple<string, string>>();
            var processor = new LogicProcessor(LogicProcessor.CommonOperators.Or, LogicProcessor.PresenceLevels.ParentOrFile, LogicProcessor.PresenceLevels.ParentOrFile, (l, r) =>
                actualResult.Add(new Tuple<string, string>(l?.VirtualPath, r?.VirtualPath)), fu=>fu != null && Path.HasExtension(fu.OriginalPath));
            Merge(aunits, bunits, processor.Process);
            AssertTuplesAreEqual(ExpectedResultParentOrFileOr, actualResult);
        }

        [TestMethod]
        public void TestImmediateParentOrDirAnd()
        {
            var aunits = GenerateUnits(ARelPaths, ABase);
            var bunits = GenerateUnits(BRelPaths, BBase);
            var actualResult = new List<Tuple<string, string>>();
            var processor = new LogicProcessor(LogicProcessor.CommonOperators.And, LogicProcessor.PresenceLevels.ImmediateParentOrDir, LogicProcessor.PresenceLevels.ImmediateParentOrDir, (l, r) =>
                actualResult.Add(new Tuple<string, string>(l?.VirtualPath, r?.VirtualPath)), fu => fu != null && Path.HasExtension(fu.OriginalPath));
            Merge(aunits, bunits, processor.Process);
            AssertTuplesAreEqual(ExpectedResultImmediateParentOrDirAnd, actualResult);
        }

        void AssertTuplesAreEqual(IList<Tuple<string, string>> expected, IList<Tuple<string, string>> actual)
        {
            Assert.IsTrue(expected.Count == actual.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                var e = expected[i];
                var a = actual[i];
                Assert.IsTrue(e.Item1 == a.Item1);
                Assert.IsTrue(e.Item2 == a.Item2);
            }
        }
    }
}
