using System;
using System.IO;

namespace EnterpriseAI.Domain.Utils
{
    public static class PathHelper
    {




        public static string GetAbsolutePathRelativeToSolution(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Length > 0)
                {

                    return Path.GetFullPath(Path.Combine(directory.FullName, relativePath));
                }

                directory = directory.Parent;
            }


            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        }
    }
}

