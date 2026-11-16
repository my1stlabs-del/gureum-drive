using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CloudDock {
static class ConfigText {
    // This catalog describes configuration, never user-entered values or credentials.
    static readonly Dictionary<string,string> Names=new Dictionary<string,string> {
        {"client_id","앱 ID"},{"client_secret","앱 비밀키"},{"config_is_local","브라우저로 로그인"},{"config_advanced","고급 설정 사용 여부"},{"scope","파일 접근 권한"},
        {"config_type","연결할 드라이브 종류"},{"config_driveid","사용할 드라이브"},{"config_driveok","선택한 드라이브 확인"},{"config_onedrive","OneDrive 계정 종류"},{"config_team_drive","공유 드라이브 사용 여부"},{"config_token","브라우저 인증 결과"},{"config_verification_code","인증 코드"},
        {"host","서버 주소"},{"port","포트 번호"},{"user","사용자 이름"},{"username","사용자 이름"},{"pass","비밀번호"},{"password","비밀번호"},{"url","서버 URL"},{"endpoint","서비스 접속 주소"},{"provider","서비스 제공 업체"},{"vendor","서버 종류"},{"region","저장소 리전"},{"location_constraint","버킷 생성 리전"},
        {"access_key_id","액세스 키 ID"},{"secret_access_key","시크릿 액세스 키"},{"env_auth","환경에 저장된 인증 정보 사용"},{"session_token","세션 토큰"},{"api_key","API 키"},{"api_secret","API 비밀키"},{"api_url","API 서버 주소"},{"account","계정"},{"email","이메일 주소"},{"key","인증 키"},{"token","인증 토큰"},{"access_token","액세스 토큰"},{"refresh_token","갱신 토큰"},
        {"auth_url","로그인 서버 주소"},{"token_url","토큰 서버 주소"},{"client_credentials","앱 자격 증명으로 인증"},{"tenant","조직 테넌트 ID"},{"tenant_id","조직 테넌트 ID"},{"drive_id","드라이브 ID"},{"drive_type","드라이브 종류"},{"root_folder_id","최상위 폴더 ID"},{"team_drive","공유 드라이브 ID"},{"service_account_file","서비스 계정 인증 파일"},{"service_account_credentials","서비스 계정 인증 정보"},{"impersonate","대신 접근할 사용자"},{"remote","연결할 원격 경로"},{"upstreams","결합할 연결 목록"},
        {"key_file","개인키 파일 경로"},{"key_pem","PEM 개인키"},{"key_file_pass","개인키 파일 비밀번호"},{"key_use_agent","SSH 에이전트 사용"},{"known_hosts_file","신뢰할 서버 목록 파일"},{"private_key","개인키"},{"pubkey","공개키"},{"2fa","2단계 인증 코드"},{"otp","일회용 인증 코드"},
        {"encoding","파일 이름 인코딩"},{"description","연결 설명"},{"chunk_size","전송 조각 크기"},{"upload_cutoff","분할 업로드 기준 크기"},{"upload_concurrency","동시 업로드 수"},{"list_chunk","목록 조회 단위"},{"hard_delete","휴지통을 거치지 않고 삭제"},{"trashed_only","휴지통 파일만 표시"},{"readonly","읽기 전용"},{"read_only","읽기 전용"},{"timeout","응답 대기 시간"},{"connect_timeout","연결 대기 시간"},{"idle_timeout","유휴 연결 대기 시간"},{"disable_checksum","체크섬 검사 끄기"},{"decompress","압축 자동 해제"},{"no_check_bucket","버킷 확인 생략"},{"directory_markers","빈 폴더 표시 파일 사용"},
        {"acl","파일 접근 권한 정책"},{"bucket_acl","버킷 접근 권한 정책"},{"storage_class","저장소 등급"},{"server_side_encryption","서버 측 암호화"},{"sse_kms_key_id","KMS 암호화 키 ID"},{"sse_customer_key","사용자 암호화 키"},{"connection_string","연결 문자열"},{"sas_url","SAS 접속 URL"},{"azureblob_account","Azure 저장소 계정"},{"unix_socket","Unix 소켓 경로"},{"headers","추가 HTTP 헤더"},{"user_agent","사용자 에이전트"},{"versions","이전 파일 버전 표시"},{"version_at","표시할 버전 시각"},{"filename_encryption","파일 이름 암호화 방식"},{"directory_name_encryption","폴더 이름 암호화"},{"password2","추가 암호화 비밀번호"},{"shared_with_me","공유받은 파일 표시"},{"upload_compression","업로드 압축"},{"disable_instance_discovery","인증 서버 자동 검색 끄기"}
    };
    static ConfigText() {
        var extra=new Dictionary<string,string> {
            {"access_grant","접근 권한 인증 정보"},{"action_policy","파일 작업 정책"},{"alternate_export","대체 내보내기 방식"},{"anonymous","익명 접속"},{"api_password","API 비밀번호"},{"app_id","앱 ID"},{"app_token","앱 토큰"},{"apple_id","Apple 계정"},
            {"application_credential_id","앱 자격 증명 ID"},{"application_credential_name","앱 자격 증명 이름"},{"application_credential_secret","앱 자격 증명 비밀키"},{"auth","인증 설정"},{"auth_token","인증 토큰"},{"auth_version","인증 API 버전"},{"bearer_token","Bearer 인증 토큰"},{"box_config_file","Box 인증 설정 파일"},{"box_sub_type","Box 계정 종류"},
            {"bucket_object_lock_enabled","버킷 객체 잠금 사용"},{"bucket_policy_only","버킷 정책만 사용"},{"cache_time","캐시 유지 시간"},{"chunk_total_size","파일 조각의 전체 크기"},{"client_certificate_password","클라이언트 인증서 비밀번호"},{"client_certificate_path","클라이언트 인증서 경로"},{"cloud_name","클라우드 이름"},{"compartment","리소스 구획"},{"config_credentials","인증 정보 설정"},{"config_file","설정 파일 경로"},{"config_profile","설정 프로필"},{"cookies","인증 쿠키"},{"create_policy","파일 생성 정책"},
            {"disable_hashcheck","해시 확인 끄기"},{"doi","데이터 세트 DOI"},{"domain","도메인"},{"endpoint_type","접속 주소 종류"},{"explicit_tls","명시적 TLS 연결"},{"hash_type","해시 방식"},{"hashes","지원할 해시 목록"},{"ibm_api_key","IBM API 키"},{"ibm_resource_instance_id","IBM 리소스 인스턴스 ID"},{"info_age","파일 정보 유지 시간"},{"item_derive","자동 생성 파일 사용"},{"level","압축 수준"},{"library","라이브러리 이름"},{"library_key","라이브러리 인증 키"},{"location","저장 위치"},{"max_age","최대 보관 시간"},{"mode","동작 방식"},
            {"namenode","HDFS 네임노드 주소"},{"namespace","네임스페이스"},{"no_escape","경로 이스케이프 끄기"},{"nounc","UNC 경로 변환 끄기"},{"object_acl","객체 접근 권한 정책"},{"otp_secret_key","일회용 인증 비밀키"},{"passphrase","암호 구문"},{"permanent_token","영구 인증 토큰"},{"plex_password","Plex 비밀번호"},{"plex_url","Plex 서버 URL"},{"plex_username","Plex 사용자 이름"},{"private_access_key","비공개 액세스 키"},{"project_number","프로젝트 번호"},{"pubkey_file","공개키 파일 경로"},{"public_key","공개키"},
            {"satellite_address","위성 서버 주소"},{"scope_access","접근 권한 범위"},{"search_policy","파일 검색 정책"},{"secret","인증 비밀키"},{"service","서비스 종류"},{"share_name","공유 이름"},{"site","사이트 주소"},{"speedup_enable","전송 가속 사용"},{"spn","서비스 사용자 이름"},{"ssh","SSH 명령"},{"storage_policy","저장소 정책"},{"storage_url","저장소 URL"},{"tenant_domain","조직 테넌트 도메인"},{"tls","TLS 보안 연결"},{"trust_token","신뢰 인증 토큰"},{"upload_prefix","업로드 경로 접두사"},{"upload_preset","업로드 사전 설정"},{"use_insecure_cipher","구형 암호화 방식 허용"},{"use_kerberos","Kerberos 인증 사용"},{"user_id","사용자 ID"},{"user_project","사용자 프로젝트"},{"web_token","웹 인증 토큰"},{"workspace_id","작업 공간 ID"},{"zone","저장소 영역"}
        };
        foreach(var item in extra) Names[item.Key]=item.Value;
        Names["config_drive_ok"]="선택한 드라이브 확인";
        Names["config_driveid_fixed"]="드라이브 ID 직접 입력";
        Names["config_siteid"]="SharePoint 사이트 ID";
        Names["config_site_url"]="SharePoint 사이트 주소";
        Names["config_sharepoint_url"]="SharePoint 서버 기준 상대 경로";
        Names["config_search_term"]="사이트 검색어";
        Names["config_site"]="사용할 SharePoint 사이트";
    }
    static readonly Dictionary<string,string> HelpKo=new Dictionary<string,string> {
        {"config_is_local","이 PC의 웹 브라우저에서 로그인할까요?\n‘예’를 선택하면 서비스의 로그인 페이지가 열립니다.\n다른 PC에서 인증해야 하는 경우에만 ‘아니오’를 선택하세요."},
        {"config_advanced","일반적인 연결에는 고급 설정이 필요하지 않습니다.\n기본 설정을 사용하려면 ‘아니오’를 선택하세요."},
        {"client_id","서비스별로 앱 ID의 입력 조건이 다릅니다. 위 원문 번역의 조건을 확인하세요. 직접 발급한 앱 ID를 요구하거나 권장하는 서비스도 있습니다."},
        {"client_secret","직접 등록한 OAuth 앱의 비밀키입니다. 위 원문 번역에 따라 앱 ID에 대응하는 비밀키를 입력하거나 비워 두세요."},
        {"config_type","사용할 OneDrive 또는 SharePoint 연결 방식을 선택하세요.\n개인 계정과 일반 회사 계정은 OneDrive 항목으로 시작하세요."},
        {"config_driveid","이 계정에서 연결할 드라이브를 선택하세요. 표시된 이름과 계정 종류를 확인해 주세요."},
        {"config_driveok","지금 선택한 드라이브가 맞는지 확인하세요. 맞으면 ‘예’, 다시 선택하려면 ‘아니오’를 선택하세요."},
        {"config_token","다른 PC 또는 수동 인증으로 발급한 인증 결과를 입력하세요. 아래 ‘원문 도움말’에서 서비스별 인증 절차를 확인할 수 있습니다."},
        {"config_verification_code","브라우저나 서비스에서 받은 인증 코드를 입력하세요."},
        {"host","접속할 서버의 호스트 이름 또는 IP 주소를 입력하세요. 예: files.example.com"},
        {"port","서버의 접속 포트 번호입니다. 별도로 변경하지 않았다면 기본값을 사용하세요."},
        {"user","이 서비스 또는 서버에 로그인할 사용자 이름을 입력하세요."},
        {"username","이 서비스 또는 서버에 로그인할 사용자 이름을 입력하세요."},
        {"pass","서비스 또는 서버에 로그인할 비밀번호입니다. 입력 내용은 화면에 표시되지 않습니다."},
        {"password","서비스 또는 서버에 로그인할 비밀번호입니다. 입력 내용은 화면에 표시되지 않습니다."},
        {"url","서비스에서 제공한 전체 서버 URL을 입력하세요. WebDAV는 https://로 시작하는 WebDAV 접속 주소를 사용합니다."},
        {"endpoint","서비스에서 제공한 API 또는 S3 접속 주소입니다. 사용하는 업체의 안내에 맞는 주소를 입력하세요."},
        {"provider","사용하는 저장소 업체를 선택하세요. 목록에 없는 S3 호환 서버는 Other를 선택하세요."},
        {"vendor","사용하는 서버의 종류를 선택하세요. 목록에 없으면 기타 서버에 해당하는 항목을 선택하세요."},
        {"region","서비스에서 지정한 저장소 리전을 선택하거나 입력하세요. 업체에서 지정한 리전 코드 값을 그대로 사용합니다."},
        {"env_auth","현재 PC의 환경 변수나 서버 역할에 저장된 인증 정보를 사용할까요?\n액세스 키와 시크릿 키를 직접 입력하려면 ‘아니오’를 선택하세요."},
        {"access_key_id","저장소 서비스에서 발급받은 액세스 키 ID를 입력하세요."},
        {"secret_access_key","액세스 키와 함께 발급받은 시크릿 키를 입력하세요. 외부에 공유하지 마세요."},
        {"scope","파일에 접근할 범위를 선택하세요. 파일을 수정·저장하려면 쓰기를 허용하는 권한이 필요합니다."},
        {"root_folder_id","특정 폴더만 연결하려면 서비스에서 발급한 폴더 ID를 입력하세요. 폴더 이름이나 경로와는 다릅니다. 기본 위치를 쓰려면 비워 두세요."},
        {"key_file","SSH/SFTP 로그인에 사용할 개인키 파일의 전체 경로를 입력하세요. 비밀번호로 로그인한다면 기본값으로 진행하세요."},
        {"key_file_pass","암호화된 개인키 파일을 열 때 사용하는 비밀번호입니다. 파일이 암호화되지 않았다면 비워 두세요."},
        {"remote","연결할 원격 저장소와 경로를 입력하세요. 예: mydrive:Documents. 연결 종류별 자세한 형식은 원문 도움말을 확인하세요."},
        {"hard_delete","이 옵션을 켜면 파일 삭제 시 휴지통을 거치지 않을 수 있습니다. 복원 가능한 삭제를 원하면 기본값을 유지하세요."}
    };
    static readonly Dictionary<string,string> Phrases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
        {"Yes","예"},{"No","아니오"},{"Other","기타"},{"None","없음"},{"Default","기본값"},{"Personal","개인용"},{"Business","회사 또는 학교용"},
        {"OneDrive Personal or Business","OneDrive 개인용 또는 회사·학교용"},{"Root Sharepoint site","SharePoint 최상위 사이트"},{"Sharepoint site name or URL","SharePoint 사이트 이름 또는 URL"},{"Search for a Sharepoint site","SharePoint 사이트 검색"},{"Type in driveID (advanced)","드라이브 ID 직접 입력 (고급)"},{"Type in SiteID (advanced)","사이트 ID 직접 입력 (고급)"},
        {"Full access all files, excluding Application Data Folder.","전체 파일 읽기·쓰기 (앱 데이터 폴더 제외)"},{"Read-only access to file metadata and file contents.","파일 정보와 내용 읽기 전용"},{"Access to files created by rclone only.","rclone으로 생성한 파일만 접근"},{"Allows read and write access to the Application Data folder.","앱 데이터 폴더 읽기·쓰기"},{"Allows read-only access to file metadata but","파일 정보 읽기 전용"},
        {"Amazon Web Services (AWS) S3","Amazon Web Services (AWS) S3"},{"Any other S3 compatible provider","기타 S3 호환 서비스"},{"Use default endpoint - leave blank.","기본 접속 주소 사용 (비워 두기)"},
        {"Get AWS credentials from runtime (environment variables or EC2/ECS meta data if no env vars).","현재 환경에서 AWS 인증 정보 가져오기"},{"Enter AWS credentials in the next step.","다음 단계에서 AWS 인증 정보 입력"}
    };
    public static string Title(Dictionary<string,object> option,string backend="") {
        string key=J.Str(option,"Name"), value;
        string contextual=WizardGuidance.ContextTitle(backend,key); if(contextual!=null) return contextual;
        if(L.Korean && WizardText.HasTitle(key)) return WizardText.Title(key);
        if(key=="config_driveid") return L.Pair("연결할 클라우드 저장소 선택","Choose the cloud storage to connect");
        if(!L.Korean) return Regex.Replace(key.Replace('_',' '),"^.",m=>m.Value.ToUpperInvariant());
        return Names.TryGetValue(key,out value)?value:L.Pair("추가 설정","Additional setting")+" · "+key;
    }
    public static bool HasTitle(string key) { return Names.ContainsKey(key)||WizardText.HasTitle(key); }
    public static string Help(Dictionary<string,object> option,string backend="") { return HelpTranslations.Text(J.Str(option,"Help")); }
    public static string Guidance(Dictionary<string,object> option,string backend="") {
        string contextual=WizardGuidance.Text(backend,option); if(contextual!=null) return contextual;
        if(J.Str(option,"Name")=="config_driveid") return L.Pair(
            "Microsoft 계정에서 접근 가능한 클라우드 저장소 중 하나를 고르는 단계입니다.\nZ: 같은 Windows 드라이브 문자는 인증을 마친 뒤 다음 단계에서 정합니다.\n\n• 개인 파일을 연결하려면 ‘개인용’ OneDrive를 선택하세요.\n• 업무 파일은 회사·학교용 OneDrive, 팀 문서는 SharePoint 문서 라이브러리를 선택하세요.\n• 이름을 구분하기 어렵다면 아래 선택한 저장소 안내와 원문 도움말을 확인하세요.\n드라이브 ID는 저장소를 구분하는 내부 번호이며 직접 외우거나 수정할 필요가 없습니다.",
            "Choose one cloud storage location available to your Microsoft account.\nYou will choose a Windows drive letter, such as Z:, after authentication.\n\n• For your personal files, choose your personal OneDrive.\n• For work files, choose your work or school OneDrive; for team documents, choose a SharePoint document library.\n• If the names are unclear, check the selection details and original help below.\nA drive ID is an internal identifier. You do not need to memorize or edit it.");
        if(!L.Korean) return "";
        string key=J.Str(option,"Name"),value;
        if(key=="config_drive_ok") return L.Pair("위에 표시된 드라이브 이름·종류·URL이 원하는 저장소와 일치하면 ‘예’를 선택하세요. 다른 저장소라면 ‘아니오’를 선택해 다시 고를 수 있습니다.","Choose Yes if the drive name, type and URL above identify the storage you want. Choose No to select another location.");
        if(!HelpKo.TryGetValue(key,out value)) return "";
        string text=value;
        text+="\n"+(J.Bool(option,"Required")?"필수 항목입니다.":"선택 항목입니다. 별도 설정이 없다면 기본값을 유지하세요.");
        if(WizardGuidance.IsSecret(option)) text+=" 입력한 비밀값은 가려집니다.";
        return text;
    }
    public static string Choice(Dictionary<string,object> option,string value,string help,string backend="") {
        string action=WizardGuidance.Choice(option,value); if(action!=null) return action;
        if(J.Str(option,"Name")=="config_driveid") return DriveLabel(value,help);
        if(String.IsNullOrWhiteSpace(value)&&String.IsNullOrWhiteSpace(help)) return L.Pair("기본값 사용 (비워 두기)","Use the default (leave blank)");
        if(J.Str(option,"Type")=="bool" || value.Equals("true",StringComparison.OrdinalIgnoreCase)||value.Equals("false",StringComparison.OrdinalIgnoreCase)) {
            if(value.Equals("true",StringComparison.OrdinalIgnoreCase)) return L.Pair("예","Yes");
            if(value.Equals("false",StringComparison.OrdinalIgnoreCase)) return L.Pair("아니오","No");
        }
        if(!L.Korean) return String.IsNullOrWhiteSpace(help)?value:help.Split('\n')[0]+" ("+value+")";
        if(J.Str(option,"Name")=="config_type" && (backend=="onedrive"||backend=="")) {
            switch(value.ToLowerInvariant()) {
                case "onedrive":return "OneDrive 개인용 또는 회사·학교용";
                case "sharepoint":return "SharePoint 최상위 사이트";
                case "url":return "SharePoint 사이트 이름 또는 URL 입력";
                case "search":return "SharePoint 사이트 검색";
                case "driveid":return "드라이브 ID 직접 입력 (고급)";
                case "siteid":return "사이트 ID 직접 입력 (고급)";
                case "path":return "사이트의 상대 경로로 연결 (고급)";
            }
        }
        string result; string line=help.Split('\n')[0].Trim();
        if(Phrases.TryGetValue(line,out result)) return result;
        if(Phrases.TryGetValue(value,out result)) return result;
        if(Regex.IsMatch(line,@"^https?://")) return line;
        string translated=HelpTranslations.Find(help);
        // Keep service values exact while also showing the translated label.
        if(J.Str(option,"Name")=="provider" || J.Str(option,"Name")=="vendor" || J.Str(option,"Name")=="region") {
            if(!String.IsNullOrWhiteSpace(translated)) return translated.Split('\n')[0]+(String.IsNullOrWhiteSpace(value)?"":" ("+value+")");
            return String.IsNullOrWhiteSpace(value)?"기본값 사용 (비워 두기)":value;
        }
        return !String.IsNullOrWhiteSpace(translated)?translated.Split('\n')[0]+" ("+value+")":L.Format("선택지 {0}","Option {0}",value);
    }
    public static string DriveLabel(string value,string help) {
        string label=(help??"").Split('\n')[0].Trim();
        if(String.IsNullOrWhiteSpace(label)||label==value) label=L.Pair("이름 정보가 없는 저장소","Storage with no name provided");
        // rclone supplies DriveName (DriveType). Translate only this final type;
        // never derive the storage type or recommendation from an opaque ID.
        label=Regex.Replace(label,@"\s*\((personal|business|documentLibrary)\)\s*$",m=>" — "+DriveType(m.Groups[1].Value),RegexOptions.IgnoreCase);
        string suffix=String.IsNullOrEmpty(value)?"":value.Substring(Math.Max(0,value.Length-8));
        return label+(suffix.Length==0?"":"  ·  ID …"+suffix);
    }
    internal static string DriveType(string type) {
        switch(type.ToLowerInvariant()) {
            case "personal":return L.Pair("개인용 OneDrive","Personal OneDrive");
            case "business":return L.Pair("회사·학교용 OneDrive","Work or school OneDrive");
            case "documentlibrary":return L.Pair("SharePoint 문서 라이브러리","SharePoint document library");
            default:return type;
        }
    }
    public static string DriveGuide(string help) {
        var match=Regex.Match(help??"",@"\((personal|business|documentLibrary)\)\s*$",RegexOptions.IgnoreCase);
        if(!match.Success) return L.Pair("저장소 종류를 확인할 정보가 없습니다. ID 모양이나 목록 순서로 판단하지 마세요. 원문 도움말에서 이름을 확인하거나 다음 단계의 확인 정보를 살펴보세요.","The storage type was not provided. Do not infer it from the ID or list order. Check the original name or the confirmation information in the next step.");
        switch(match.Groups[1].Value.ToLowerInvariant()) {
            case "personal":return L.Pair("개인 Microsoft 계정의 OneDrive입니다. 웹 OneDrive에 보관한 개인 문서·사진을 연결하려면 이 종류를 선택하세요.","This is a personal Microsoft account's OneDrive. Choose this type for personal documents and photos stored in OneDrive on the web.");
            case "business":return L.Pair("조직에서 제공하는 개인 작업용 OneDrive입니다. 회사·학교 계정의 내 파일을 연결할 때 선택하세요.","This is an individual work or school OneDrive provided by an organization. Choose it for your own files in that organization.");
            default:return L.Pair("SharePoint 사이트의 문서 라이브러리입니다. 팀·부서의 공유 문서를 연결할 때 선택하세요. 개인 OneDrive와는 별도 저장소입니다.","This is a document library in a SharePoint site. Choose it for team or department documents; it is a separate storage location from your personal OneDrive.");
        }
    }
    public static string Raw(Dictionary<string,object> option) {
        var text=J.Str(option,"Help");
        foreach(var example in J.Items(J.Get(option,"Examples"))) { var ex=J.Map(example); text+="\r\n\r\n"+J.Str(ex,"Value")+" — "+J.Str(ex,"Help"); }
        return text;
    }
    public static string Service(string key,string original) {
        if(!L.Korean) return original;
        switch(key) {
            case "alias":return "기존 연결의 별칭"; case "local":return "로컬 디스크"; case "memory":return "메모리 저장소";
            case "crypt":return "파일 암호화 저장소"; case "chunker":return "대용량 파일 분할"; case "union":return "여러 저장소 통합";
            case "combine":return "여러 저장소 결합"; case "compress":return "압축 저장소"; case "cache":return "저장소 캐시";
            case "hasher":return "체크섬 보완 저장소"; case "archive":return "압축 파일 읽기";
            case "s3":return "S3 호환 저장소 (AWS 등)"; case "drive":return "Google Drive"; case "onedrive":return "Microsoft OneDrive";
            case "hdfs":return "Hadoop 분산 파일 시스템"; case "storj":case "tardigrade":return "Storj 분산 클라우드 저장소";
            case "doi":return "DOI 데이터 세트"; case "koofr":return "Koofr · Digi Storage 호환 저장소";
            case "pixeldrain":return "Pixeldrain"; case "sia":return "Sia 분산 클라우드";
            case "iclouddrive":return "iCloud Drive 및 사진"; case "qingstor":return "QingCloud 객체 저장소";
            case "oracleobjectstorage":return "Oracle Cloud 객체 저장소"; case "swift":return "OpenStack Swift 호환 저장소";
            case "google cloud storage":return "Google Cloud Storage (구글 드라이브와 별도 서비스)";
            default:return original;
        }
    }
}
}
