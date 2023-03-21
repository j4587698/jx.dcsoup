using System;
using System.IO;

namespace Supremes;

public class UncheckedIOException : Exception
{
    public UncheckedIOException(IOException cause)
        : base(cause.Message, cause)
    {
    }

    public UncheckedIOException(string message, IOException cause)
        : base(message, cause)
    {
    }
}