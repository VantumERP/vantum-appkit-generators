using System;
using System.Linq;
using System.Reflection;

namespace TestConsumer
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("=== Vantum.AppKit.Generators Test ===\n");

            // Discover all generated manifests
            var manifestTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Namespace == "Vantum.Generated" && t.Name.StartsWith("Manifest_"))
                .ToList();

            Console.WriteLine($"Found {manifestTypes.Count} generated manifest(s):\n");

            foreach (var type in manifestTypes)
            {
                var jsonField = type.GetField("Json", BindingFlags.Public | BindingFlags.Static);
                if (jsonField != null)
                {
                    var json = jsonField.GetValue(null) as string;
                    Console.WriteLine($"--- {type.Name} ---");
                    Console.WriteLine(json);
                    Console.WriteLine();
                }
            }

            if (manifestTypes.Count == 0)
            {
                Console.WriteLine("❌ No manifests found. The generator may not be working.");
            }
            else
            {
                Console.WriteLine("✅ Generator is working!");
            }
        }
    }
}
