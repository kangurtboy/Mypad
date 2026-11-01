using System.Text;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.Encoding;

public class EncodingDetector : IEncodingDetector
{
    private static readonly UTF8Encoding Utf8WithBom = new(true);
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private static readonly UnicodeEncoding UnicodeLe = new(false, true);
    private static readonly UnicodeEncoding UnicodeBe = new(true, true);

    public DetectedEncoding Detect(byte[] buffer, int length)
    {
        if (buffer == null || length <= 0)
        {
            return new DetectedEncoding(Utf8WithoutBom, false, 1.0f);
        }

        int scanLength = Math.Min(buffer.Length, length);

        // 1. Check for Byte Order Marks (BOM)
        if (scanLength >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            return new DetectedEncoding(Utf8WithBom, true, 1.0f);
        }

        if (scanLength >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
        {
            return new DetectedEncoding(UnicodeLe, true, 1.0f);
        }

        if (scanLength >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
        {
            return new DetectedEncoding(UnicodeBe, true, 1.0f);
        }

        // 2. Check if valid UTF-8 without BOM
        if (IsValidUtf8(buffer, scanLength))
        {
            return new DetectedEncoding(Utf8WithoutBom, false, 0.95f);
        }

        // 3. Fallback to System Default ANSI / UTF-8
        return new DetectedEncoding(Utf8WithoutBom, false, 0.5f);
    }

    private static bool IsValidUtf8(byte[] bytes, int length)
    {
        int i = 0;
        while (i < length)
        {
            byte current = bytes[i];

            if (current <= 0x7F)
            {
                i++;
                continue;
            }

            int expectedContinuationBytes;
            if ((current & 0xE0) == 0xC0)
            {
                expectedContinuationBytes = 1;
            }
            else if ((current & 0xF0) == 0xE0)
            {
                expectedContinuationBytes = 2;
            }
            else if ((current & 0xF8) == 0xF0)
            {
                expectedContinuationBytes = 3;
            }
            else
            {
                return false;
            }

            i++;
            for (int j = 0; j < expectedContinuationBytes; j++)
            {
                if (i >= length || (bytes[i] & 0xC0) != 0x80)
                {
                    return false;
                }
                i++;
            }
        }

        return true;
    }
}
