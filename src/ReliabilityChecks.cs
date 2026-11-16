using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CloudDock {
static class ReliabilityChecks {
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run() {
        string root=Path.Combine(Path.GetTempPath(),"CloudDock-reliability-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        ExtendedPathChecks(root);
        string file=Path.Combine(root,"names.json"); File.WriteAllText(file,"{\"drive\":\"Original\"}");
        var store=new LocalSettingsStore(); Check(J.Str(store.Read(file),"drive")=="Original","Initial settings read");
        using(var held=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {
            Check(J.Str(store.Read(file),"drive")=="Original","Locked settings must retain last good values");
            bool blocked=false; try { store.Read(file,true); } catch(IOException) { blocked=true; }
            Check(blocked,"Locked settings must block saving");
            var cold=new LocalSettingsStore(); cold.Read(file); blocked=false; try { cold.Read(file,true); } catch(IOException) { blocked=true; }
            Check(blocked,"Unreadable settings must not become writable defaults");
        }
        Check(File.ReadAllText(file)=="{\"drive\":\"Original\"}","Locked settings changed");
        File.WriteAllText(file,"{broken"); Check(store.Read(file).Count==0,"Corrupt settings fallback");
        store.Read(file); Check(Directory.GetFiles(root,"*.corrupt-*").Length==1,"Repeated backup");
        Check(File.ReadAllText(Directory.GetFiles(root,"*.corrupt-*")[0])=="{broken","Backup contents");
        File.WriteAllText(file,"null"); store.Read(file); Check(Directory.GetFiles(root,"*.corrupt-*").Length==2,"New corruption needs a new backup");
        File.WriteAllText(file,"{\"drive\":\"Recovered\"}"); Check(J.Str(store.Read(file,true),"drive")=="Recovered","Recovered settings must become writable");
        string meta=Path.Combine(root,"vfsMeta"); Directory.CreateDirectory(meta);
        string a=Path.Combine(meta,"drive","sub"); Directory.CreateDirectory(a); File.WriteAllText(Path.Combine(a,"dirty"),"{\"Dirty\":true}");
        Check(PendingState.RemoteCacheBusy(meta,"drive"),"Account cache missing");
        Check(!PendingState.RemoteCacheBusy(meta,"drive-2"),"Other account must not block removal");
        string variant=Path.Combine(meta,"onedrive{Ab_12}","folder"); Directory.CreateDirectory(variant); File.WriteAllText(Path.Combine(variant,"dirty"),"{\"Dirty\":true}");
        Check(PendingState.RemoteCacheBusy(meta,"onedrive"),"Override suffix cache missing");
        Check(!PendingState.RemoteCacheBusy(meta,"one"),"Account prefix must not match");
        Check(PendingState.RemoteCacheBusy(meta,"../unsafe"),"Unknown name requires confirmation");
        Check(StartupDiagnostics.Classify("token=secret https://host/private?key=secret C:\\private") == "ENGINE_START_FAILED","Raw diagnostics must not escape");
        Check(StartupDiagnostics.Classify("bind: address already in use token=secret")=="RC_PORT_IN_USE","Safe diagnostic category");
        JobChecks().GetAwaiter().GetResult();
        EngineGuardChecks().GetAwaiter().GetResult();
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"reliability-test.txt"),"PASS: settings lock fallback/write protection/recovery/corruption backups, account cache isolation/hash suffix, diagnostics allow-list, RC cancellation completion race/unconfirmed failures/config mutation guards. Preserved fixtures: "+root);
    }
    static void ExtendedPathChecks(string root) {
        string normal=Path.Combine(root,"확장 경로 검사"); Directory.CreateDirectory(normal);
        string extended=@"\\?\"+normal;
        // These are private synthetic fixtures, never live VFS cache files.
        string clean=Path.Combine(extended,"clean"); Directory.CreateDirectory(clean);
        string cleanFile=Path.Combine(clean,"meta.json"); File.WriteAllText(cleanFile,"{\"Dirty\":false}");
        Check(!PendingState.CacheBusy(J.Obj("pathMeta",clean)),"Extended clean path must allow disconnect");
        string dirty=Path.Combine(extended,"dirty"); Directory.CreateDirectory(dirty);
        File.WriteAllText(Path.Combine(dirty,"meta.json"),"{\"Dirty\":true}");
        Check(PendingState.CacheBusy(J.Obj("pathMeta",dirty)),"Extended dirty path must block disconnect");
        using(var held=new FileStream(cleanFile,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            Check(PendingState.CacheBusy(J.Obj("pathMeta",clean)),"Extended locked metadata must block disconnect");
        string deep=Path.Combine(extended,new string('a',100),new string('b',100),"긴 경로"); Directory.CreateDirectory(deep);
        Check(deep.Length>260,"Long-path fixture must exceed MAX_PATH");
        File.WriteAllText(Path.Combine(deep,"meta.json"),"{\"Dirty\":true}");
        Check(PendingState.CacheBusy(J.Obj("pathMeta",deep)),"Long extended dirty path must block disconnect");
        string metaRoot=Path.Combine(extended,"vfsMeta"); string account=Path.Combine(metaRoot,"onedrive"); Directory.CreateDirectory(account);
        File.WriteAllText(Path.Combine(account,"meta.json"),"{\"Dirty\":true}");
        Check(PendingState.RemoteCacheBusy(metaRoot,"onedrive"),"Extended remote cache inspection");
        Check(!PendingState.RemoteCacheBusy(metaRoot,"drive"),"Extended remote cache isolation");
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"extended-path-test.txt"),"PASS: extended Unicode clean/dirty/locked cache, path longer than 260 characters, account cache isolation. Fixtures preserved: "+normal);
    }
    static async Task EngineGuardChecks() {
        bool finished=false;
        using(var engine=new Engine("unused",(route,body)=> {
            if(route=="config/create") return Task.FromResult(J.Obj("jobid",42));
            if(route=="job/status") return Task.FromResult(J.Obj("finished",finished));
            throw new IOException("simulated stop failure");
        })) {
            bool failed=false; try { await engine.Job("config/create",J.Obj("name","test"),()=>true); } catch(InvalidOperationException) { failed=true; }
            Check(failed && engine.ConfigJobUnconfirmed,"Unconfirmed job must remain tracked");
            failed=false; try { await engine.EnsureConfigIdle(); } catch(Exception) { failed=true; }
            Check(failed,"Live authentication must block config mutations");
            finished=true; await engine.EnsureConfigIdle(); Check(!engine.ConfigJobUnconfirmed,"Terminal confirmation must release config guard");
        }
        using(var engine=new Engine("unused",(route,body)=> { throw new TaskCanceledException("simulated start timeout"); })) {
            bool failed=false; try { await engine.Job("config/create",J.Obj("name","test"),()=>true); } catch(TaskCanceledException) { failed=true; }
            Check(failed && engine.ConfigJobUnconfirmed,"Unknown start must preserve config guard");
            failed=false; try { await engine.EnsureConfigIdle(); } catch(Exception) { failed=true; }
            Check(failed,"Unknown job ID must not allow config mutations");
        }
    }
    static async Task JobChecks() {
        int reads=0,stops=0;
        Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> race=(route,body)=> {
            if(route=="job/stop") { stops++; throw new IOException("simulated stop race"); }
            reads++; return Task.FromResult(J.Obj("finished",reads>1,"success",true));
        };
        bool confirmed=false; try { await RcJob.Wait(race,1,()=>true,1); } catch(ConfirmedJobCancellation) { confirmed=true; }
        Check(confirmed && reads==2 && stops==1,"Stop race requires terminal confirmation");
        reads=0;
        Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> running=(route,body)=> {
            if(route=="job/stop") throw new IOException("simulated stop failure");
            reads++; return Task.FromResult(J.Obj("finished",false));
        };
        bool blocked=false; try { await RcJob.Wait(running,1,()=>true,1); } catch(InvalidOperationException) { blocked=true; }
        Check(blocked && reads==2,"Live job stop failure must not become confirmed cancellation");
        Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> timeout=(route,body)=> { throw new TaskCanceledException("simulated HTTP timeout"); };
        blocked=false; try { await RcJob.Wait(timeout,1,()=>true,1); } catch(ConfirmedJobCancellation) { throw new Exception("HTTP timeout mistaken for confirmed cancellation"); } catch(TaskCanceledException) { blocked=true; }
        Check(blocked,"Transport timeout must remain unconfirmed");
        int polls=0;
        Func<string,Dictionary<string,object>,Task<Dictionary<string,object>>> stopped=(route,body)=> {
            if(route=="job/stop") return Task.FromResult(J.Obj());
            return Task.FromResult(J.Obj("finished",++polls>2,"success",false));
        };
        confirmed=false; try { await RcJob.Wait(stopped,1,()=>true,1); } catch(ConfirmedJobCancellation) { confirmed=true; }
        Check(confirmed && polls==3,"Stop acknowledgement must wait for terminal state");
    }
}
}
