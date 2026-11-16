using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CloudDock {
static class WizardGuidance {
    public static bool IsSecret(Dictionary<string,object> o) {
        return J.Bool(o,"IsPassword") || Regex.IsMatch(J.Str(o,"Name"),@"\A(?:client_secret|secret_access_key|api_secret|secret|pass|password|password2|key_file_pass|config_password|config_token|service_account_credentials)\z");
    }
    public static string ContextTitle(string service,string key) {
        if(service=="jottacloud"&&key=="config_type") return L.Pair("로그인 방식 선택","Choose authentication method");
        if(service=="crypt"&&key=="password") return L.Pair("파일 암호화 비밀번호","File encryption password");
        if(service=="crypt"&&key=="password2") return L.Pair("추가 암호화 비밀번호 (salt)","Additional encryption password (salt)");
        return null;
    }
    public static string Text(string service,Dictionary<string,object> o) {
        string k=J.Str(o,"Name");
        if(k=="config_shared_client_id") return L.Pair("앱 ID는 Google 로그인 이메일이 아니라 앱의 연결을 식별하는 값입니다.\n• 아니오: 다음 단계에서 직접 발급한 앱 ID와 비밀키를 입력합니다.\n• 예: 지원 종료 경고를 확인하고 공용 앱 ID로 계속합니다.\n원문에 적힌 2026년 경고는 현재 실제 중단 여부를 확인한 결과가 아닙니다.","An app ID identifies the connecting application, not your Google login.\n• No: enter your own client ID and secret next.\n• Yes: continue with the shared ID after reviewing the retirement warning.\nThe original 2026 warning does not verify the service's current status.");
        if(service=="crypt"&&(k=="password"||k=="password2")) return L.Pair("클라우드 로그인 비밀번호와 다른 암호화용 값입니다. 비밀번호와 관련 설정을 안전하게 보관하세요. 잃어버리면 저장된 자료를 복호화하지 못할 수 있습니다. 기존 암호화 저장소를 연결한다면 사용했던 값을 그대로 입력하세요.","This is separate from your cloud login password. Keep the encryption passwords and settings safe: losing them may prevent decryption. Use the original values when connecting existing encrypted storage.");
        if(k=="env_auth") {
            if(service=="drive"||service=="google cloud storage") return L.Pair("Google 실행 환경의 IAM 인증 정보를 사용하는 설정입니다. 일반 개인 계정의 브라우저 로그인과 구분하세요. 원문의 서비스 계정 적용 조건을 확인하세요.","Use IAM credentials from the Google runtime environment. This is different from personal browser login. Check the service-account conditions in the original help.");
            if(service=="s3") return L.Pair("현재 환경에 AWS 인증 정보가 준비돼 있을 때 사용합니다. 접근 키와 비밀키를 직접 입력하려면 아니오를 선택하고 원문의 적용 조건을 확인하세요.","Use this when AWS credentials are already configured in the runtime. For manually entered keys, choose No and check the conditions in the original help.");
            if(service=="azureblob") return L.Pair("환경 변수·Azure CLI·관리 ID 등 현재 환경의 인증 정보를 사용하는 설정입니다. 원문에 명시된 인증 방식과 준비 조건을 확인하세요.","Use credentials from environment variables, Azure CLI or managed identity. Check the original help for supported methods and prerequisites.");
            return L.Pair("현재 실행 환경에 설정된 인증 정보를 사용합니다. 지원하는 방식은 서비스마다 다르므로 원문의 적용 조건을 확인하세요.","Use credentials configured in the runtime environment. Supported methods vary by service; check the original conditions.");
        }
        if(k=="config_type" && service!="onedrive") return L.Pair("이 서비스에서 지원하는 인증 방식을 선택하세요. 각 선택지 아래의 준비 사항과 제한을 확인하세요.","Choose an authentication method supported by this service. Review the prerequisites and limits for each choice.");
        if(k=="config_fs_advanced") return L.Pair("일반 연결을 먼저 시도하려면 아니오로 진행하세요. 서비스 안내나 문제 해결을 위해 특정 설정을 바꿀 때 예를 선택하세요.","Choose No to try a normal connection first. Choose Yes when you need specific settings for your service or troubleshooting.");
        if(k=="client_id") return L.Pair("로그인 이메일이 아니라 직접 등록한 OAuth 앱의 ID입니다. 발급이 필요한지는 위 도움말의 서비스별 조건을 확인하세요.","This is your registered OAuth application's ID, not a login email. Check the help above to see whether your service requires one.");
        if(k=="client_secret") return L.Pair("같은 OAuth 앱에서 발급한 비밀키를 입력하세요. Google·Microsoft 계정의 로그인 비밀번호가 아닙니다. 다른 사람과 공유하지 마세요.","Enter the secret issued for the same OAuth application. This is not your Google or Microsoft login password. Do not share it.");
        if(k=="config_token") return L.Pair("웹 브라우저가 있는 PC에서 위 인증 명령을 실행하고 반환된 결과를 붙여넣으세요. 가능하면 같은 rclone 버전을 사용하세요. 결과에는 비밀값이 포함될 수 있으므로 외부에 공유하지 마세요.","Run the provided command on a PC with a web browser and paste its result here. Use the same rclone version when possible. The result may contain secrets; do not share it.");
        if(k=="config_2fa"||k=="config_2fa_sms"||k=="config_auth_code") return L.Pair("서비스에서 지정한 인증 앱 또는 문자 메시지로 받은 코드를 입력하세요. 일반 로그인 비밀번호와 구분하세요. SMS 요청 명령이 표시된 경우에만 해당 입력을 사용하세요.","Enter the code from the authentication app or SMS specified by the service, not your login password. Use an SMS request command only when the prompt offers it.");
        return null;
    }
    public static string Choice(Dictionary<string,object> o,string value) {
        bool yes=value.Equals("true",StringComparison.OrdinalIgnoreCase),no=value.Equals("false",StringComparison.OrdinalIgnoreCase); if(!yes&&!no) return null;
        switch(J.Str(o,"Name")) {
            case "config_shared_client_id": return yes?L.Pair("예 — 공용 앱 ID로 계속","Yes — continue with shared client ID"):L.Pair("아니오 — 직접 발급한 앱 ID 입력","No — enter my own client ID");
            case "config_is_local": return yes?L.Pair("예 — 이 PC의 브라우저에서 로그인","Yes — sign in using this PC's browser"):L.Pair("아니오 — 다른 PC 또는 수동 방식으로 인증","No — authenticate on another PC or manually");
            case "config_change_team_drive": return yes?L.Pair("예 — 공유 드라이브 설정·변경","Yes — configure or change Shared Drive"):L.Pair("아니오 — 공유 드라이브 설정·변경 안 함","No — do not configure or change Shared Drive");
            case "config_fs_advanced": return yes?L.Pair("예 — 고급 설정 변경","Yes — edit advanced settings"):L.Pair("아니오 — 현재 설정으로 진행","No — continue with current settings");
            case "config_drive_ok": return yes?L.Pair("예 — 이 저장소 사용","Yes — use this storage"):L.Pair("아니오 — 다시 선택","No — select again");
            case "config_refresh_token": case "config_refresh": return yes?L.Pair("예 — 저장된 인증 정보 교체","Yes — replace saved authentication"):L.Pair("아니오 — 교체하지 않음","No — do not replace it");
        }
        return null;
    }
    public static string InputHint(Dictionary<string,object> o,bool choice) {
        if(choice) return L.Pair("목록에서 사용할 항목을 선택하세요.","Choose an item from the list.");
        if(J.Bool(o,"Required")) return L.Pair("필수 항목입니다. 값을 입력해야 다음 단계로 진행할 수 있습니다.","Required. Enter a value to continue.");
        return L.Pair("선택 항목입니다. 값을 비워 둘 때의 동작은 위 도움말을 확인하세요.","Optional. Check the help above for what leaving it blank does.");
    }
}
}
