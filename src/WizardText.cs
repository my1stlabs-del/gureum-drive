using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CloudDock {
static class WizardText {
    static readonly Dictionary<string,string> Titles=new Dictionary<string,string> {
        {"no_check_certificate","서버 인증서 확인 생략"},
        {"allow_insecure_tls_ciphers","보안이 약한 TLS 암호화 방식 허용"},
        {"disable_tls13","TLS 1.3 사용 안 함"},
        {"no_check_upload","업로드 결과 확인 생략"},
        {"skip_hash_validation","해시 검증 생략"},
        {"skip_checksum_gphotos","Google Photos 체크섬 확인 생략"},
        {"no_data_encryption","파일 내용 암호화 사용 안 함"},
        {"pass_bad_blocks","손상된 암호화 블록 허용"},
        {"strict_names","암호화된 이름 엄격 검사"},
        {"filename_encoding","암호화된 파일 이름 인코딩"},
        {"master_keys","암호화 마스터 키"},
        {"public_access","공개 접근 설정"},
        {"requester_pays","요청자가 비용을 부담하는 저장소"},
        {"shared_credentials_file","공유 인증 정보 파일"},
        {"role_arn","AWS 역할 ARN"},
        {"role_session_name","역할 세션 이름"},
        {"role_session_duration","역할 세션 유지 시간"},
        {"role_external_id","외부 역할 ID"},
        {"profile","인증 프로필"},
        {"sse_customer_key_file","사용자 제공 암호화 키 파일"},
        {"object_lock_mode","객체 잠금 방식"},
        {"object_lock_retain_until_date","객체 보존 기한"},
        {"object_lock_legal_hold_status","법적 보존 상태"},
        {"bypass_governance_retention","관리형 보존 제한 우회"},
        {"delete_snapshots","스냅샷 삭제 방식"},
        {"use_trash","휴지통 사용"},
        {"link_password","공유 링크 비밀번호"},
        {"link_scope","공유 링크 접근 범위"},
        {"link_type","공유 링크 종류"},
        {"expose_onenote_files","OneNote 파일 표시"},
        {"skip_gdocs","Google 문서 제외"},
        {"show_all_gdocs","모든 Google 문서 표시"},
        {"export_formats","문서 내보내기 형식"},
        {"import_formats","문서 가져오기 형식"},
        {"keep_revision_forever","파일 수정 버전 영구 보존"},
        {"copy_shortcut_content","바로가기 대상 내용 복사"},
        {"skip_shortcuts","바로가기 제외"},
        {"use_created_date","파일 생성일 사용"},
        {"acknowledge_abuse","유해 파일 경고 확인 후 다운로드"},
        {"stop_on_upload_limit","업로드 한도 도달 시 중단"},
        {"stop_on_download_limit","다운로드 한도 도달 시 중단"},
        {"copy_concurrency","동시 복사 작업 수"},
        {"copy_timeout","복사 대기 시간"},
        {"connections","연결 수"},
        {"concurrency","동시 작업 수"},
        {"list_page_size","목록 페이지 크기"},
        {"host_keys","신뢰할 서버 호스트 키"},
        {"pin_host_key","서버 호스트 키 고정"},
        {"known_hosts_file","신뢰할 서버 목록 파일"},
        {"client_id","앱 ID"},
        {"client_secret","앱 비밀키"},
        {"config_change_team_drive","공유 드라이브 설정 변경"},
        {"config_team_drive","사용할 공유 드라이브 선택"},
        {"config_warning","서비스 이용 시 주의사항"},
        {"config_2fa_sms","문자로 받은 인증 코드"},
        {"config_2fa_phone","인증 문자를 받을 전화번호"},
        {"config_2fa","2단계 인증 코드"},
        {"config_type","연결할 드라이브 종류"},
        {"config_login_token","개인 로그인 토큰"},
        {"config_traditional","접속할 서비스"},
        {"config_machine_specific","이 PC 전용 API 키 생성"},
        {"config_username","계정 이메일 주소"},
        {"config_password","인증에 사용할 비밀번호"},
        {"config_auth_code","문자로 받은 인증 코드"},
        {"config_non_standard","기본 저장 영역 외의 위치 사용"},
        {"config_device","연결할 기기"},
        {"config_mountpoint","기기의 저장 영역"},
        {"config_driveid","연결할 클라우드 저장소 선택"},
        {"config_driveid_fixed","드라이브 ID 직접 입력"},
        {"config_siteid","SharePoint 사이트 ID"},
        {"config_site_url","SharePoint 사이트 주소"},
        {"config_sharepoint_url","SharePoint 서버 기준 상대 경로"},
        {"config_search_term","사이트 검색어"},
        {"config_site","사용할 SharePoint 사이트"},
        {"config_drive_ok","선택한 드라이브 확인"},
        {"consent_to_authorize","다시 로그인"},
        {"config_retry","인증 다시 시도"},
        {"config_refresh","저장된 인증 정보 교체"},
        {"config_change_root","시작 폴더 변경"},
        {"config_workspace","작업 공간 선택"},
        {"config_team_drive_id","팀 저장소 선택"},
        {"config_fs_advanced","고급 설정 변경"},
        {"config_shared_client_id","공용 앱 ID 사용 여부"},
        {"config_refresh_token","저장된 인증 정보 교체"},
        {"config_is_local","브라우저로 로그인"},
        {"config_verification_code","인증 코드"},
        {"config_token","브라우저 인증 결과"}
    };
    public static string Title(string key) { string value; return Titles.TryGetValue(key,out value)?value:null; }
    public static bool HasTitle(string key) { return Titles.ContainsKey(key); }
    public static string Dynamic(string original) {
        string input=Regex.Replace(original??"",@"\s+"," ").Trim();
        { var match=Regex.Match(input,"^Change current Shared Drive \\(Team Drive\\) ID (.+?)\\?$",RegexOptions.Singleline); if(match.Success) return String.Format("현재 공유 드라이브(팀 드라이브) ID {0}을(를) 변경할까요?",new object[]{match.Groups[1].Value}); }
        { var match=Regex.Match(input,"^Enter the verification code sent to (.+?)$",RegexOptions.Singleline); if(match.Success) return String.Format("{0}(으)로 전송된 인증 코드를 입력하세요.",new object[]{match.Groups[1].Value}); }
        { var match=Regex.Match(input,"^Authentication failed: (.+?) Try Again\\?$",RegexOptions.Singleline); if(match.Success) return String.Format("인증 실패: {0}\n\n다시 시도할까요?",new object[]{match.Groups[1].Value}); }
        { var match=Regex.Match(input,"^Change current root folder id (.+?)\\?$",RegexOptions.Singleline); if(match.Success) return String.Format("현재 시작 폴더 ID {0}을(를) 변경할까요?",new object[]{match.Groups[1].Value}); }
        { var match=Regex.Match(input,"^rclone's shared (.+?) client_id is being retired and will stop working during 2026\\. Create your own to avoid interruption: (.+?) Continue using the shared client_id anyway\\?$",RegexOptions.Singleline); if(match.Success) return String.Format("rclone의 공용 {0} client_id는 지원이 종료되는 중이며 2026년 중 작동이 중단될 예정입니다.\n연결 중단을 피하려면 직접 앱 ID를 만드세요: {1}\n\n그래도 공용 client_id를 계속 사용할까요?",new object[]{match.Groups[1].Value,match.Groups[2].Value}); }
        { var match=Regex.Match(input,"^Verification code Go to this URL, authenticate then paste the code here\\. (.+?)$",RegexOptions.Singleline); if(match.Success) return String.Format("인증 코드\n\n다음 주소를 열어 인증한 뒤 받은 코드를 여기에 붙여넣으세요.\n\n{0}\n",new object[]{match.Groups[1].Value}); }
        return null;
    }
}
}
