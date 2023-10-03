using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mono.Cecil;

using BBRAPIModules;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;

namespace Bluscream {
    public class DocumentationGenerator : BattleBitModule {


        public abstract class BaseDefinition {
            public string? Type { get; set; }
            public string? Name { get; set; }
            public bool? Static { get; set; }
            public bool? Abstract { get; set; }
            public bool? Virtual { get; set; }
        }

        public class ConfigStructure {
            public string? Name { get; set; }
            public FileInfo? Path { get; set; }
            public bool Global { get; set; }
            public Dictionary<string, string> Fields { get; set; } = new();
            public Dictionary<string, string> Properties { get; set; } = new();
            public Dictionary<string, object> Content { get; set; } = new();
        }

        public class CommandInfo {
            public string? Name { get; set; }
            public string? MethodName { get; set; }
            public string? Description { get; set; }
            public List<string> Permissions { get; set; } = new();
        }
        public class ModuleFileMetaData {
            public FileInfo? Path { get; set; }
            public List<ModuleMetaData> Modules { get; set; } = new();
        }

        public class ModuleMetaData {
            public string? Name { get; set; }
            public string? Version { get; set; }
            public List<CommandInfo> Commands { get; set; } = new();
            public List<ConfigStructure> ConfigStructures { get; set; } = new();
        }

        public class ModuleParser {
            [Obsolete]
            public List<ModuleFileMetaData> ParseModules(DirectoryInfo directory) {
                var moduleInfos = new List<ModuleFileMetaData>();
                foreach (var file in directory.GetFiles("*.cs")) {
                    moduleInfos.Add(new() { Path = file, Modules = ParseModule(file) });
                }
                return moduleInfos;
            }

            [Obsolete]
            public List<ModuleMetaData> ParseModule(FileInfo file) {
                var moduleInfos = new List<ModuleMetaData>();
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(file.FullName);
                foreach (var module in assembly.Modules) {
                    var moduleInfo = new ModuleMetaData {
                        Name = module.Name,
                        Version = module.Assembly.Name.Version.ToString(),
                        Commands = GetCommands(module),
                        ConfigStructures = GetConfigStructures(module)
                    };
                    moduleInfos.Add(moduleInfo);
                }
                return moduleInfos;
            }
            public List<CommandInfo> GetCommands(Mono.Cecil.ModuleDefinition module) {
                var commandInfos = new List<CommandInfo>();

                foreach (var type in module.Types) {
                    foreach (var method in type.Methods) {
                        var commandAttribute = method.CustomAttributes
                            .FirstOrDefault(a => a.AttributeType.Name == "CommandCallbackAttribute");

                        if (commandAttribute != null) {
                            var commandInfo = new CommandInfo() {
                                Name = (string)commandAttribute.ConstructorArguments[0].Value,
                                Description = (string)commandAttribute.Properties.FirstOrDefault(p => p.Name == "Description").Argument.Value,
                                Permissions = ((string[])commandAttribute.Properties.FirstOrDefault(p => p.Name == "Permissions").Argument.Value).ToList()
                            };

                            commandInfos.Add(commandInfo);
                        }
                    }
                }
                return commandInfos;
            }

            [Obsolete]
            public List<ConfigStructure> GetConfigStructures(Mono.Cecil.ModuleDefinition module) {
                var configStructures = new List<ConfigStructure>();
                foreach (var type in module.Types) {
                    if (type.BaseType != null && type.BaseType.Name == "ModuleConfiguration") {
                        var cfg = new ConfigStructure() { Name = type.Name, Global = type.IsStatic() };
                        foreach (var field in type.Fields) {
                            cfg.Fields.Add(field.FieldType.Name, field.Name);
                        }
                        foreach (var property in type.Properties) {
                            cfg.Fields.Add(property.PropertyType.Name, property.Name);
                        }
                        cfg.Path = new FileInfo(cfg.Global ? "configurations" : GetServerConfigs(new DirectoryInfo("configurations"), type.Name + ".json").First()?.FullName);
                        if (cfg.Path.Exists) {
                            var configText = cfg.Path.ReadAllText();
                            var configJson = JsonSerializer.Deserialize<Dictionary<string, object>>(configText);
                            cfg.Content = configJson!;
                            configStructures.Add(cfg);
                        }
                    }
                }

                return configStructures;
            }

            public DirectoryInfo? GetServerConfigsDirectory(DirectoryInfo parentDirectory, IPAddress ip, int port) {
                var regex = new Regex($"^{Regex.Escape(ip.ToString())}_{Regex.Escape(port.ToString())}$");
                return FindServerConfigDirs(parentDirectory).FirstOrDefault(dir => regex.IsMatch(dir.Name));
            }
            public List<DirectoryInfo> FindServerConfigDirs(DirectoryInfo parentDirectory) {
                var regex = new Regex($"^[\\w.]+_\\d+$");
                return parentDirectory.GetDirectories().Where(dir => regex.IsMatch(dir.Name)).ToList();
            }
            public List<FileInfo> GetServerConfigs(DirectoryInfo parentDirectory, string configName) {
                var ret = new List<FileInfo>();
                var dirs = FindServerConfigDirs(parentDirectory);
                foreach (var dir in dirs) {
                    foreach (var file in dir.GetFiles()) {
                        if (file.Name == configName) ret.Add(file);
                    }
                }
                return ret;
            }
        }
    }

    public static partial class Extensions {
        public static bool IsStatic(this Mono.Cecil.TypeDefinition type) {
            return type.IsSealed && type.IsAbstract;
        }
    }

}
