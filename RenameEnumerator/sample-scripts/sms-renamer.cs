using System.IO;
using System.Collections.Generic;
using DocAssistRuntime;

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

		static bool IsAllDigit(string testee)
		{
			foreach (var ch in testee)
			{
				if (!char.IsDigit(ch))
				{
					return false;
				}
			}
			return true;
		}
		
		static bool TryParseDate(string candidate, IEnumerable<char> delimiters, out int year, out int month, out int day)
		{
			year = month = day = 0;
			candidate = candidate.Trim();
			candidate = candidate.Trim('"');
			var segs = candidate.Split(' ');
			if (segs.Length < 2) return false;	// has to be date + time
			var dateCand = segs[0];
			dateCand = dateCand.Trim(',');
			
			string[] dateSegs = null;
			foreach (var delimiter in delimiters)
			{
				dateSegs = dateCand.Split(delimiter);
				if (dateSegs.Length == 3) break;
			}
			if (dateSegs == null || dateSegs.Length != 3) return false;
			if (dateSegs[2] == "??") dateSegs[2] = "0";
			if (!int.TryParse(dateSegs[0], out year)) return false;
			if (!int.TryParse(dateSegs[1], out month)) return false;
			if (!int.TryParse(dateSegs[2], out day)) return false;
			if (year < 1900 || year > 2020) return false;
			if (month < 1 || month > 12) return false;
			if (day < 1 || day > 31) return false;
			return true;
		}
		
		static bool TryParseDate2(string candidate, out int year, out int month, out int day)
		{
			year = month = day = 0;
			candidate = candidate.Trim();
			var segs = candidate.Split(',');
			if (segs.Length < 2) return false;	// has to be date + time
			var dateCand = segs[1];
			dateCand = dateCand.Trim();
			string[] dateSegs = dateCand.Split('.');
			if (dateSegs.Length != 3) return false;
			if (dateSegs[2] == "??") dateSegs[2] = "0";
			if (!int.TryParse(dateSegs[0], out year)) return false;
			if (!int.TryParse(dateSegs[1], out month)) return false;
			if (!int.TryParse(dateSegs[2], out day)) return false;
			if (year < 1900 || year > 2020) return false;
			if (month < 1 || month > 12) return false;
			if (day < 1 || day > 31) return false;
			return true;
		}
	
		static string ProcessText(FileInfo file)
		{
			int minDateYear = 0, minDateMonth = 0, minDateDay = 0;
			int maxDateYear = 0, maxDateMonth = 0, maxDateDay = 0;
			var firstTime = true;
			using (var sr = new StreamReader(file.OpenRead()))
			{
				string line;
				while (!sr.EndOfStream && (line=sr.ReadLine())!=null)
				{
					int year, month, day;
					if (!TryParseDate(line, new []{'-','.'}, out year, out month, out day)) 
					{
						if (!TryParseDate2(line, out year, out month, out day)) continue;
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
			
			if (firstTime)
			{
				return file.Name;
			}
			
			var origName = Path.GetFileNameWithoutExtension(file.Name);
			var segsOrigName = origName.Split('_');
			string suffix="";
			if (segsOrigName.Length>0 && segsOrigName[0] == "sms")
			{
				var i = 1;
				for ( ; i<segsOrigName.Length && IsAllDigit(segsOrigName[i]); i++);
				for ( ; i < segsOrigName.Length; i++)
				{
					suffix += "-";
					suffix += segsOrigName[i];
				}
			}
			
			var newName = string.Format("{0:0000}{1:00}{2:00}-{3:0000}{4:00}{5:00}{6}.txt", minDateYear, minDateMonth, minDateDay, maxDateYear, maxDateMonth, maxDateDay, suffix);
			return newName;
		}
		
		static string ProcessCsv(FileInfo file)
		{
			int minDateYear = 0, minDateMonth = 0, minDateDay = 0;
			int maxDateYear = 0, maxDateMonth = 0, maxDateDay = 0;
			var firstTime = true;
			using (var f = file.OpenRead())
			{
				using (var sr = new StreamReader(f))
				{
					string line;
					
					while (!sr.EndOfStream && (line=sr.ReadLine())!=null)
					{
						line = line.Trim();
						if (line == string.Empty) continue;
						var segs = line.SplitCsvLine();
						foreach (var seg in segs)
						{
							int year, month, day;
							
							if (TryParseDate(seg, new []{'.'}, out year, out month, out day))
							{
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
								break;
							}
						}
					}
				}
			}
			
			var origName = Path.GetFileNameWithoutExtension(file.Name);
			var segsOrigName = origName.Split('_');
			string suffix="";
			if (segsOrigName.Length>0 && segsOrigName[0] == "sms")
			{
				var i = 1;
				for ( ; i<segsOrigName.Length && IsAllDigit(segsOrigName[i]); i++);
				for ( ; i < segsOrigName.Length; i++)
				{
					suffix += "-";
					suffix += segsOrigName[i];
				}
			}
			
			var newName = string.Format("{0:0000}{1:00}{2:00}-{3:0000}{4:00}{5:00}{6}.csv", minDateYear, minDateMonth, minDateDay, maxDateYear, maxDateMonth, maxDateDay, suffix);
			return newName;
		}
		
		static int TestSmsType(FileInfo file)
		{
			using (var f = file.OpenRead())
			{
				using (var sr = new StreamReader(f))
				{
					string line;
					bool hasNonEmptyLine = false;
					while (!sr.EndOfStream && (line=sr.ReadLine())!=null)
					{
						line = line.Trim();
						if (line == string.Empty) continue;
						hasNonEmptyLine = true;
						var segs = line.SplitCsvLine();
						if (segs.Count==1) return 2;
					}
					return hasNonEmptyLine? 1 : 0;
				}
			}
		}
	
		public static IList<string> RenameFiles(IEnumerable<FileInfo> files)
        {
			var newNames = new List<string>();
			foreach (var file in files)
            {
				var type = TestSmsType(file);
				
				if (type == 1)	//csv
				{
					var newName = ProcessCsv(file);
					newNames.Add(newName);
				}
				else if (type == 2) //txt
				{
					var newName = ProcessText(file);
					newNames.Add(newName);
				}
				else
				{
					newNames.Add("");
				}
			}
			return newNames;
		}
	}
}
