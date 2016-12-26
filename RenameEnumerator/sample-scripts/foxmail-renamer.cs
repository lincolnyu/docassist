using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RenameScript
{
    class Program
    {
        static int CompareDate(int y1, int m1, int d1, int y2, int m2, int d2)
        {
            var c = y1.CompareTo(y2);
            if (c != 0) return c;
            c = m1.CompareTo(m2);
            return c != 0 ? c : d1.CompareTo(d2);
        }

        public static IList<string> Rename(IEnumerable<FileSystemInfo> files)
		{
			var newNames = new List<string>();
			foreach (var f in files)
			{
				if (!(f is FileInfo))
				{
					newNames.Add(f.FullName);	// ignore non-files
					continue;
				}
				var file = (FileInfo)f;
				if (file.Length == 0)
				{
					newNames.Add("");	// remove empty files
					continue;
				}
                int minDateYear = 0, minDateMonth = 0, minDateDay = 0;
                int maxDateYear = 0, maxDateMonth = 0, maxDateDay = 0;
			    var firstTime = true;
				using (var sr = new StreamReader(file.OpenRead()))
				{
					while (!sr.EndOfStream)
					{
					    var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
					    if (line.Length < "Date:".Length || line.Substring(0, "Date:".Length) != "Date:") continue;
					    var segs = line.Split(' ');
					    if (segs.Length < 5) continue;
					    var sDay = segs[2];
					    var sMonth = segs[3];
					    var sYear = segs[4];
						int day, year;
						if (!int.TryParse(sDay, out day)) continue;
						if (!int.TryParse(sYear, out year)) continue;
					    var month = 0;
					    switch (sMonth.ToLower())
					    {
                            case "jan":
					            month = 1;
					            break;
                            case "feb":
					            month = 2;
					            break;
                            case "mar":
					            month = 3;
					            break;
                            case "apr":
					            month = 4;
					            break;
                            case "may":
					            month = 5;
					            break;
                            case "jun":
					            month = 6;
					            break;
                            case "jul":
					            month = 7;
					            break;
                            case "aug":
					            month = 8;
					            break;
                            case "sep":
					            month = 9;
					            break;
                            case "oct":
					            month = 10;
					            break;
                            case "nov":
					            month = 11;
					            break;
                            case "dec":
					            month = 12;
					            break;
					    }
                        if (firstTime || CompareDate(year, month, day, minDateYear, minDateMonth, minDateDay) < 0)
                        {
                            minDateDay = day;
                            minDateMonth = month;
                            minDateYear = year;
                        }
					    if (firstTime || CompareDate(year, month, day, maxDateYear, maxDateMonth, maxDateDay) > 0)
					    {
					        maxDateDay = day;
					        maxDateMonth = month;
					        maxDateYear = year;
					    }
					    firstTime = false;
					}
				}

			    var datePrefix = string.Format("{0:0000}{1:00}{2:00}-{3:0000}{4:00}{5:00}", minDateYear, minDateMonth, minDateDay,
			                                   maxDateYear, maxDateMonth, maxDateDay);
			    var newName = new StringBuilder();
				/*
			    for (var dir = file.Directory; dir != null && dir.Name.ToLower() != "foxbox"; dir = dir.Parent)
			    {
			        newName.Insert(0, dir.Name);
					newName.Insert(0, '-');
			    } */
			    newName.Insert(0, datePrefix);
				newName.Append(".txt");
                newNames.Add(Path.Combine(@"F:\_bkp\Documents\My Mails\Foxmail\zhangyimin\temp", newName.ToString()));
			}

			return newNames;
		}
    }
}
