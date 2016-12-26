using System.Collections.Generic;
using System.Text;

namespace DocAssistRuntime
{
    public static class CsvHandler
    {
        public static IList<string> SplitCsvLine(this string originalLine, bool trim = false, bool trimQm = true)
        {
            var segments = new List<string>();
            var insideQuote = false;
            var sbSeg = new StringBuilder();
            var sbOriginal = new StringBuilder(originalLine);
            sbOriginal.Append(',');
            var amendedOriginal = sbOriginal.ToString();
            for (var i = 0; i < amendedOriginal.Length; i++)
            {
                var ch = amendedOriginal[i];

                if (ch == '"')
                {
                    if (!insideQuote)
                    {
                        insideQuote = true;
                        // this quotation mark is added for now and left for decision later okn
                        // quotation marks are considered part of the string only when they are not 
                        // enclosing the entire segment when trimQm is on
                        sbSeg.Append('"');
                    }
                    else
                    {
                        if (i + 1 == amendedOriginal.Length || amendedOriginal[i + 1] != '"')
                        {
                            // it's a closing quotation boundary
                            // flip the flag
                            insideQuote = false;

                            // this quotation mark is added for now
                            // quotation marks are considered part of the string only when they are not
                            // enclosing the entire segment when trimQm is on
                            sbSeg.Append('"');
                        }
                        else // dual quotation marks
                        {
                            sbSeg.Append('"');
                            i++; // skip the second quotation mark
                        }
                    }
                }
                else if (ch == ',' && !insideQuote)
                {
                    var seg = sbSeg.ToString();
                    if (trim)
                    {
                        seg = seg.Trim();
                    }
                    if (trimQm)
                    {
                        seg = seg.Trim('"');
                    }
                    segments.Add(seg);
                    sbSeg.Clear();
                }
                else
                {
                    sbSeg.Append(ch);
                }
            }
            return segments;
        }
    }
}
