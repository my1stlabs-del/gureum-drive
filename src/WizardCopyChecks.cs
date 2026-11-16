using System;
namespace CloudDock {
static class WizardCopyChecks {
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Run() {
        var shared=J.Obj("Name","config_shared_client_id","Type","bool","Default",false);
        Check(ConfigText.Title(shared,"drive")=="공용 앱 ID 사용 여부","Shared ID title");
        Check(ConfigText.Choice(shared,"false","No","drive").Contains("직접 발급"),"Shared ID action label");
        Check(J.Get(shared,"Default").Equals(false),"Default value must remain unchanged");
        string help="rclone's shared Google Drive client_id is being retired and will stop working during 2026.\nCreate your own to avoid interruption: https://rclone.org/drive/#making-your-own-client-id\n\nContinue using the shared client_id anyway?";
        string translated=HelpTranslations.Find(help);
        Check(translated!=null&&translated.Contains("Google Drive")&&translated.Contains("https://rclone.org/drive/#making-your-own-client-id"),"Dynamic template must preserve service and URL");
        Check(ConfigText.Title(J.Obj("Name","config_type"),"jottacloud").Contains("로그인 방식"),"Contextual auth title");
        Check(!ConfigText.Guidance(J.Obj("Name","config_type"),"jottacloud").Contains("OneDrive"),"Wrong service guidance");
        Check(ConfigText.Guidance(J.Obj("Name","password"),"crypt").Contains("복호화"),"Encryption password guidance");
        Check(WizardGuidance.IsSecret(J.Obj("Name","client_secret","IsPassword",false)),"Dynamic secret masking");
        Check(!WizardGuidance.InputHint(J.Obj("Required",true),false).Contains("빈칸"),"Required input hint");
        Check(!ConfigText.Guidance(J.Obj("Name","env_auth"),"drive").Contains("AWS"),"IAM service context");
        string command="rclone authorize \"drive\" \"OPAQUE-SYNTHETIC-VALUE\"";
        string manual=HelpTranslations.Find("For this to work, you will need rclone available on a machine that has\na web browser available.\n\n"+command+"\nThen paste the result.");
        Check(manual!=null && manual.Contains(command),"Manual authentication command must remain exact");
    }
}
}
