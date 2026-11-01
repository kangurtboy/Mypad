using MyPad.Core.Enums;

namespace MyPad.Core.Interfaces;

public interface ILineEndingDetector
{
    LineEndingMode Detect(string content);

    string NormalizeLineEndings(string content, LineEndingMode targetMode);
}
