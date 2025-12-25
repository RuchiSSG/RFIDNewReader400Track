//using System;
//using System.Reflection;
//using System.Linq;
//using System.Text;
//using UHFReaderModule;
//using UHF;

//namespace RFIDReaderPortal.Diagnostics
//{
//    /// <summary>
//    /// Run this once to discover your exact UHF SDK structure
//    /// Add a temporary controller action to call this
//    /// </summary>
//    public class UHFSDKDiagnostics
//    {
//        public static string DiscoverSDK()
//        {
//            var output = new StringBuilder();
//            output.AppendLine("=== UHF SDK STRUCTURE DISCOVERY ===\n");

//            try
//            {
//                // Get UHFReaderModule assembly
//                var uhfReaderType = typeof(UHFReader);
//                var assembly = uhfReaderType.Assembly;

//                output.AppendLine($"Assembly: {assembly.FullName}\n");
//                output.AppendLine(new string('=', 60) + "\n");

//                // Find all types in the assembly
//                var allTypes = assembly.GetTypes()
//                    .Where(t => t.IsPublic)
//                    .OrderBy(t => t.Name)
//                    .ToList();

//                output.AppendLine($"Found {allTypes.Count} public types\n");

//                // 1. Find main reader class
//                output.AppendLine("MAIN READER CLASS:");
//                output.AppendLine("==================");

//                var readerMethods = uhfReaderType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
//                    .Where(m => !m.IsSpecialName)
//                    .OrderBy(m => m.Name);

//                output.AppendLine($"\nUHFReader Methods ({readerMethods.Count()}):");
//                foreach (var method in readerMethods)
//                {
//                    var parameters = string.Join(", ",
//                        method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                    output.AppendLine($"  {method.ReturnType.Name} {method.Name}({parameters})");
//                }

//                // 2. Find events
//                output.AppendLine("\n\nUHFReader EVENTS:");
//                output.AppendLine("=================");
//                var events = uhfReaderType.GetEvents(BindingFlags.Public | BindingFlags.Instance);

//                if (events.Length == 0)
//                {
//                    output.AppendLine("  No events found");
//                }
//                else
//                {
//                    foreach (var evt in events)
//                    {
//                        output.AppendLine($"\n  Event: {evt.Name}");
//                        output.AppendLine($"  Type: {evt.EventHandlerType.Name}");

//                        // Get delegate signature
//                        var invokeMethod = evt.EventHandlerType.GetMethod("Invoke");
//                        if (invokeMethod != null)
//                        {
//                            var evtParams = string.Join(", ",
//                                invokeMethod.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                            output.AppendLine($"  Signature: void ({evtParams})");
//                        }
//                    }
//                }

//                // 3. Find properties
//                output.AppendLine("\n\nUHFReader PROPERTIES:");
//                output.AppendLine("=====================");
//                var properties = uhfReaderType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

//                foreach (var prop in properties)
//                {
//                    output.AppendLine($"  {prop.PropertyType.Name} {prop.Name} " +
//                        $"{{ {(prop.CanRead ? "get;" : "")} {(prop.CanWrite ? "set;" : "")} }}");
//                }

//                // 4. Find all EventArgs classes
//                output.AppendLine("\n\nEVENT ARGS CLASSES:");
//                output.AppendLine("===================");
//                var eventArgsTypes = allTypes
//                    .Where(t => t.Name.Contains("EventArgs") || t.Name.Contains("Args"))
//                    .ToList();

//                if (eventArgsTypes.Count == 0)
//                {
//                    output.AppendLine("  No EventArgs classes found");
//                }
//                else
//                {
//                    foreach (var argsType in eventArgsTypes)
//                    {
//                        output.AppendLine($"\n  Class: {argsType.Name}");

//                        var argsProps = argsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//                        if (argsProps.Length > 0)
//                        {
//                            output.AppendLine("  Properties:");
//                            foreach (var prop in argsProps)
//                            {
//                                output.AppendLine($"    {prop.PropertyType.Name} {prop.Name}");
//                            }
//                        }
//                    }
//                }

//                // 5. Find delegate types
//                output.AppendLine("\n\nDELEGATE TYPES:");
//                output.AppendLine("===============");
//                var delegateTypes = allTypes
//                    .Where(t => typeof(Delegate).IsAssignableFrom(t))
//                    .ToList();

//                foreach (var delType in delegateTypes)
//                {
//                    output.AppendLine($"\n  Delegate: {delType.Name}");
//                    var invoke = delType.GetMethod("Invoke");
//                    if (invoke != null)
//                    {
//                        var delParams = string.Join(", ",
//                            invoke.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                        output.AppendLine($"  Signature: {invoke.ReturnType.Name} ({delParams})");
//                    }
//                }

//                // 6. Check for common method names
//                output.AppendLine("\n\nCONNECTION METHODS FOUND:");
//                output.AppendLine("=========================");
//                var connectionMethods = new[] { "Connect", "Open", "OpenNet", "OpenNetPort", "OpenConnection", "Initialize" };
//                foreach (var methodName in connectionMethods)
//                {
//                    var methods = uhfReaderType.GetMethods()
//                        .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
//                        .ToList();

//                    if (methods.Any())
//                    {
//                        foreach (var method in methods)
//                        {
//                            var parameters = string.Join(", ",
//                                method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                            output.AppendLine($"  ✓ {method.Name}({parameters}) : {method.ReturnType.Name}");
//                        }
//                    }
//                    else
//                    {
//                        output.AppendLine($"  ✗ {methodName} - not found");
//                    }
//                }

//                output.AppendLine("\n\nREADING METHODS FOUND:");
//                output.AppendLine("======================");
//                var readingMethods = new[] { "StartInventory", "StopInventory", "StartReading", "ReadTags", "Inventory" };
//                foreach (var methodName in readingMethods)
//                {
//                    var methods = uhfReaderType.GetMethods()
//                        .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
//                        .ToList();

//                    if (methods.Any())
//                    {
//                        foreach (var method in methods)
//                        {
//                            var parameters = string.Join(", ",
//                                method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                            output.AppendLine($"  ✓ {method.Name}({parameters}) : {method.ReturnType.Name}");
//                        }
//                    }
//                    else
//                    {
//                        output.AppendLine($"  ✗ {methodName} - not found");
//                    }
//                }

//                // 7. Test instantiation
//                output.AppendLine("\n\nINSTANTIATION TEST:");
//                output.AppendLine("===================");
//                try
//                {
//                    var reader = new UHFReader();
//                    output.AppendLine("  ✓ new UHFReader() - SUCCESS");

//                    // Check if it has an IsConnected property
//                    var isConnectedProp = uhfReaderType.GetProperty("IsConnected");
//                    if (isConnectedProp != null)
//                    {
//                        output.AppendLine($"  ✓ IsConnected property exists");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    output.AppendLine($"  ✗ Failed to create instance: {ex.Message}");
//                }

//                output.AppendLine("\n\n=== DISCOVERY COMPLETE ===");

//            }
//            catch (Exception ex)
//            {
//                output.AppendLine($"\n\n✗ ERROR: {ex.Message}");
//                output.AppendLine($"\nStack Trace:\n{ex.StackTrace}");
//            }

//            return output.ToString();
//        }
//    }
//}