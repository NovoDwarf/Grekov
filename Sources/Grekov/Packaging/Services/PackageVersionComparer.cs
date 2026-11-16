using Grekov.Abstractions;

namespace Grekov.Packaging.Services;

internal static class PackageVersionComparer
{
    public static bool Satisfies(string actual, PackageDependency dependency)
    {
        if (dependency.Operator == PackageDependencyOperator.Any)
        {
            return true;
        }

        if (!TryParse(actual, out var version, out var suffix)
            || !TryParse(dependency.Version, out var required, out var requiredSuffix))
        {
            return false;
        }

        var comparison = version.CompareTo(required);

        if (comparison == 0)
        {
            comparison = CompareSuffix(suffix, requiredSuffix);
        }

        return dependency.Operator switch
        {
            PackageDependencyOperator.Exact => comparison == 0,
            PackageDependencyOperator.AtLeast => comparison >= 0,
            _ => false
        };
    }

    private static bool TryParse(string? value, out Version version, out string suffix)
    {
        version = new Version(0, 0, 0, 0);
        suffix = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('+', 2)[0].Split('-', 2);
        var numbers = parts[0].Split('.');

        if (numbers.Length is < 1 or > 4)
        {
            return false;
        }

        var components = new int[4];

        for (var index = 0; index < numbers.Length; index++)
        {
            if (!int.TryParse(numbers[index], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out components[index]))
            {
                return false;
            }
        }

        if (parts.Length == 2)
        {
            suffix = parts[1];

            if (suffix.Split('.').Any(static part => part.Length == 0
                || part.Any(static character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
            {
                return false;
            }
        }

        version = new Version(components[0], components[1], components[2], components[3]);
        return true;
    }

    private static int CompareSuffix(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return (left.Length == 0).CompareTo(right.Length == 0);
        }

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');

        for (var index = 0; index < Math.Min(leftParts.Length, rightParts.Length); index++)
        {
            var leftNumber = leftParts[index].All(char.IsAsciiDigit);
            var rightNumber = rightParts[index].All(char.IsAsciiDigit);
            var comparison = leftNumber && rightNumber
                ? CompareNumbers(leftParts[index], rightParts[index])
                : leftNumber != rightNumber
                    ? rightNumber.CompareTo(leftNumber)
                    : string.CompareOrdinal(leftParts[index], rightParts[index]);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    private static int CompareNumbers(string left, string right)
    {
        left = left.TrimStart('0');
        right = right.TrimStart('0');
        var comparison = left.Length.CompareTo(right.Length);
        return comparison != 0 ? comparison : string.CompareOrdinal(left, right);
    }
}
