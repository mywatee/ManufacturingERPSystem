using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using ManufacturingERP.Models;

namespace ManufacturingERP.Core;

public class MessageToSegmentsConverter : IValueConverter
{
    private static readonly Regex CodeBlockRegex = new(@"```(\w*)\n?(.*?)```",
        RegexOptions.Singleline);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrEmpty(text))
            return new List<ChatDisplaySegment> { new() { Content = text ?? "", IsCode = false } };

        var segments = new List<ChatDisplaySegment>();
        int lastIndex = 0;

        foreach (Match match in CodeBlockRegex.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                segments.Add(new ChatDisplaySegment
                {
                    Content = text[lastIndex..match.Index].TrimEnd(),
                    IsCode = false
                });
            }

            segments.Add(new ChatDisplaySegment
            {
                Content = match.Groups[2].Value.Trim(),
                IsCode = true
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            segments.Add(new ChatDisplaySegment
            {
                Content = text[lastIndex..].TrimStart(),
                IsCode = false
            });
        }

        if (segments.Count == 0)
            segments.Add(new ChatDisplaySegment { Content = text, IsCode = false });

        return segments;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
