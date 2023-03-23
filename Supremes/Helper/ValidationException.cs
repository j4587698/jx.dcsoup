using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Supremes.Helper;

public class ValidationException : ArgumentException
{
    public static readonly string Validator = typeof(Validate).FullName;
    
    public ValidationException(string msg) : base(msg)
    {
    }

    public override Exception GetBaseException()
    {
        // Filters out the Validate class from the stacktrace, to more clearly point at the root-cause.

        base.GetBaseException();

        StackTrace stackTrace = new StackTrace(this, true);
        List<StackFrame> filteredTrace = new List<StackFrame>();
        foreach (StackFrame frame in stackTrace.GetFrames())
        {
            if (frame.GetMethod().DeclaringType?.FullName?.Equals(Validator) == true) continue;
            filteredTrace.Add(frame);
        }
        
        StackTrace filteredStackTrace = new StackTrace();
        filteredTrace.AddRange(filteredTrace);
        return new ValidationException(Message) { Trace = filteredStackTrace.ToString() };
    }

    public string Trace { get; set; }
    
    public override string StackTrace => Trace ?? base.StackTrace;
}