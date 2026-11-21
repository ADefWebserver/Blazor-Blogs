namespace BlazorBlogs.Data;

public sealed class ThemePackageValidationException(string message) : Exception(message);

public sealed class ThemePackageConflictException(string message) : Exception(message);

public sealed class ThemePackageTooLargeException(string message) : Exception(message);
