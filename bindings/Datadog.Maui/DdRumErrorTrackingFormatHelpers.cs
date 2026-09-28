/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using System;
using System.Diagnostics;
using System.Reflection;

namespace Datadog.Maui
{
    internal static class DdRumErrorTrackingFormatHelpers
    {
        internal static string FormatManagedFrame(StackFrame frame, MethodBase method)
        {
            var declaringType = method.DeclaringType?.FullName;
            var qualifiedMethod = string.IsNullOrEmpty(declaringType)
                ? method.Name
                : $"{declaringType}.{method.Name}";
            var parameters = string.Join(", ", method.GetParameters().Select(FormatParameter));
            var rendered = $"   at {qualifiedMethod}({parameters})";

            var fileName = frame.GetFileName();
            var lineNumber = frame.GetFileLineNumber();
            if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
            {
                rendered += $" in {fileName}:line {lineNumber}";
            }

            return rendered;
        }

        private static string FormatParameter(ParameterInfo parameter)
        {
            var typeName = parameter.ParameterType.FullName ?? parameter.ParameterType.Name;
            return string.IsNullOrEmpty(parameter.Name) ? typeName : $"{typeName} {parameter.Name}";
        }
    }
}
