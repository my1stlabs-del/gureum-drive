using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
class EngineCheck {
 static void Main() { try { Run().GetAwaiter().GetResult(); } catch(Exception e) { Console.WriteLine(e.GetType().FullName); Console.WriteLine(e.Message); if(e.InnerException!=null) Console.WriteLine(e.InnerException.Message); Environment.ExitCode=1; } }
 static async Task Run() {
  var type=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CloudDock.exe")).GetType("CloudDock.Engine");
  string fixture=Path.Combine(Path.GetTempPath(),"CloudDock-engine-check-"+Guid.NewGuid().ToString("N"));
  var engine=Activator.CreateInstance(type,new object[]{fixture});
  try {
   await (Task)type.GetMethod("Start").Invoke(engine,new object[0]);
   var method=type.GetMethod("Job");
   var result=await (Task<Dictionary<string,object>>)method.Invoke(engine,new object[]{"core/version",new Dictionary<string,object>(),null});
   Console.WriteLine("PASS: engine startup and async RC job: "+result["version"]);
   try {
    await (Task)method.Invoke(engine,new object[]{"core/version",new Dictionary<string,object>(),new Func<bool>(()=>true)});
    throw new Exception("Cancellation ignored");
   } catch(OperationCanceledException) { Console.WriteLine("PASS: completed-job cancellation"); }
   Console.WriteLine("Preserved fixture: "+fixture);
  } finally { ((IDisposable)engine).Dispose(); }
 }
}
