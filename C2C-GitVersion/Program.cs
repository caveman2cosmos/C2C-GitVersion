using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GitVersion
{
    class Program
    {
        static int Main()
        {
            Console.Out.WriteLine($"C2C GitVersion {Assembly.GetExecutingAssembly().GetName().Version} by Alberts2");
            Console.Out.WriteLine();
            var version = "default-0";
            var sha = "0";
            var error = false;
            try
            {
                var repoPath = Path.GetFullPath("..\\");
                using (var repo = new Repository(repoPath))
                {
                    var tip = repo.Head.Tip;
                    if (tip != null)
                    {
                        sha = tip.Id.ToString();
                        var tag = repo.Tags.FirstOrDefault(t => t.Target.Sha == tip.Sha);
                        version = tag != null ? tag.FriendlyName : $"{repo.Head.FriendlyName}-{tip.Id.ToString().Substring(0, 7)}";

                        using (var changes = repo.Diff.Compare<TreeChanges>(new[] { "Sources" }, true))
                        {
                            if (changes.Any())
                            {
                                sha += "-dirty";
                                version += "-dirty";
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                error = true;
            }

            if (!error)
            {
                Console.WriteLine($"build_git_sha={sha}");
                Console.WriteLine($"build_git_version={version}");

                var text = "/* version.c */\r\n" +
                           "#include \"CvGameCoreDLL.h\"\r\n" +
                           "#include \"version.h\"\r\n" +
                           $"const char * build_git_sha = \"{sha}\";\r\n" +
                           $"const char * build_git_version = \"{version}\";";
                try
                {
                    var path = Path.GetFullPath("..\\Sources\\version.cpp");
                    Console.WriteLine($"writing to {path}");
                    File.WriteAllText(path, text);
                    Console.WriteLine("done");
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return -1;
        }
    }
}
