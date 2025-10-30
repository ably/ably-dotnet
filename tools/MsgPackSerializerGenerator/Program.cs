using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IO.Ably;
using IO.Ably.Types;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace MsgPackSerializerGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: MsgPackSerializerGenerator <output-directory>");
                return;
            }

            var outputDirectory = args[0];
            Console.WriteLine($"Generating MsgPack serializers to: {outputDirectory}");

            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            var applicationLibraryAssembly = typeof(ProtocolMessage).Assembly;
            
            // Validate: Check for types with JsonProperty but missing MessagePackObject
            Console.WriteLine("\n=== Validation: Checking for missing MessagePackObject annotations ===");
            var missingAnnotations = FindTypesWithJsonPropertyButNoMessagePack(applicationLibraryAssembly);
            if (missingAnnotations.Any())
            {
                Console.WriteLine("WARNING: Found types with [JsonProperty] but missing [MessagePackObject]:");
                foreach (var type in missingAnnotations.OrderBy(t => t.FullName))
                {
                    Console.WriteLine($"  ⚠️  {type.FullName}");
                }
                Console.WriteLine("\nThese types are serialized by Newtonsoft.Json but will NOT have MsgPack serializers!");
                Console.WriteLine("Please add [MessagePackObject] and [Key] attributes to these types.\n");
            }
            else
            {
                Console.WriteLine("✓ All types with [JsonProperty] have [MessagePackObject] annotations\n");
            }
            
            // Automatically discover types annotated with [MessagePackObject]
            var typesToGenerate = DiscoverMessagePackTypes(applicationLibraryAssembly);
            
            // Add System.Net.HttpStatusCode enum (from external assembly)
            var additionalTypes = new List<Type>(typesToGenerate)
            {
                typeof(System.Net.HttpStatusCode)
            };

            Console.WriteLine($"Found {additionalTypes.Count} types to generate serializers for:");
            foreach (var type in additionalTypes.OrderBy(t => t.FullName))
            {
                Console.WriteLine($"  ✓ {type.FullName}");
            }

            SerializerGenerator.GenerateCode(
                new SerializerCodeGenerationConfiguration
                {
                    Namespace = "IO.Ably.CustomSerialisers",
                    OutputDirectory = outputDirectory,
                    EnumSerializationMethod = EnumSerializationMethod.ByUnderlyingValue,
                    IsRecursive = true,
                    PreferReflectionBasedSerializer = false,
                    SerializationMethod = SerializationMethod.Map
                },
                additionalTypes.ToArray());

            Console.WriteLine("\n✓ Serializer generation complete!");
            
            if (missingAnnotations.Any())
            {
                Console.WriteLine("\n⚠️  WARNING: Some types are missing MessagePackObject annotations (see above)");
                // Don't fail the build, but make it visible
            }
        }

        /// <summary>
        /// Finds types that have JsonProperty attributes but are missing MessagePackObject annotation.
        /// This helps identify types that are serialized by Newtonsoft but might be missed for MsgPack.
        /// </summary>
        private static List<Type> FindTypesWithJsonPropertyButNoMessagePack(Assembly assembly)
        {
            var missingTypes = new List<Type>();
            
            // Types that have custom serializers and don't need MessagePackObject
            var customSerializerTypes = new HashSet<string>
            {
                "IO.Ably.Types.MessageExtras",  // Has MessageExtrasMessagePackSerializer
                "IO.Ably.Capability",           // Has CapabilityMessagePackSerializer
            };

            try
            {
                var allTypes = assembly.GetTypes();

                foreach (var type in allTypes)
                {
                    // Skip if it has a custom serializer
                    if (customSerializerTypes.Contains(type.FullName))
                        continue;

                    // Skip compiler-generated and special types
                    if (type.FullName?.Contains("<>") == true ||
                        type.FullName?.Contains("+<") == true ||
                        type.IsSpecialName)
                        continue;

                    // Check if type has MessagePackObject
                    var hasMessagePackObject = type.GetCustomAttribute<MessagePackObjectAttribute>() != null;
                    
                    if (!hasMessagePackObject)
                    {
                        // Check if any properties have JsonProperty attribute
                        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var hasJsonProperty = properties.Any(p =>
                            p.GetCustomAttribute<JsonPropertyAttribute>() != null);

                        if (hasJsonProperty)
                        {
                            missingTypes.Add(type);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Handle types that couldn't be loaded
                var loadedTypes = ex.Types.Where(t => t != null);
                foreach (var type in loadedTypes)
                {
                    if (customSerializerTypes.Contains(type.FullName))
                        continue;

                    var hasMessagePackObject = type.GetCustomAttribute<MessagePackObjectAttribute>() != null;
                    if (!hasMessagePackObject)
                    {
                        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var hasJsonProperty = properties.Any(p =>
                            p.GetCustomAttribute<JsonPropertyAttribute>() != null);

                        if (hasJsonProperty)
                        {
                            missingTypes.Add(type);
                        }
                    }
                }
            }

            return missingTypes;
        }

        /// <summary>
        /// Discovers all types in the assembly that are annotated with [MessagePackObject]
        /// </summary>
        private static List<Type> DiscoverMessagePackTypes(Assembly assembly)
        {
            var messagePackTypes = new List<Type>();

            try
            {
                // Get all types from the assembly
                var allTypes = assembly.GetTypes();

                foreach (var type in allTypes)
                {
                    // Check if type has [MessagePackObject] attribute
                    if (type.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                    {
                        messagePackTypes.Add(type);
                        
                        // Also check for nested types with [MessagePackObject]
                        var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var nestedType in nestedTypes)
                        {
                            if (nestedType.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                            {
                                messagePackTypes.Add(nestedType);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine("Warning: Some types could not be loaded:");
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine($"  - {loaderException?.Message}");
                }
                
                // Use the types that were successfully loaded
                messagePackTypes.AddRange(
                    ex.Types
                        .Where(t => t != null && t.GetCustomAttribute<MessagePackObjectAttribute>() != null)
                );
            }

            return messagePackTypes;
        }
    }
}
