using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CloudDock {
static class PendingState {
    public static bool Blocks(Dictionary<string,object> stats) {
        // VFS inUse counts references to the mounted VFS, not open files.
        var disk=J.Map(J.Get(stats,"diskCache"));
        return Count(disk,"uploadsQueued")>0 || Count(disk,"uploadsInProgress")>0 || Count(disk,"erroredFiles")>0;
    }
    static long Count(Dictionary<string,object> values,string key) { return Convert.ToInt64(J.Get(values,key)??0); }
    static string ReadShared(string file) {
        using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
        using(var reader=new StreamReader(stream)) return reader.ReadToEnd();
    }
    public static bool RemoteCacheBusy(string root,string remote) {
        // CloudDock-generated names need no OS encoding. Unknown names require confirmation.
        if(!Regex.IsMatch(remote,@"\A[A-Za-z0-9_-]+\z")) return true;
        try {
            foreach(string directory in Directory.EnumerateDirectories(root)) {
                string name=Path.GetFileName(directory);
                if(Regex.IsMatch(name,@"\A"+Regex.Escape(remote)+@"(?:\{[A-Za-z0-9_-]{5,22}\})?\z",RegexOptions.IgnoreCase))
                    if(CacheBusy(J.Obj("pathMeta",directory))) return true;
            }
            return false;
        } catch(DirectoryNotFoundException) { return false; }
        catch(IOException) { return true; } catch(UnauthorizedAccessException) { return true; }
    }
    public static bool CacheBusy(Dictionary<string,object> disk) {
        string meta=J.Str(disk,"pathMeta");
        if(String.IsNullOrWhiteSpace(meta)) throw new IOException(L.Pair("미전송 변경 내용을 확인할 캐시 정보가 없습니다. 연결을 유지합니다.","Cannot verify unsent changes. Keeping the drive connected."));
        try { foreach(string file in Directory.EnumerateFiles(meta,"*",SearchOption.AllDirectories)) {
            try { var info=J.Serializer().DeserializeObject(ReadShared(file)) as Dictionary<string,object>; if(info==null || !info.ContainsKey("Dirty") || !(info["Dirty"] is bool) || (bool)info["Dirty"]) return true; }
            catch(FileNotFoundException) {} catch(DirectoryNotFoundException) {}
            catch(IOException) { return true; } catch(UnauthorizedAccessException) { return true; } catch(ArgumentException) { return true; }
        }
        } catch(DirectoryNotFoundException) { return false; }
        catch(ArgumentException) { throw new IOException(L.Pair("캐시 경로를 읽지 못해 연결 해제를 중단했습니다. GureumDrive.exe와 같은 폴더의 GureumDrive.exe.config가 최신 파일인지 확인하세요. 캐시를 삭제하지 마세요.","Disconnect was stopped because the cache path could not be read. Check that GureumDrive.exe.config beside GureumDrive.exe is up to date. Do not delete the cache.")); }
        catch(IOException) { return true; } catch(UnauthorizedAccessException) { return true; }
        return false;
    }
}
}
