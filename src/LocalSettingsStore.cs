using System;
using System.Collections.Generic;
using System.IO;

namespace CloudDock {
// Reads for display may fall back; writes must re-read successfully before changing anything.
sealed class LocalSettingsStore {
    readonly Dictionary<string,Dictionary<string,object>> good=new Dictionary<string,Dictionary<string,object>>();
    readonly Dictionary<string,string> backedUp=new Dictionary<string,string>();
    public string Warning { get; private set; }
    public Dictionary<string,object> Read(string file,bool forWrite=false) {
        Warning="";
        try {
            string text=File.ReadAllText(file);
            Dictionary<string,object> value;
            try {
                value=J.Serializer().DeserializeObject(text) as Dictionary<string,object>;
                if(value==null) throw new ArgumentException("Invalid settings object");
            } catch(ArgumentException) {
                string previous;
                if(!backedUp.TryGetValue(file,out previous) || previous!=text) {
                    // Back up the text we parsed; don't copy a possibly replaced file.
                    string backup=file+".corrupt-"+Guid.NewGuid().ToString("N");
                    File.WriteAllText(backup,text); backedUp[file]=text;
                }
                Warning=L.Pair("손상된 설정을 백업했습니다. 기본값을 사용합니다.","Damaged settings backed up. Using defaults.");
                value=J.Obj();
            }
            good[file]=value; return new Dictionary<string,object>(value);
        } catch(FileNotFoundException) {
            if(!good.ContainsKey(file)) return J.Obj();
            return Unavailable(file,forWrite);
        } catch(DirectoryNotFoundException) {
            if(!good.ContainsKey(file)) return J.Obj();
            return Unavailable(file,forWrite);
        } catch(IOException) { return Unavailable(file,forWrite); }
        catch(UnauthorizedAccessException) { return Unavailable(file,forWrite); }
    }
    Dictionary<string,object> Unavailable(string file,bool forWrite) {
        Warning=L.Pair("설정을 읽지 못했습니다. 기존 설정 보호를 위해 저장을 중단합니다.","Settings could not be read. Saving is blocked to protect existing settings.");
        if(forWrite) throw new IOException(Warning);
        Dictionary<string,object> previous;
        return good.TryGetValue(file,out previous)?new Dictionary<string,object>(previous):J.Obj();
    }
}
static class StartupDiagnostics {
    // Allow-list categories only. Never retain or display arbitrary backend stderr.
    public static string Classify(string line) {
        if(line==null) return "";
        if(line.IndexOf("address already in use",StringComparison.OrdinalIgnoreCase)>=0 || line.IndexOf("Only one usage of each socket address",StringComparison.OrdinalIgnoreCase)>=0) return "RC_PORT_IN_USE";
        if(line.IndexOf("permission denied",StringComparison.OrdinalIgnoreCase)>=0 || line.IndexOf("access is denied",StringComparison.OrdinalIgnoreCase)>=0) return "ACCESS_DENIED";
        return "ENGINE_START_FAILED";
    }
}
}
