using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;

namespace GitVersion
{
    class Program
    {
        static int Main()
        {
            Console.Out.WriteLine($"C2C GitVersion {Assembly.GetExecutingAssembly().GetName().Version} by Alberts2");
            Console.Out.WriteLine();
            var c2cVersion = "0.0.0";
            var gitVersion = "default-0";
            var sha = "0";
            var shortSha = "0";
            var error = false;
            var isAppveyorBuild = false;
            try
            {
                var repoPath = Path.GetFullPath("..\\");

                var appVeyorVersion = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_VERSION");
                if (appVeyorVersion != null)
                {
                    c2cVersion = appVeyorVersion;
                    Console.Out.WriteLine($"APPVEYOR_BUILD_VERSION:{c2cVersion}");
                    isAppveyorBuild = true;
                }
                else
                {
                    using (var input = new StreamReader(repoPath + "appveyor.yml"))
                    {
                        Dictionary<object, object> yamlObject = (Dictionary<object, object>) new Deserializer().Deserialize(input);
                        var hasVer = yamlObject.TryGetValue("version", out var data);
                        if (hasVer)
                        {
                            c2cVersion = data.ToString().Replace("{build}", "0");
                            Console.Out.WriteLine($"appveyor.yml-c2c-version:{c2cVersion}");
                        }
                    }
                }

                using (var repo = new Repository(repoPath))
                {
                    var tip = repo.Head.Tip;
                    if (tip != null)
                    {
                        sha = tip.Id.ToString();
                        shortSha = sha.Substring(0, 7);
                        var tag = repo.Tags.FirstOrDefault(t => t.Target.Sha == tip.Sha);
                        if (!isAppveyorBuild)
                        {
                            gitVersion = tag != null ? tag.FriendlyName : $"{repo.Head.FriendlyName}-{shortSha}";
                            using (var changes = repo.Diff.Compare<TreeChanges>(new[] { "Sources" }, true))
                            {
                                if (changes.Any())
                                {
                                    gitVersion += "-dirty";
                                    sha += "-dirty";
                                    shortSha += "-dirty";
                                }
                            } 
                        }
                        else
                        {
                            var appVeyorBranch = Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");
                            gitVersion = tag != null ? tag.FriendlyName : $"{(appVeyorBranch != null ? appVeyorBranch : "no-branch")}-{shortSha}";
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
                Console.WriteLine($"build_git_version={gitVersion}");
                Console.WriteLine($"build_c2c_version={c2cVersion}");
                var path = Path.GetFullPath("..\\Sources\\version.cpp");
                var text = "/* version.cpp */\r\n" +
                           "#include \"CvGameCoreDLL.h\"\r\n" +
                           "#include \"version.h\"\r\n" +
                           $"const char * build_c2c_version = \"{c2cVersion}\";\r\n" +
                           $"const char * build_git_sha = \"{sha}\";\r\n" +
                           $"const char * build_git_short_sha = \"{shortSha}\";\r\n" +
                           $"const char * build_git_version = \"{gitVersion}\";";

                if (File.Exists(path)
                {
                    if(File.ReadAllText(path) == text))
                        return 0;
                }
                try
                {
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
