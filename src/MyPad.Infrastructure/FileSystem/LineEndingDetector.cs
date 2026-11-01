using MyPad.Core.Enums;
using MyPad.Core.Interfaces;

namespace MyPad.Infrastructure.FileSystem;

public class LineEndingDetector : ILineEndingDetector
{
    public LineEndingMode Detect(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return LineEndingMode.Crlf;
        }

        int crlfCount = 0;
        int lfCount = 0;
        int crCount = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (c == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++; // skip LF
                }
                else
                {
                    crCount++;
                }
            }
            else if (c == '\n')
            {
                lfCount++;
            }
        }

        int nonZeroTypes = (crlfCount > 0 ? 1 : 0) + (lfCount > 0 ? 1 : 0) + (crCount > 0 ? 1 : 0);

        if (nonZeroTypes > 1)
        {
            return LineEndingMode.Mixed;
        }

        if (lfCount > 0)
        {
            return LineEndingMode.Lf;
        }

        if (crCount > 0)
        {
            return LineEndingMode.Cr;
        }

        return LineEndingMode.Crlf;
    }

    public string NormalizeLineEndings(string content, LineEndingMode targetMode)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        string targetEnding = targetMode switch
        {
            LineEndingMode.Lf => "\n",
            LineEndingMode.Cr => "\r",
            _ => "\r\n"
        };

        // First normalize to LF, then replace with target
        string unified = content.Replace("\r\n", "\n").Replace("\r", "\n");
        return targetEnding == "\n" ? unified : unified.Replace("\n", targetEnding);
    }
}
