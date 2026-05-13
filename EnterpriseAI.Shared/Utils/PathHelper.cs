using System;
using System.IO;

namespace EnterpriseAI.Shared.Utils
{
    public static class PathHelper
    {
        /// <summary>
        /// Finds the absolute path by locating the solution root directory and appending the relative path.
        /// It traverses upwards from the application base directory until it finds the .sln file.
        /// </summary>
        public static string GetAbsolutePathRelativeToSolution(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Length > 0)
                {
                    // Found the solution root
                    return Path.GetFullPath(Path.Combine(directory.FullName, relativePath));
                }

                directory = directory.Parent;
            }

            // Fallback if .sln is not found (e.g. published app in production)
            // Just use the application base directory
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        }
    }
}
