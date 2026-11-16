using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CloudDock {
static class HelpTranslations {
    static Dictionary<string,string> translations;
    static string Normalize(string s) { return Regex.Replace(s??"",@"\s+"," ").Trim(); }
    static void Load() {
        if(translations!=null) return;
        translations=new Dictionary<string,string>(StringComparer.Ordinal);
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CloudDock.RcloneHelp.ko.json"))
        using(var reader=new StreamReader(stream)) foreach(var pair in J.Map(J.Serializer().DeserializeObject(reader.ReadToEnd()))) translations[Normalize(pair.Key)]=Convert.ToString(pair.Value);
    }
    public static string Find(string original) {
        // Local template translation keeps account-specific names and URLs on
        // the user's PC; these values never go to the build-time translator.
        if(L.Korean && (original??"").StartsWith("Drive OK?",StringComparison.Ordinal)) {
            var found=Regex.Match(original,"Found drive (?<name>.+?) of type \"(?<type>[^\"]+)\"\\s*URL: (?<url>\\S+)",RegexOptions.Singleline);
            if(found.Success) return "이 드라이브가 맞습니까?\n\n찾은 드라이브: "+found.Groups["name"].Value+"\n종류: "+ConfigText.DriveType(found.Groups["type"].Value)+"\nURL: "+found.Groups["url"].Value;
        }
        string dynamic=WizardText.Dynamic(original); if(L.Korean && dynamic!=null) return dynamic;
        if(L.Korean && (original??"").StartsWith("For this to work, you will need rclone available",StringComparison.Ordinal)) {
            var command=Regex.Match(original,@"(?m)^\s*rclone authorize [^\r\n]+");
            if(command.Success) return "브라우저가 있는 PC에 rclone이 설치돼 있어야 합니다.\n같은 rclone 버전 사용을 권장합니다. 그 PC에서 다음 명령을 실행한 뒤 반환된 결과를 여기에 붙여넣으세요.\n\n"+command.Value.Trim()+"\n\n추가 안내: https://rclone.org/remote_setup/";
        }
        Load(); string value; return translations.TryGetValue(Normalize(original),out value)?value:null;
    }
    public static string Text(string original) { if(String.IsNullOrWhiteSpace(original)) return ""; return L.Korean?Find(original)??L.Pair("이 버전에는 이 새 도움말의 번역이 없습니다. 원문 보기를 눌러 내용을 확인하세요.",original):original; }
}
}
