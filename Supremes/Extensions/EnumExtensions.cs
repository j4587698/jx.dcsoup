using System;

namespace Supremes.Extensions;

public static class EnumExtensions
{
    public static bool HasBody(this IConnection.Method method) {
        switch(method) {
            case IConnection.Method.Get:
            case IConnection.Method.Delete:
            case IConnection.Method.Head:
            case IConnection.Method.Options:
            case IConnection.Method.Trace:
                return false;
            case IConnection.Method.Post:
            case IConnection.Method.Put:
            case IConnection.Method.Patch:
                return true;
            default:
                throw new ArgumentException("Invalid HTTP method.");
        }
    }
}