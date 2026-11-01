using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

public interface IEncodingDetector
{
    DetectedEncoding Detect(byte[] buffer, int length);
}
