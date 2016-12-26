using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RenameScript
{
    class Program
    {
        public static IList<string> RenameFiles(IEnumerable<FileInfo> files)
        {
            var fileList = files.ToArray();
            // after population the location of each of its item indicates
            // the place of the item in file list with an index the same as the item
            // in the list sorted by last writting time
            var inversePermute = new List<int>();
            foreach (var file in fileList)
            {
                var createdAt = file.LastWriteTimeUtc;

                var i = 0;
                for (; i < inversePermute.Count; i++)
                {
                    var index = inversePermute[i];
                    var timeAtIndex = fileList[index].LastWriteTimeUtc;
                    if (createdAt.CompareTo(timeAtIndex) < 0)
                    {
                        break;
                    }
                }
                inversePermute.Insert(i, inversePermute.Count);
            }

            var numFiles = inversePermute.Count;
            const string ext = ".dat";  // TODO change extension or uncomment the following code to get extent from the extent of the first file
            //var ext = fileList[0].Extension;
            var minNumDigits = (int)Math.Floor(Math.Log10(numFiles)) + 1;   // it starts from 1
            var patternSb = new StringBuilder("{0:");
            patternSb.Append('0', minNumDigits);
            patternSb.Append("}");
            patternSb.Append(ext);
            var pattern = patternSb.ToString();
            var newNames = new List<string>();

            // it is the inverse of the list above populated and each item represents 
            // the place of the file at the same location in the original list
            // in the list sorted by last writing time
            var permute = new int[numFiles];
            for (var i = 0; i < numFiles; i++)
            {
                permute[inversePermute[i]] = i;
            }
            foreach (var i in permute)
            {
                var name = string.Format(pattern, i+1); // 1-based
                newNames.Add(name);
            }

            return newNames;
        }
    }
}
