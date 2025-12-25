//using System;
//using System.Reflection;
//using System.Linq;
//using UHFReaderModule;
//using UHF;

//namespace RFIDReaderPortal.Test
//{
//    public class QuickSDKCheck
//    {
//        public static string CheckSDK()
//        {
//            var output = new System.Text.StringBuilder();

//            try
//            {
//                var uhfReaderType = typeof(UHFReader);

//                // 1. Find RFIDCallBack delegate signature
//                output.AppendLine("=== RFIDCallBack Delegate ===");
//                try
//                {
//                    var callbackType = typeof(RFIDCallBack);
//                    var invoke = callbackType.GetMethod("Invoke");
//                    if (invoke != null)
//                    {
//                        var parameters = invoke.GetParameters();
//                        output.AppendLine($"Signature: {invoke.ReturnType.Name} RFIDCallBack(");
//                        foreach (var p in parameters)
//                        {
//                            output.AppendLine($"    {p.ParameterType.Name} {p.Name},");
//                        }
//                        output.AppendLine(")");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    output.AppendLine($"Error: {ex.Message}");
//                }

//                // 2. Find all methods containing "Open"
//                output.AppendLine("\n=== Methods with 'Open' ===");
//                var openMethods = uhfReaderType.GetMethods()
//                    .Where(m => m.Name.Contains("Open", StringComparison.OrdinalIgnoreCase))
//                    .ToList();

//                foreach (var method in openMethods)
//                {
//                    var parameters = string.Join(", ",
//                        method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                    output.AppendLine($"{method.ReturnType.Name} {method.Name}({parameters})");
//                }

//                // 3. Find all methods containing "Close"
//                output.AppendLine("\n=== Methods with 'Close' ===");
//                var closeMethods = uhfReaderType.GetMethods()
//                    .Where(m => m.Name.Contains("Close", StringComparison.OrdinalIgnoreCase))
//                    .ToList();

//                foreach (var method in closeMethods)
//                {
//                    var parameters = string.Join(", ",
//                        method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
//                    output.AppendLine($"{method.ReturnType.Name} {method.Name}({parameters})");
//                }

//                // 4. Find StartInventory exact signature
//                output.AppendLine("\n=== StartInventory Method ===");
//                var startMethods = uhfReaderType.GetMethods()
//                    .Where(m => m.Name == "StartInventory")
//                    .ToList();

//                foreach (var method in startMethods)
//                {
//                    output.AppendLine($"{method.ReturnType.Name} {method.Name}(");
//                    foreach (var p in method.GetParameters())
//                    {
//                        var direction = "";
//                        if (p.ParameterType.IsByRef)
//                            direction = p.IsOut ? "out " : "ref ";

//                        var typeName = p.ParameterType.Name.Replace("&", "");
//                        output.AppendLine($"    {direction}{typeName} {p.Name},");
//                    }
//                    output.AppendLine(")");
//                }

//                // 5. Find StopInventory exact signature
//                output.AppendLine("\n=== StopInventory Method ===");
//                var stopMethods = uhfReaderType.GetMethods()
//                    .Where(m => m.Name == "StopInventory")
//                    .ToList();

//                foreach (var method in stopMethods)
//                {
//                    output.AppendLine($"{method.ReturnType.Name} {method.Name}(");
//                    foreach (var p in method.GetParameters())
//                    {
//                        var direction = "";
//                        if (p.ParameterType.IsByRef)
//                            direction = p.IsOut ? "out " : "ref ";

//                        var typeName = p.ParameterType.Name.Replace("&", "");
//                        output.AppendLine($"    {direction}{typeName} {p.Name},");
//                    }
//                    output.AppendLine(")");
//                }

//            }
//            catch (Exception ex)
//            {
//                output.AppendLine($"\nError: {ex.Message}\n{ex.StackTrace}");
//            }

//            return output.ToString();
//        }
//    }
//}