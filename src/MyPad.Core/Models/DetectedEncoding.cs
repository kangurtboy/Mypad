using System.Text;

namespace MyPad.Core.Models;

public record DetectedEncoding(Encoding Encoding, bool HasBom, float Confidence);
