using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CloudDock {
static class LocalizationChecks {
    public static void Run(string providerFile) {
        string original=CultureInfo.CurrentUICulture.Name;
        var providers=J.Items(J.Serializer().DeserializeObject(File.ReadAllText(providerFile))).Select(J.Map).ToList();
        var options=providers.SelectMany(p=>J.Items(J.Get(p,"Options"))).Select(J.Map).ToList();
        L.Initialize("ko-KR");
        ReliabilityChecks.Run();
        // A mounted but idle VFS normally has inUse=1. Cached clean files also
        // remain after use and must not be mistaken for pending uploads.
        var idle=J.Obj("inUse",1,"diskCache",J.Obj("files",17,"uploadsQueued",0,"uploadsInProgress",0,"erroredFiles",0));
        if(PendingState.Blocks(idle)) throw new Exception("Idle mounted VFS must allow shutdown");
        foreach(string pendingKey in new[]{"uploadsQueued","uploadsInProgress","erroredFiles"}) {
            var pending=J.Map(J.Get(idle,"diskCache")); pending[pendingKey]=1;
            if(!PendingState.Blocks(idle)) throw new Exception("Pending state must block shutdown: "+pendingKey);
            pending[pendingKey]=0;
        }
        string probeRoot=Path.Combine(Path.GetTempPath(),"CloudDock-shutdown-check-"+Guid.NewGuid().ToString("N"));
        string probeCache=Path.Combine(probeRoot,"cache"),probeMeta=Path.Combine(probeRoot,"meta");
        Directory.CreateDirectory(probeCache); Directory.CreateDirectory(probeMeta);
        var probe=J.Obj("path",probeCache,"pathMeta",probeMeta);
        if(PendingState.CacheBusy(probe)) throw new Exception("Empty cache must allow shutdown");
        string sample=Path.Combine(probeCache,"sample.bin"); File.WriteAllText(sample,"test");
        if(PendingState.CacheBusy(probe)) throw new Exception("Clean cache must allow shutdown");
        using(var held=new FileStream(sample,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            if(PendingState.CacheBusy(probe)) throw new Exception("Clean open cache must not be exclusively probed");
        if(PendingState.CacheBusy(probe)) throw new Exception("Closed cache handle must allow shutdown");
        File.WriteAllText(Path.Combine(probeMeta,"sample.bin"),"{\"Dirty\":true}");
        if(!PendingState.CacheBusy(probe)) throw new Exception("Dirty cache must block shutdown");
        File.WriteAllText(Path.Combine(probeMeta,"sample.bin"),"{broken");
        if(!PendingState.CacheBusy(probe)) throw new Exception("Incomplete metadata must block shutdown");
        using(var held=new FileStream(Path.Combine(probeMeta,"sample.bin"),FileMode.Open,FileAccess.Write,FileShare.None))
            if(!PendingState.CacheBusy(probe)) throw new Exception("Unreadable metadata must block shutdown");
        // Preserve test fixtures: cleanup is deliberately not part of this check.
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shutdown-test.txt"),"PASS: idle mounted VFS, queued/active/errored uploads, empty/clean/open/closed/dirty/incomplete/locked metadata. Fixtures preserved: "+probeRoot);
        if(!L.Korean||L.T("내 드라이브")!="내 드라이브") throw new Exception("Korean locale failed");
        var missing=options.Where(o=>!J.Bool(o,"Advanced")&&!ConfigText.HasTitle(J.Str(o,"Name"))).Select(o=>J.Str(o,"Name")).Distinct().ToList();
        if(missing.Count>0) throw new Exception("Missing basic option titles: "+String.Join(",",missing));
        foreach(var option in options) {
            if(!String.IsNullOrWhiteSpace(J.Str(option,"Help"))&&HelpTranslations.Find(J.Str(option,"Help"))==null) throw new Exception("Missing actual source translation: "+J.Str(option,"Name"));
            if(J.Str(option,"Help").Length>0&&!Regex.IsMatch(ConfigText.Help(option),"[가-힣]")) throw new Exception("Missing Korean guidance: "+J.Str(option,"Name"));
            foreach(var ex in J.Items(J.Get(option,"Examples")).Select(J.Map)) if(String.IsNullOrWhiteSpace(ConfigText.Choice(option,J.Str(ex,"Value"),J.Str(ex,"Help")))) throw new Exception("Empty choice label");
        }
        WizardCopyChecks.Run();
        var boolean=J.Obj("Name","config_is_local","Type","bool");
        if(!ConfigText.Choice(boolean,"true","Yes").StartsWith("예 —")||!ConfigText.Choice(boolean,"false","No").StartsWith("아니오 —")) throw new Exception("Boolean labels failed");
        var drive=J.Obj("Name","config_driveid","Type","string");
        string rawId="opaque-drive-id-12345678";
        string driveName=ConfigText.Choice(drive,rawId,"My documents (personal)");
        if(!driveName.Contains("My documents")||!driveName.Contains("개인용 OneDrive")||driveName.Contains(rawId)) throw new Exception("Drive name/type must replace opaque IDs");
        if(!ConfigText.DriveLabel(rawId,"Team files (documentLibrary)").Contains("SharePoint")) throw new Exception("Document library type missing");
        if(!ConfigText.DriveGuide("").Contains("정보가 없습니다")) throw new Exception("Unknown drive must not be guessed");
        if(!ConfigText.DriveLabel(rawId,rawId).Contains("이름 정보가 없는")) throw new Exception("Opaque help must not be displayed as a name");
        if(!ConfigText.Guidance(drive).Contains("Windows")) throw new Exception("Drive-letter explanation missing");
        var failure=ErrorText.From("failed to mount FUSE fs: mount stopped before calling Init: mount failed: cgofuse: cannot find winfsp\nHint: Install WinFsp from https://winfsp.dev/rel/");
        if(!failure.WinFsp||!failure.Translation.Contains("초기화")||!failure.Guidance.Contains("설치")) throw new Exception("WinFsp translation and remedy missing");
        if(!ErrorText.Translate("permission denied: D:\\문서\\file.txt").Contains("접근 권한 없음")) throw new Exception("Mixed-language paths must not suppress error translation");
        L.Initialize("en-US");
        if(L.Korean||L.T("내 드라이브")!="My drives"||!ConfigText.Choice(boolean,"true","Yes").StartsWith("Yes —")) throw new Exception("English locale failed");
        if(!ConfigText.Choice(drive,rawId,"Documents (business)").Contains("Work or school OneDrive")) throw new Exception("English drive labels failed");
        foreach(var option in options.Where(o=>J.Str(o,"Name")!="config_driveid")) if(ConfigText.Help(option)!=J.Str(option,"Help")) throw new Exception("English help must preserve original");
        L.Initialize("fr-FR"); if(L.T("취소")!="Cancel") throw new Exception("Unsupported locale fallback failed");
        var oldCulture=CultureInfo.CurrentCulture;
        try { System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR"); if(J.Str(J.Obj("number",1.5),"number")!="1.5") throw new Exception("Locale changed a protocol number"); }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture=oldCulture; }
        L.Initialize(original);
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"localization-test.txt"),"PASS: ko-KR, en-US, unsupported locale fallback, boolean labels, "+providers.Count+" providers, "+options.Count+" actual source help translations, "+options.Where(o=>!J.Bool(o,"Advanced")).Select(o=>J.Str(o,"Name")).Distinct().Count()+" basic option titles, drive labels, WinFsp error translation and remedy. English help preserved verbatim; original remains expandable.");
    }
}
}
