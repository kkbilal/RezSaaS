#!/usr/bin/env python3
"""
RezSaaS -- UCTAN UCA DUMAN TESTI (end-to-end smoke test).

Urunun cekirdek dongusunu GERCEK bir API'ye karsi bastan sona kosturur:

    platform admin -> tenant + owner provision -> salon kurulumu
    (sube, calisma saati, kaynak, personel, yetkinlik, hizmet, varyant)
    -> musteri kaydi -> slot arama -> randevu TALEBI -> salon ONAYI
    -> RANDEVU dogar -> dogrulanir
    -> musteri randevusunu GORUR -> BASKASI iptal EDEMEZ (404) -> iptal politikasi
       penceresinde iptal REDDEDILIR (409) -> musteri KENDI randevusunu IPTAL EDER
    -> iptal idempotenttir -> hem musteri hem isletme "Cancelled" gorur

Bagimlilik YOK: sadece Python 3 standart kutuphanesi (urllib + hmac/hashlib/base64).

Kullanim:
    python scripts/e2e-smoke.py --api-url http://localhost:5252
    python scripts/e2e-smoke.py --seed-business      # bkz. BILINEN URUN BOSLUGU

BILINEN URUN BOSLUGU (bu script'in ortaya cikardigi sey):
    Organization modulundeki `Business` kaydini olusturan HICBIR uretim kod yolu yok.
    `Business.Create(...)` sadece testlerden cagriliyor. Ne bir API ucu, ne bir seeder,
    ne de tenant provisioning bunu yapiyor. Ama:
      - `BranchManagementService.CreateAsync` tenant icin aktif bir Business ARIYOR;
        bulamazsa BUSINESS_NOT_FOUND doner  -> SUBE ACILAMAZ.
      - Tum public uclar (`/api/public/businesses/{slug}/...`) Business.Slug uzerinden
        cozumleniyor -> SALON PUBLIC'TE YOK.
    Yani tenant acildiktan sonra isletme kurulumu API uzerinden BASLAYAMIYOR.

    Varsayilan calisma bu bosluga TOSLAR ve net rapor verir (dogru davranis budur).
    `--seed-business` bayragi, Business satirini dogrudan Postgres'e yazan bir
    TEST KOSUM ARACI (harness) kestirmesidir; boylece zincirin GERI KALANI
    (sube -> ... -> randevu) yine de uctan uca dogrulanabilir.
    Bu bayrak urun kodunu DEGISTIRMEZ ve bosluk kapandiginda SILINMELIDIR.
"""

from __future__ import annotations

import argparse
import base64
import hashlib
import hmac
import http.cookiejar
import json
import os
import struct
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from datetime import datetime, timedelta, timezone

DEFAULT_API_URL = "http://localhost:5252"
DEFAULT_BOOTSTRAP_TOKEN = "rezsaas-local-e2e-bootstrap-token"

# Identity:PasswordRequiredLength=12, PasswordRequiredUniqueChars=4 + ASP.NET varsayilanlari
PASSWORD = "SmokeTest!2026aA"

# Platform admin SABIT tutulur (timestamp'li DEGIL): bootstrap idempotent olmadigi icin
# (ikinci kez 409 doner) ayni kimlikle tekrar tekrar GIRIS yapabilelim diye.
DEFAULT_ADMIN_EMAIL = "e2e-smoke-admin@rezsaas.test"

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Identity:AuthenticationWindowMinutes = 1 (IP basina dakikada 10 kimlik istegi).
# 429 yiyen kimlik cagrilarinda pencerenin kapanmasini bu kadar bekleriz.
RATE_LIMIT_WINDOW_SECONDS = 62


# --------------------------------------------------------------------------------------
# Cikti yardimcilari
# --------------------------------------------------------------------------------------

TOTAL_STEPS = 60

# Kosum sirasinda ortaya cikan URUN BULGULARI. FAIL olmayan (INFO) bulgular da buraya
# girer: testi dusurmeyen ama urun kararini etkileyen gercekler kaybolmasin.
FINDINGS: list[str] = []


def finding(text: str) -> None:
    FINDINGS.append(text)


class Reporter:
    def __init__(self) -> None:
        self.results: list[tuple[str, str, str]] = []
        self.index = 0
        self._current = "(baslamadan once)"

    def start(self, name: str) -> None:
        self.index += 1
        self._current = name
        sys.stdout.write(f"[{self.index}/{TOTAL_STEPS}] {name} ... ")
        sys.stdout.flush()

    def ok(self, detail: str = "") -> None:
        print(f"OK{(' (' + detail + ')') if detail else ''}")
        self.results.append((self._current, "PASS", detail))

    def skip(self, detail: str = "") -> None:
        print(f"SKIP{(' (' + detail + ')') if detail else ''}")
        self.results.append((self._current, "SKIP", detail))

    def info(self, detail: str = "") -> None:
        """
        Bilgi toplama adimi: bir GERCEGI kayda gecirir ama testi DUSURMEZ.

        Neden ayri bir statu: bu adimlarin sonucu "dogru/yanlis" degil, "sunucu ne yapiyor".
        PASS deseydik bulgu yesil satirlarin arasinda KAYBOLURDU; FAIL deseydik cekirdek
        dongu saglamken test kirmizi yanardi. INFO, bulguyu GORUNUR tutar.
        """
        print(f"INFO{(' (' + detail + ')') if detail else ''}")
        self.results.append((self._current, "INFO", detail))

    def fail(self, detail: str = "") -> None:
        print("FAIL")
        if detail:
            print(f"      -> {detail}")
        self.results.append((self._current, "FAIL", detail))

    def summary(self) -> bool:
        print()
        print("=" * 78)
        print("OZET")
        print("=" * 78)
        width = max((len(name) for name, _, _ in self.results), default=10)
        for name, status, detail in self.results:
            line = f"  {status:<5} {name:<{width}}"
            if detail:
                line += f"  {detail}"
            print(line)
        failed = [r for r in self.results if r[1] == "FAIL"]
        passed = [r for r in self.results if r[1] == "PASS"]
        skipped = [r for r in self.results if r[1] == "SKIP"]
        infos = [r for r in self.results if r[1] == "INFO"]
        print("-" * 78)
        print(
            f"  gecti={len(passed)}  kaldi={len(failed)}  "
            f"atlandi={len(skipped)}  bilgi={len(infos)}"
        )
        print("=" * 78)

        if FINDINGS:
            print()
            print("=" * 78)
            print("URUN BULGULARI")
            print("=" * 78)
            for number, text in enumerate(FINDINGS, start=1):
                print(f"  {number}. {text}")
            print("=" * 78)

        return not failed


REPORT = Reporter()

# Cekirdek dongu (talep -> onay -> RANDEVU) kanitlandi mi? Adim 24'ten sonra True olur.
# Adim 25 bir REGRESYON kontrolu; dusmesi cekirdek dongunun calistigi gercegini degistirmez.
CORE_LOOP_PROVEN = False


class StepFailed(Exception):
    """Bir adim basarisiz oldu; zincir devam edemez."""


# --------------------------------------------------------------------------------------
# HTTP istemcisi (cookie'li, aktor basina ayri oturum)
# --------------------------------------------------------------------------------------


class ApiError(Exception):
    def __init__(self, method: str, path: str, status: int, body: str) -> None:
        self.method = method
        self.path = path
        self.status = status
        self.body = body
        super().__init__(f"{method} {path} -> HTTP {status}\n      body: {body}")


class Session:
    """Tek bir aktorun (admin / owner / customer) cookie'li oturumu."""

    def __init__(self, base_url: str, label: str) -> None:
        self.base_url = base_url.rstrip("/")
        self.label = label
        self.origin = self._origin(self.base_url)
        self.tenant_id: str | None = None
        self.jar = http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(
            urllib.request.HTTPCookieProcessor(self.jar)
        )

    @staticmethod
    def _origin(base_url: str) -> str:
        parts = urllib.parse.urlsplit(base_url)
        return f"{parts.scheme}://{parts.netloc}"

    def request(
        self,
        method: str,
        path: str,
        body: object | None = None,
        query: dict[str, object] | None = None,
        headers: dict[str, str] | None = None,
        expect: tuple[int, ...] = (200, 201, 204),
        retry_on_rate_limit: int = 0,
    ) -> tuple[int, object]:
        """
        retry_on_rate_limit: beklenmeyen 429'da kac kez bekleyip yeniden denesin.

        Identity:AuthenticationPermitLimit = IP basina DAKIKADA 10 istek. Bu kosum tek
        akista ~8 kimlik cagrisi yapiyor (admin/owner/customer/intruder icin register+login),
        yani art arda kosuldugunda tavana CARPIYOR. 429 URUN HATASI DEGIL -- kasitli bir
        koruma. Bu yuzden kimlik cagrilarinda pencereyi bekleyip TEKRAR deniyoruz.
        Beklemeyi SESSIZCE yapmiyoruz: ekrana yaziyoruz ki gecikme gizli kalmasin.
        """
        url = self.base_url + path
        if query:
            clean = {k: str(v) for k, v in query.items() if v is not None}
            url += "?" + urllib.parse.urlencode(clean)

        data = None
        req_headers = {
            "Accept": "application/json",
            # UnsafeRequestOriginMiddleware fail-closed: POST/PUT/PATCH/DELETE icin
            # Origin (ya da Referer) SART. Origin == request'in kendi origin'i ise gecer.
            "Origin": self.origin,
        }
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            req_headers["Content-Type"] = "application/json"
        if self.tenant_id:
            req_headers["X-RezSaaS-Tenant"] = self.tenant_id
        if headers:
            req_headers.update(headers)

        req = urllib.request.Request(url, data=data, method=method, headers=req_headers)
        retry_after: str | None = None

        try:
            with self.opener.open(req, timeout=30) as response:
                status = response.status
                raw = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as error:
            status = error.code
            raw = error.read().decode("utf-8", errors="replace")
            retry_after = error.headers.get("Retry-After")
        except urllib.error.URLError as error:
            raise StepFailed(
                f"API'ye baglanilamadi ({url}): {error.reason}. API kosuyor mu?"
            ) from error

        if status == 429 and status not in expect and retry_on_rate_limit > 0:
            delay = RATE_LIMIT_WINDOW_SECONDS
            if retry_after and retry_after.strip().isdigit():
                delay = int(retry_after.strip()) + 2
            print(f"\n      (HTTP 429 hiz siniri -- {delay}sn beklenip yeniden denenecek) ", end="")
            sys.stdout.flush()
            time.sleep(delay)
            return self.request(
                method,
                path,
                body=body,
                query=query,
                headers=headers,
                expect=expect,
                retry_on_rate_limit=retry_on_rate_limit - 1,
            )

        payload: object
        try:
            payload = json.loads(raw) if raw.strip() else None
        except json.JSONDecodeError:
            payload = raw

        if status not in expect:
            raise ApiError(method, path, status, _short(raw))

        return status, payload

    def get(self, path: str, **kwargs) -> tuple[int, object]:
        return self.request("GET", path, **kwargs)

    def post(self, path: str, body: object | None = None, **kwargs) -> tuple[int, object]:
        return self.request("POST", path, body=body, **kwargs)

    def put(self, path: str, body: object | None = None, **kwargs) -> tuple[int, object]:
        return self.request("PUT", path, body=body, **kwargs)

    def patch(self, path: str, body: object | None = None, **kwargs) -> tuple[int, object]:
        return self.request("PATCH", path, body=body, **kwargs)


def _short(raw: str, limit: int = 600) -> str:
    raw = " ".join(raw.split())
    return raw if len(raw) <= limit else raw[:limit] + " ...(kirpildi)"


# --------------------------------------------------------------------------------------
# TOTP (RFC 6238: SHA1, 30 sn, 6 hane) -- ASP.NET Identity authenticator ile uyumlu
# --------------------------------------------------------------------------------------


def totp(shared_key_base32: str, at: float | None = None) -> str:
    key = shared_key_base32.strip().replace(" ", "").upper()
    key += "=" * (-len(key) % 8)  # base32 padding
    secret = base64.b32decode(key)
    counter = int((at if at is not None else time.time()) // 30)
    digest = hmac.new(secret, struct.pack(">Q", counter), hashlib.sha1).digest()
    offset = digest[-1] & 0x0F
    code = struct.unpack(">I", digest[offset : offset + 4])[0] & 0x7FFFFFFF
    return str(code % 1_000_000).zfill(6)


def totp_fresh(shared_key_base32: str) -> str:
    """Kod suresi dolmak uzereyse bir sonraki pencereyi bekle, sonra uret."""
    remaining = 30 - (time.time() % 30)
    if remaining < 3:
        time.sleep(remaining + 0.3)
    return totp(shared_key_base32)


# --------------------------------------------------------------------------------------
# Kalici durum: admin'in TOTP sharedKey'i
# --------------------------------------------------------------------------------------
#
# Platform admin bootstrap TEK SEFERLIK (ikinci cagri 409). Admin'de 2FA acildiktan sonra
# POST /api/auth/login artik "401 RequiresTwoFactor" doner -- yani sonraki kosularda GIRIS
# ICIN de TOTP gerekir. Ama sharedKey'i okumak icin (POST /api/auth/manage/2fa) once giris
# yapmak gerekir => yumurta-tavuk. Cozum: sharedKey'i ilk kosuda diske yazip sonraki
# kosularda oradan okumak. (artifacts/local/ .gitignore'da.)

STATE_PATH = os.path.join(REPO_ROOT, "artifacts", "local", "e2e-smoke-state.json")


def load_state() -> dict[str, str]:
    try:
        with open(STATE_PATH, encoding="utf-8") as handle:
            data = json.load(handle)
        return data if isinstance(data, dict) else {}
    except (OSError, json.JSONDecodeError):
        return {}


def save_state(state: dict[str, str]) -> None:
    try:
        os.makedirs(os.path.dirname(STATE_PATH), exist_ok=True)
        with open(STATE_PATH, "w", encoding="utf-8") as handle:
            json.dump(state, handle, indent=2)
    except OSError:
        pass  # durum dosyasi yazilamazsa test yine de kosabilmeli


# --------------------------------------------------------------------------------------
# --seed-business: BILINEN URUN BOSLUGU icin test kosum araci kestirmesi
# --------------------------------------------------------------------------------------


def read_dotenv() -> dict[str, str]:
    path = os.path.join(REPO_ROOT, ".env")
    values: dict[str, str] = {}
    if not os.path.exists(path):
        return values
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, _, value = line.partition("=")
            values[key.strip()] = value.strip()
    return values


def seed_business(tenant_id: str, slug: str, display_name: str) -> str:
    """
    Organization.Businesses satirini DOGRUDAN veritabanina yazar.

    Bu bir URUN AKISI DEGILDIR. Business olusturan bir uretim kod yolu OLMADIGI icin
    (bkz. dosya basindaki 'BILINEN URUN BOSLUGU') zincirin geri kalanini dogrulayabilmek
    adina konulmus bir test kosum araci kestirmesidir.
    """
    env = read_dotenv()
    db = env.get("REZSAAS_POSTGRES_DB", "rezsaas")
    user = env.get("REZSAAS_POSTGRES_USER", "rezsaas")
    password = env.get("REZSAAS_POSTGRES_PASSWORD", "")
    host = env.get("REZSAAS_POSTGRES_HOST", "localhost")
    port = env.get("REZSAAS_POSTGRES_PORT", "5432")

    business_id = str(uuid.uuid4())
    columns = (
        '"Id","TenantId","Slug","NormalizedSlug","DisplayName","CategoryKey",'
        '"Description","PublicRules","PublicStaffDisplayPolicy","RatingAverage",'
        '"ReviewCount","SeoDescription","SeoTitle","Status","CreatedAtUtc"'
    )
    values = (
        f"'{business_id}','{tenant_id}','{slug}','{slug.upper()}','{display_name}',"
        f"'hair','','','ShowNames',0,0,'','{display_name}','Active',now()"
    )
    sql = f'INSERT INTO organization."Businesses" ({columns}) VALUES ({values});'

    attempts = [
        (
            ["docker", "compose", "exec", "-T", "postgres", "psql", "-U", user, "-d", db,
             "-v", "ON_ERROR_STOP=1", "-c", sql],
            {**os.environ, "PGPASSWORD": password},
            REPO_ROOT,
        ),
        (
            ["psql", "-h", host, "-p", port, "-U", user, "-d", db,
             "-v", "ON_ERROR_STOP=1", "-c", sql],
            {**os.environ, "PGPASSWORD": password},
            REPO_ROOT,
        ),
    ]

    errors: list[str] = []
    for command, env_vars, cwd in attempts:
        try:
            result = subprocess.run(
                command, env=env_vars, cwd=cwd, capture_output=True, text=True, timeout=60
            )
        except (FileNotFoundError, subprocess.TimeoutExpired) as error:
            errors.append(f"{command[0]}: {error}")
            continue
        if result.returncode == 0:
            return business_id
        errors.append(f"{command[0]}: {_short((result.stderr or result.stdout).strip())}")

    raise StepFailed(
        "Business satiri yazilamadi (docker compose psql ve yerel psql denendi). "
        + " | ".join(errors)
    )


# --------------------------------------------------------------------------------------
# Akis
# --------------------------------------------------------------------------------------


def run(
    api_url: str,
    bootstrap_token: str,
    do_seed: bool,
    admin_email: str,
    admin_password: str,
    admin_totp_secret: str | None = None,
) -> bool:
    stamp = datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S")
    unique = uuid.uuid4().hex[:6]
    tag = f"{stamp}{unique}"

    owner_email = f"owner+{tag}@rezsaas.test"
    customer_email = f"customer+{tag}@rezsaas.test"
    # Sahiplik kontrolunu sinayan IKINCI musteri (adim 27): randevu ONUN DEGIL.
    attacker_email = f"intruder+{tag}@rezsaas.test"
    tenant_slug = f"salon-{tag}"
    branch_slug = f"merkez-{tag}"
    business_slug = tenant_slug  # seed edilen Business ayni slug'i kullanir

    admin = Session(api_url, "admin")
    owner = Session(api_url, "owner")
    customer = Session(api_url, "customer")

    print("=" * 78)
    print("RezSaaS -- UCTAN UCA DUMAN TESTI")
    print("=" * 78)
    print(f"  API           : {api_url}")
    print(f"  tenant slug   : {tenant_slug}")
    print(f"  admin         : {admin_email}")
    print(f"  owner         : {owner_email}")
    print(f"  customer      : {customer_email}")
    print(f"  bootstrap tokn: {bootstrap_token}")
    print(f"  token SHA256  : {hashlib.sha256(bootstrap_token.encode()).hexdigest()}")
    print("                  ^ API'yi baslatirken bunu su ayara koyun:")
    print("                    Identity__Bootstrap__PlatformAdminBootstrapTokenSha256")
    print("=" * 78)
    print()

    # -- 1 -------------------------------------------------------------------- saglik
    REPORT.start("API erisilebilir mi (/health)")
    status, _ = admin.get("/health", expect=(200, 503))
    if status != 200:
        REPORT.fail(f"HTTP {status} -- API saglikli degil (DB kapali olabilir)")
        return False
    REPORT.ok("HTTP 200")

    # -- 2 --------------------------------------------------- platform admin bootstrap
    # Bootstrap TEK SEFERLIK (2. cagri 409) ve rate limit'i CIMRI: IP basina 15 dakikada 5.
    # Bu yuzden ONCE GIRIS deniyoruz; sadece admin YOKSA bootstrap'e dokunuyoruz. Boylece
    # tekrarli kosular bootstrap kotasini HIC harcamaz.
    state = load_state()
    saved_secret = admin_totp_secret or state.get(admin_email)
    login_body = {"email": admin_email, "password": admin_password}

    def try_login() -> tuple[int, object]:
        return admin.post(
            "/api/auth/login",
            login_body,
            query={"useCookies": "true"},
            expect=(200, 401),
            retry_on_rate_limit=2,
        )

    REPORT.start("Platform admin hazir (gerekirse bootstrap)")
    status, payload = try_login()
    detail = payload.get("detail") if isinstance(payload, dict) else None

    if status == 200 or detail == "RequiresTwoFactor":
        REPORT.ok("mevcut admin bulundu -- bootstrap'e gerek yok")
    else:
        # Admin yok (ya da parola yanlis) -> bootstrap dene.
        status, payload = admin.post(
            "/api/admin/bootstrap/platform-admin",
            {
                "email": admin_email,
                "password": admin_password,
                "bootstrapToken": bootstrap_token,
            },
            expect=(200, 400, 409, 429, 503),
        )
        if status == 200:
            REPORT.ok(f"HTTP 200 -- {admin_email} olusturuldu")
        elif status == 409:
            REPORT.fail(
                "HTTP 409 PLATFORM_ADMIN_ALREADY_EXISTS -- veritabaninda BASKA bir platform "
                f"admin var ve '{admin_email}' ile giris yapilamiyor. Mevcut admin'in "
                "kimligini --admin-email / --admin-password ile verin."
            )
            return False
        elif status == 429:
            REPORT.fail(
                "HTTP 429 -- bootstrap rate limit'i asildi (IP basina 15 dakikada 5 istek). "
                "15 dakika bekleyin ya da API'yi yeniden baslatin (limit bellekte tutulur)."
            )
            return False
        elif status == 503:
            REPORT.fail(
                "HTTP 503 -- Identity:Bootstrap:PlatformAdminBootstrapTokenSha256 AYARLANMAMIS. "
                "API'yi yukaridaki SHA256 ile baslatin (README'deki tam komuta bakin)."
            )
            return False
        else:
            REPORT.fail(f"HTTP {status} -- {payload}")
            return False
        status, payload = try_login()
        detail = payload.get("detail") if isinstance(payload, dict) else None

    # -- 3 ------------------------------------------------------------- admin girisi
    # 2FA acik bir admin'de duz giris "401 RequiresTwoFactor" doner -> TOTP ile tekrar dene.
    REPORT.start("Admin girisi (cookie)")
    if status == 401:
        if detail != "RequiresTwoFactor":
            REPORT.fail(
                f"HTTP 401 ({detail}) -- '{admin_email}' ile giris yapilamiyor. "
                "Mevcut admin'i kullanmak icin --admin-email / --admin-password verin."
            )
            return False
        if not saved_secret:
            REPORT.fail(
                f"HTTP 401 RequiresTwoFactor -- '{admin_email}' hesabinda 2FA acik ama "
                f"TOTP anahtari elimizde yok ({STATE_PATH} bulunamadi). Cozum: anahtari "
                "--admin-totp-secret ile verin, VEYA platform admin'i silip bootstrap'i "
                "bastan calistirin (bkz. README 'Tekrar tekrar kosmak')."
            )
            return False
        login_body["twoFactorCode"] = totp_fresh(saved_secret)
        admin.post(
            "/api/auth/login",
            login_body,
            query={"useCookies": "true"},
            expect=(200,),
            retry_on_rate_limit=2,
        )
        REPORT.ok("HTTP 200 (TOTP ile), cookie alindi")
    else:
        REPORT.ok("HTTP 200, cookie alindi")

    # -- 4 ---------------------------------------------------------- admin 2FA anahtari
    REPORT.start("Admin 2FA anahtari (sharedKey)")
    _, payload = admin.post("/api/auth/manage/2fa", {}, expect=(200,))
    shared_key = payload.get("sharedKey") if isinstance(payload, dict) else None
    if not shared_key:
        REPORT.fail(f"sharedKey yanitta yok: {payload}")
        return False
    state[admin_email] = shared_key
    save_state(state)  # sonraki kosularda giris icin gerekli
    REPORT.ok("sharedKey alindi ve saklandi")

    # -- 5 --------------------------------------------------------------- admin 2FA ac
    REPORT.start("Admin 2FA etkinlestirme (TOTP)")
    _, payload = admin.post(
        "/api/auth/manage/2fa",
        {"enable": True, "twoFactorCode": totp_fresh(shared_key)},
        expect=(200,),
    )
    if not (isinstance(payload, dict) and payload.get("isTwoFactorEnabled")):
        REPORT.fail(f"2FA acilamadi: {payload}")
        return False
    REPORT.ok("isTwoFactorEnabled=true")

    # -- 6 ------------------------------------------------------------------- step-up
    # TUM /api/admin/* uclari PlatformAdminWithStepUp istiyor; privileged hesap icin
    # 2FA olmadan step-up STEP_UP_MFA_REQUIRED ile reddedilir.
    REPORT.start("Admin step-up (parola + TOTP)")
    _, payload = admin.post(
        "/api/session/step-up",
        {
            "password": admin_password,
            "twoFactorCode": totp_fresh(shared_key),
            "recoveryCode": None,
        },
        expect=(200,),
    )
    if not (isinstance(payload, dict) and payload.get("isSatisfied")):
        REPORT.fail(f"step-up saglanmadi: {payload}")
        return False
    REPORT.ok(f"isSatisfied=true, method={payload.get('method')}")

    # -- 7 -------------------------------------------------- step-up dogrulama (bootstrap)
    REPORT.start("Session bootstrap ile step-up dogrulama")
    _, payload = admin.get("/api/session/bootstrap", expect=(200,))
    step_up = payload.get("stepUp", {}) if isinstance(payload, dict) else {}
    if not step_up.get("isSatisfied"):
        REPORT.fail(f"stepUp.isSatisfied != true: {payload}")
        return False
    REPORT.ok("stepUp.isSatisfied=true")

    # -- 8 ------------------------------------------------------------- owner kaydi
    # DIKKAT: POST /api/admin/tenants MEVCUT bir OwnerUserAccountId istiyor.
    # Yani owner ONCE kaydolmali; userAccountId'sini session bootstrap'tan aliyoruz.
    REPORT.start("Owner kaydi (/api/auth/register)")
    owner.post(
        "/api/auth/register",
        {"email": owner_email, "password": PASSWORD},
        expect=(200,),
        retry_on_rate_limit=2,
    )
    REPORT.ok(owner_email)

    # -- 9 ------------------------------------------- owner girisi + userAccountId alma
    REPORT.start("Owner girisi + userAccountId")
    owner.post(
        "/api/auth/login",
        {"email": owner_email, "password": PASSWORD},
        query={"useCookies": "true"},
        expect=(200,),
        retry_on_rate_limit=2,
    )
    _, payload = owner.get("/api/session/bootstrap", expect=(200,))
    owner_user_id = payload["account"]["userAccountId"]
    REPORT.ok(f"userAccountId={owner_user_id}")

    # -- 10 ------------------------------------------------------ tenant provisioning
    REPORT.start("Tenant + owner provisioning (admin)")
    _, payload = admin.post(
        "/api/admin/tenants",
        {
            "slug": tenant_slug,
            "displayName": f"Duman Salon {tag}",
            "ownerUserAccountId": owner_user_id,
            # Provisioning artik salonun public kimligini (Business) de olusturuyor.
            # Kategori zorunlu: Business.CategoryKey bir domain invariant'i ve /kesfet
            # filtresi buna dayaniyor.
            "categoryKey": "hair",
        },
        expect=(201,),
    )
    tenant_id = payload["tenantId"]
    owner.tenant_id = tenant_id  # tum /api/business/* cagrilarinda X-RezSaaS-Tenant
    REPORT.ok(f"tenantId={tenant_id}")

    # -- 11 ------------------- Business provisioning tarafindan OLUSTURULDU mu? (regresyon)
    #
    # Eskiden burada bir HARNESS KESTIRMESI vardi (--seed-business): Business satirini
    # DOGRUDAN Postgres'e yaziyorduk, cunku onu olusturan HICBIR URETIM KODU YOLU YOKTU.
    # Bu, urunun tek bir salonu bile onboard edemedigi anlamina geliyordu (owner sube bile
    # acamiyordu: BUSINESS_NOT_FOUND) -- ve bu testin ortaya cikardigi EN BUYUK bosluktu.
    #
    # Artik POST /api/admin/tenants Business'i da olusturuyor. Kestirme SILINDI.
    # Bu adim artik bir REGRESYON TESTI: yanitta businessId GELMEZSE bosluk geri gelmis demektir.
    REPORT.start("Business provisioning ile olusturuldu mu (regresyon)")
    business_id = payload.get("businessId")
    if not business_id:
        REPORT.fail("Yanitta businessId YOK -- Business provisioning'de olusturulmuyor (LANSMAN BLOKAJI geri geldi)")
        raise SystemExit(1)
    REPORT.ok(f"businessId={business_id} (API tarafindan olusturuldu, seed YOK)")

    # -- 12 ---------------------------------------------------------------- sube acma
    REPORT.start("Sube olusturma (owner)")
    try:
        _, payload = owner.post(
            "/api/business/branches",
            {
                "slug": branch_slug,
                "displayName": "Merkez Sube",
                "timeZoneId": "Europe/Istanbul",  # IANA sart; baska format 0 slot doner
                "city": "Istanbul",
                "district": "Kadikoy",
                "addressLine": "Duman Testi Sokak 1",
            },
            expect=(201,),
        )
    except ApiError as error:
        if "BUSINESS_NOT_FOUND" in error.body:
            REPORT.fail(
                "BUSINESS_NOT_FOUND -- URUN BOSLUGU DOGRULANDI: tenant acildi ama "
                "Organization.Business kaydi yok, ve onu olusturan bir API ucu/seeder/"
                "event handler MEVCUT DEGIL (Business.Create yalnizca testlerden cagriliyor). "
                "Sube acilamiyor -> isletme kurulumu API uzerinden BASLAYAMIYOR. "
                "Zincirin geri kalanini dogrulamak icin: --seed-business"
            )
        else:
            REPORT.fail(str(error))
        return False
    branch_id = payload["id"]
    REPORT.ok(f"branchId={branch_id}")

    # -- 13 ---------------------------------------------------------- calisma saatleri
    # Slot aranacak GUNUN acik olmasi sart. Hangi gune denk geldigini hesaplamak yerine
    # 7 gunun hepsini aciyoruz.
    REPORT.start("Calisma saatleri (7 gun 09:00-19:00)")
    days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    for day in days:
        owner.put(
            f"/api/business/branches/{branch_id}/working-hours/{day}",
            {"opensAt": "09:00", "closesAt": "19:00", "isClosed": False},
            expect=(200,),
        )
    REPORT.ok("7/7 gun acik")

    # -- 14 -------------------------------------------------------------- kaynak tipi
    REPORT.start("Kaynak tipi olusturma")
    _, payload = owner.post(
        "/api/business/resource-types",
        {"key": f"koltuk-{unique}", "displayName": "Kuafor Koltugu"},
        expect=(201,),
    )
    resource_type_id = payload["id"]
    REPORT.ok(f"resourceTypeId={resource_type_id}")

    # -- 15 ------------------------------------------------------------------- kaynak
    # DIKKAT: Slot motoru kaynak adayi BULAMAZSA 0 slot doner -- varyantta
    # RequiredResourceTypeId olmasa bile subede en az 1 aktif kaynak SART.
    REPORT.start("Kaynak olusturma")
    _, payload = owner.post(
        f"/api/business/branches/{branch_id}/resources",
        {"resourceTypeId": resource_type_id, "displayName": "Koltuk 1"},
        expect=(201,),
    )
    resource_id = payload["id"]
    REPORT.ok(f"resourceId={resource_id}")

    # -- 16 ----------------------------------------------------------------- personel
    REPORT.start("Personel olusturma")
    _, payload = owner.post(
        f"/api/business/branches/{branch_id}/staff",
        {"displayName": "Ayse Usta", "userAccountId": None},
        expect=(201,),
    )
    staff_id = payload["id"]
    REPORT.ok(f"staffId={staff_id}")

    # -- 17 --------------------------------------------------- yetkinlik + personele ata
    # Slot motoru: varyantin RequiredSkillIds'inin HEPSI personelde yoksa personel elenir
    # ve 0 slot doner. Yetkinligi hem personele hem varyanta baglayarak bu eslesmeyi
    # GERCEKTEN sinariz.
    REPORT.start("Yetkinlik olustur + personele ata")
    _, payload = owner.post(
        "/api/business/skills", {"name": f"Sac Kesimi {unique}"}, expect=(201,)
    )
    skill_id = payload["id"]
    owner.post(
        f"/api/business/staff/{staff_id}/skills", {"skillId": skill_id}, expect=(200,)
    )
    REPORT.ok(f"skillId={skill_id} -> personele atandi")

    # -- 18 -------------------------------------------------------------------- hizmet
    REPORT.start("Hizmet olusturma")
    _, payload = owner.post(
        "/api/business/services",
        {"name": "Sac Kesimi", "categoryKey": "hair"},
        expect=(201,),
    )
    service_id = payload["id"]
    REPORT.ok(f"serviceId={service_id}")

    # -- 19 ------------------------------------------------- varyant + gerekli yetkinlik
    REPORT.start("Varyant olustur + gerekli yetkinlik bagla")
    _, payload = owner.post(
        f"/api/business/services/{service_id}/variants",
        {
            "name": "Sac Kesimi (30dk)",
            "durationMinutes": 30,
            "priceAmount": 500,
            "currencyCode": "TRY",
            "requiredResourceTypeId": resource_type_id,
        },
        expect=(201,),
    )
    variant_id = payload["id"]
    owner.post(
        f"/api/business/services/{service_id}/variants/{variant_id}/required-skills/{skill_id}",
        expect=(200,),
    )
    REPORT.ok(f"variantId={variant_id}, gerekli yetkinlik baglandi")

    # -- 20 --------------------------------------------------- musteri kaydi + girisi
    REPORT.start("Musteri kaydi + girisi")
    customer.post(
        "/api/auth/register",
        {"email": customer_email, "password": PASSWORD},
        expect=(200,),
        retry_on_rate_limit=2,
    )
    # E-POSTA DOGRULAMA TUZAGI: Identity:RequireConfirmedEmail=true + DeliveryMode=Unconfigured
    # olsaydi musteri e-postasini DOGRULAYAMAZ ve buraya kadar bile gelemezdi.
    # appsettings.Development.json bunu zaten false yapiyor (bkz. README).
    try:
        customer.post(
            "/api/auth/login",
            {"email": customer_email, "password": PASSWORD},
            query={"useCookies": "true"},
            expect=(200,),
            retry_on_rate_limit=2,
        )
    except ApiError as error:
        REPORT.fail(
            f"{error}\n      -> Muhtemel sebep: Identity:RequireConfirmedEmail=true ve "
            "e-posta saglayicisi yok. API'yi Identity__RequireConfirmedEmail=false ile "
            "baslatin (bkz. README)."
        )
        return False
    REPORT.ok(customer_email)

    # -- 21 ---------------------------------------------------------------- slot arama
    REPORT.start("Public slot arama")
    target_date = (datetime.now(timezone.utc) + timedelta(days=3)).date().isoformat()
    _, payload = customer.get(
        f"/api/public/businesses/{business_slug}/slots",
        query={
            "branchSlug": branch_slug,
            "date": target_date,
            "serviceVariantIds": variant_id,
        },
        expect=(200,),
    )
    slots = payload.get("slots") or []
    if not slots:
        REPORT.fail(
            f"0 SLOT dondu (tarih={target_date}, tz={payload.get('branchTimeZoneId')}, "
            f"sure={payload.get('durationMinutes')}dk). Olasi sebepler: o gun calisma "
            "saati kapali; personel gerekli yetkinlige sahip degil; subede aktif kaynak "
            "yok; TimeZoneId IANA formatinda degil."
        )
        return False
    # Booking:Security:DefaultResponseBuffer = 2 saat -> baslangic now+2sa'ten SONRA olmali
    # (3 gun sonrasini hedefledigimiz icin zaten guvendeyiz, yine de acikca seciyoruz).
    cutoff = datetime.now(timezone.utc) + timedelta(hours=3)
    chosen = None
    for slot in slots:
        start = datetime.fromisoformat(slot["startUtc"].replace("Z", "+00:00"))
        if start > cutoff and slot.get("staffCandidates"):
            chosen = slot
            break
    if not chosen:
        REPORT.fail("Uygun slot yok (hepsi response buffer icinde ya da personel adayi bos)")
        return False
    start_utc = chosen["startUtc"]
    staff_candidate_id = chosen["staffCandidates"][0]["id"]
    REPORT.ok(
        f"{len(slots)} slot; secilen local={chosen['localStart']} "
        f"startUtc={start_utc} staff={staff_candidate_id}"
    )

    # -- 22 ------------------------------------------------------------- randevu talebi
    REPORT.start("Randevu TALEBI gonderme (musteri)")
    _, payload = customer.post(
        f"/api/public/businesses/{business_slug}/appointment-requests",
        {
            "branchSlug": branch_slug,
            "serviceVariantIds": [variant_id],
            "staffMemberId": staff_candidate_id,
            "startUtc": start_utc,
        },
        headers={"Idempotency-Key": str(uuid.uuid4())},
        expect=(201,),
    )
    request_id = payload["appointmentRequestId"]
    REPORT.ok(f"requestId={request_id}, status={payload.get('status')}")

    # -- 23 ---------------------------------------------------------------- onay (owner)
    REPORT.start("Talebi ONAYLAMA (owner)")
    _, payload = owner.post(
        f"/api/business/appointment-requests/{request_id}/approve",
        headers={"Idempotency-Key": str(uuid.uuid4())},
        expect=(200,),
    )
    appointment_id = payload.get("appointmentId")
    if not appointment_id:
        REPORT.fail(f"appointmentId donmedi: {payload}")
        return False
    REPORT.ok(f"appointmentId={appointment_id}, status={payload.get('status')}")

    # -- 24 ---------------------------------------------------- RANDEVU DOGRULAMA
    # ACIK UTC araligi veriyoruz (offset=0). Parametresiz cagri BOZUK -- bkz. adim 25.
    REPORT.start("RANDEVUNUN DOGDUGUNU dogrulama")
    now = datetime.now(timezone.utc)
    _, payload = owner.get(
        "/api/business/appointments",
        query={
            "fromUtc": (now - timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "toUtc": (now + timedelta(days=30)).strftime("%Y-%m-%dT%H:%M:%SZ"),
        },
        expect=(200,),
    )
    appointments = payload.get("appointments") or []
    match = next(
        (a for a in appointments if a.get("appointmentId") == appointment_id), None
    )
    if not match:
        REPORT.fail(
            f"appointmentId={appointment_id} /api/business/appointments listesinde YOK "
            f"({len(appointments)} randevu dondu)"
        )
        return False
    # FIYAT SNAPSHOT'ININ BASLANGIC DEGERI (adim 39 bunu kullanir).
    # Randevu DOGDUGU ANDAKI line fiyatini burada, HENUZ HICBIR SEY DEGISMEDEN yakaliyoruz.
    # Iddia: bu deger, katalogdaki fiyat sonradan degisse bile SABIT kalmali.
    booked_lines = list(match.get("lines") or [])
    booked_line = next(
        (line for line in booked_lines if line.get("serviceVariantId") == variant_id), None
    )
    if not booked_line:
        REPORT.fail(
            f"Randevunun lines[] dizisinde variantId={variant_id} YOK ({booked_lines}). "
            "Fiyat snapshot'i dogrulanamaz -- line, hangi varyanttan dogduğunu tasimali."
        )
        return False
    booked_price = booked_line.get("priceAmount")
    booked_duration = booked_line.get("durationMinutes")
    booked_currency = booked_line.get("currencyCode")

    REPORT.ok(
        f"status={match.get('status')} start={match.get('startUtc')} "
        f"staff={match.get('staffMemberDisplayName')} kaynak={match.get('resourceDisplayName')} "
        f"line={booked_price} {booked_currency}/{booked_duration}dk"
    )

    global CORE_LOOP_PROVEN
    CORE_LOOP_PROVEN = True  # talep -> onay -> RANDEVU zinciri kanitlandi

    # -- 25 ------------------------------------------- REGRESYON: parametresiz randevu listesi
    # BusinessAppointmentComposer.cs:78
    #     DateTimeOffset rangeStartUtc = fromUtc ?? DateTimeOffset.UtcNow.Date;
    # `DateTimeOffset.UtcNow.Date` bir DateTime (Kind=Unspecified) dondurur; DateTimeOffset'e
    # ortuk donusum YEREL saat dilimi offset'ini uygular (Istanbul'da +03:00). Npgsql ise
    # `timestamp with time zone` icin offset'i 0 OLMAYAN DateTimeOffset yazmayi REDDEDER
    # -> 500. Yani sunucu saat dilimi UTC DEGILSE, `fromUtc` verilmeyen her cagri PATLAR.
    # UTC makinelerde (CI) sorunsuz gectigi icin testlerden kaciyor.
    REPORT.start("Regresyon: parametresiz GET /api/business/appointments")
    status, payload = owner.get(
        "/api/business/appointments", expect=(200, 500)
    )
    if status == 500:
        REPORT.fail(
            "HTTP 500 -- URUN HATASI: BusinessAppointmentComposer.cs:78 "
            "`fromUtc ?? DateTimeOffset.UtcNow.Date` YEREL offset'li (+03:00) bir "
            "DateTimeOffset uretiyor; Npgsql `timestamp with time zone` icin offset!=0 "
            "kabul etmiyor. Sunucu saat dilimi UTC olmayan her yerde (or. Turkiye) "
            "fromUtc'suz cagri 500 doner. Duzeltme: `DateTimeOffset.UtcNow.Date` yerine "
            "`new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero)` kullanin."
        )
        return False
    REPORT.ok(f"HTTP 200 ({len(payload.get('appointments') or [])} randevu)")

    # ==================================================================================
    # MUSTERI IPTALI (adim 26-36)
    #
    # Buraya kadar: randevu DOGDU. Simdi urunun ikinci yarisi: musteri onu IPTAL
    # edebiliyor mu, ve iptal EDEMEMESI gereken durumlarda GERCEKTEN engelleniyor mu?
    # ==================================================================================

    def cancel_appointment(
        session: Session,
        idempotency_key: str,
        expect: tuple[int, ...] = (200, 400, 401, 403, 404, 409, 422),
    ) -> tuple[int, object]:
        """POST .../appointments/{id}/cancel -- govde YOK, Idempotency-Key destekli."""
        return session.request(
            "POST",
            f"/api/public/businesses/{business_slug}/appointments/{appointment_id}/cancel",
            headers={"Idempotency-Key": idempotency_key},
            expect=expect,
        )

    def history_items(
        session: Session,
        status_filter: str | None = None,
        expect: tuple[int, ...] = (200,),
    ) -> tuple[int, object, list[dict]]:
        code, body = session.get(
            "/api/customer/appointment-history",
            query={"status": status_filter} if status_filter else None,
            expect=expect,
        )
        items = body.get("items") if isinstance(body, dict) else None
        return code, body, list(items or [])

    def find_appointment(items: list[dict]) -> dict | None:
        return next(
            (
                item
                for item in items
                if item.get("appointmentId") == appointment_id
                and item.get("itemType") == "Appointment"
            ),
            None,
        )

    def owner_appointments() -> list[dict]:
        """Randevuyu ISLETME gozunden oku -- musterinin yanitina GUVENMIYORUZ."""
        moment = datetime.now(timezone.utc)
        _, body = owner.get(
            "/api/business/appointments",
            query={
                "fromUtc": (moment - timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%SZ"),
                "toUtc": (moment + timedelta(days=30)).strftime("%Y-%m-%dT%H:%M:%SZ"),
            },
            expect=(200,),
        )
        return [
            row
            for row in (body.get("appointments") or [])
            if row.get("appointmentId") == appointment_id
        ]

    def patch_cutoff(hours: int) -> None:
        """
        Isletmenin iptal politikasini degistirir.

        DIKKAT: /api/business/settings/profile "PATCH ama davranisi PUT" -- displayName ve
        staffDisplayPolicy gelmezse dogrulama duser (BUSINESS_PROFILE_SETTINGS_INVALID_REQUEST).
        Bu yuzden ONCE GET ile mevcut profili okuyup TUM alanlari geri gonderiyoruz.
        """
        _, current = owner.get("/api/business/settings/profile", expect=(200,))
        if not isinstance(current, dict):
            raise StepFailed(f"GET settings/profile beklenmeyen yanit: {current}")
        owner.patch(
            "/api/business/settings/profile",
            {
                "displayName": current.get("displayName"),
                "description": current.get("description") or "",
                "publicRules": current.get("publicRules") or "",
                "seoTitle": current.get("seoTitle") or "",
                "seoDescription": current.get("seoDescription") or "",
                "staffDisplayPolicy": current.get("staffDisplayPolicy") or "ShowNames",
                "cancellationCutoffHours": hours,
            },
            expect=(200,),
        )

    # -- 26 --------------------------------------- (A) musteri KENDI randevusunu goruyor
    REPORT.start("Musteri randevu gecmisinde randevu var mi")
    _, _, items = history_items(customer)
    mine = find_appointment(items)
    if not mine:
        REPORT.fail(
            f"appointmentId={appointment_id} GET /api/customer/appointment-history icinde YOK "
            f"({len(items)} kayit dondu) -- musteri KENDI randevusunu goremiyor."
        )
        return False
    if mine.get("status") != "Confirmed":
        REPORT.fail(f"status='{mine.get('status')}' -- 'Confirmed' bekleniyordu: {mine}")
        return False
    REPORT.ok(
        f"itemType=Appointment status=Confirmed salon={mine.get('businessDisplayName')} "
        f"({len(items)} kayit)"
    )

    # -- 27 ------------------------------------ (B) [GUVENLIK] BASKA musteri iptal EDEMEZ
    # BU ADIM EN ONEMLISI: sahiplik kontrolu gercekten calisiyor mu?
    # Beklenen 404 -- 403 DEGIL. 403 "bu randevu VAR ama senin degil" bilgisini SIZDIRIR.
    REPORT.start("[GUVENLIK] Baska musteri randevuyu iptal EDEMEZ (404 bekleniyor)")
    attacker = Session(api_url, "attacker")
    attacker.post(
        "/api/auth/register",
        {"email": attacker_email, "password": PASSWORD},
        expect=(200,),
        retry_on_rate_limit=2,
    )
    attacker.post(
        "/api/auth/login",
        {"email": attacker_email, "password": PASSWORD},
        query={"useCookies": "true"},
        expect=(200,),
        retry_on_rate_limit=2,
    )
    status, body = cancel_appointment(attacker, str(uuid.uuid4()))
    error_code = body.get("errorCode") if isinstance(body, dict) else None
    if status == 200:
        REPORT.fail(
            "GUVENLIK ACIGI: HTTP 200 -- BASKA bir musteri bu randevuyu IPTAL ETTI. "
            f"Sahiplik kontrolu CALISMIYOR. Yanit: {body}"
        )
        return False
    if status == 403:
        REPORT.fail(
            f"HTTP 403 ({error_code}) -- iptal engellendi AMA YANLIS kodla: 403, randevunun "
            "VAR OLDUGUNU sizdirir (numaralandirma). Sozlesme: 404 APPOINTMENT_NOT_FOUND."
        )
        return False
    if status != 404 or error_code != "APPOINTMENT_NOT_FOUND":
        REPORT.fail(
            f"HTTP {status} (errorCode={error_code}) -- 404 APPOINTMENT_NOT_FOUND "
            f"bekleniyordu. Govde: {body}"
        )
        return False
    REPORT.ok(f"HTTP 404 APPOINTMENT_NOT_FOUND ({attacker_email} reddedildi)")

    # -- 28 ------------------------- (B2) saldiridan sonra randevu BOZULMAMIS olmali
    REPORT.start("[GUVENLIK] Randevu saldiri sonrasi saglam (Confirmed)")
    survivors = owner_appointments()
    if not survivors:
        REPORT.fail(f"appointmentId={appointment_id} isletme listesinden KAYBOLDU")
        return False
    if survivors[0].get("status") != "Confirmed":
        REPORT.fail(
            f"GUVENLIK ACIGI: 404 donuldu AMA randevunun statusu DEGISTI "
            f"('{survivors[0].get('status')}') -- yan etki var."
        )
        return False
    REPORT.ok("status=Confirmed (yan etki yok)")

    # -- 29 ----------------------------- (C) iptal politikasi: cutoff'u 168 saate cikar
    # KURULUM SECIMI (acikca): randevu ~3 GUN (72 saat) ileride. Cutoff'u 168 saat yaparsak
    # (7 gun = Business.MaxCancellationCutoffHours) randevu KESINLIKLE pencerenin icine duser
    # (now + 168sa > startUtc) -> iptal REDDEDILMELI. Boylece ne randevuyu gecmise cekmek ne
    # de sunucu saatini oynatmak gerekir; kural API uzerinden, urun akisiyla test edilir.
    REPORT.start("Iptal politikasi cutoff=168sa yapiliyor (owner)")
    patch_cutoff(168)
    REPORT.ok("cancellationCutoffHours=168 (randevu 72sa ileride -> pencere ICINDE)")

    # -- 30 ------------------------------ (C2) pencere icindeki randevu IPTAL EDILEMEZ
    REPORT.start("[POLITIKA] Cutoff penceresinde iptal reddediliyor (409 TOO_LATE)")
    status, body = cancel_appointment(customer, str(uuid.uuid4()))
    error_code = body.get("errorCode") if isinstance(body, dict) else None
    if status == 200:
        REPORT.fail(
            "URUN HATASI: HTTP 200 -- iptal politikasi ZORLANMIYOR. cutoff=168sa iken "
            f"randevu saatine 72 saat kala iptal edildi. Yanit: {body}"
        )
        return False
    if status != 409 or error_code != "APPOINTMENT_CANCEL_TOO_LATE":
        REPORT.fail(
            f"HTTP {status} (errorCode={error_code}) -- 409 APPOINTMENT_CANCEL_TOO_LATE "
            f"bekleniyordu. Govde: {body}"
        )
        return False
    cutoff_hours = body.get("cancellationCutoffHours") if isinstance(body, dict) else None
    if cutoff_hours != 168:
        REPORT.fail(
            f"409 dogru AMA cancellationCutoffHours={cutoff_hours!r} -- 168 bekleniyordu. "
            "Bu alan bos gelirse UI 'neden iptal edemiyorum' sorusunu YANITLAYAMAZ. "
            f"Govde: {body}"
        )
        return False
    REPORT.ok("HTTP 409 APPOINTMENT_CANCEL_TOO_LATE, cancellationCutoffHours=168")

    # -- 31 ------------------------------------- (C3) politikayi 0'a (kural yok) geri cek
    REPORT.start("Iptal politikasi cutoff=0 (kural yok) yapiliyor")
    patch_cutoff(0)
    REPORT.ok("cancellationCutoffHours=0")

    # -- 32 ------------------------------ (D) musteri KENDI randevusunu IPTAL EDEBILIYOR
    REPORT.start("Musteri KENDI randevusunu iptal ediyor")
    cancel_key = str(uuid.uuid4())  # adim 34'te AYNI anahtarla tekrar cagrilacak
    status, body = cancel_appointment(customer, cancel_key)
    if status != 200:
        REPORT.fail(
            f"HTTP {status} -- iptal BASARISIZ. Govde: {body}\n"
            "      -> cutoff=0, randevu Confirmed ve cagiran musteri SAHIBI: iptal edilebilmeliydi."
        )
        return False
    if body.get("status") != "Cancelled" or body.get("appointmentId") != appointment_id:
        REPORT.fail(f"HTTP 200 AMA govde beklenmedik: {body}")
        return False
    REPORT.ok(f"HTTP 200 status=Cancelled appointmentId={appointment_id}")

    # -- 33 ---------------------------- (D2) randevu gecmisinde artik Cancelled gorunuyor
    REPORT.start("Musteri gecmisinde randevu Cancelled gorunuyor")
    _, _, items = history_items(customer)
    mine = find_appointment(items)
    if not mine:
        REPORT.fail(
            f"appointmentId={appointment_id} gecmisten KAYBOLDU -- iptal SILME degildir, "
            "musteri gecmisini gormeye devam etmeli."
        )
        return False
    if mine.get("status") != "Cancelled":
        REPORT.fail(f"status='{mine.get('status')}' -- 'Cancelled' bekleniyordu: {mine}")
        return False
    REPORT.ok("itemType=Appointment status=Cancelled")

    # -- 34 -------------------------------------------------------- (E) IDEMPOTENT iptal
    # Iki senaryo birden:
    #   a) AYNI Idempotency-Key -> kayitli sonuc REPLAY edilir
    #   b) YENI anahtar + zaten iptal edilmis randevu -> yine BASARILI (cift tiklama)
    # Sonra MUKERRER ETKI olmadigini isletme listesinden dogruluyoruz.
    REPORT.start("[IDEMPOTENT] Ayni iptal tekrar cagriliyor")
    status, replay = cancel_appointment(customer, cancel_key)
    if status != 200:
        REPORT.fail(
            f"AYNI Idempotency-Key ile tekrar -> HTTP {status} (200 bekleniyordu). "
            f"Govde: {replay}"
        )
        return False
    status, retry = cancel_appointment(customer, str(uuid.uuid4()))
    if status != 200:
        REPORT.fail(
            f"YENI anahtar + zaten iptal edilmis randevu -> HTTP {status} (200 bekleniyordu; "
            f"sozlesme: zaten iptal edilmis randevu icin de BASARILI doner). Govde: {retry}"
        )
        return False
    if (
        replay.get("appointmentId") != appointment_id
        or retry.get("appointmentId") != appointment_id
    ):
        REPORT.fail(f"Idempotent cagrilar BASKA randevu dondu: replay={replay} retry={retry}")
        return False
    duplicates = owner_appointments()
    if len(duplicates) != 1:
        REPORT.fail(
            f"MUKERRER ETKI: randevu isletme listesinde {len(duplicates)} kez var (1 bekleniyordu)"
        )
        return False
    if duplicates[0].get("status") != "Cancelled":
        REPORT.fail(f"Tekrar iptal sonrasi status bozuldu: {duplicates[0].get('status')}")
        return False
    REPORT.ok("iki tekrar cagri da HTTP 200; tek kayit, status=Cancelled (mukerrer etki yok)")

    # -- 35 ---------------------------------- (F) isletme tarafinda da Cancelled gorunuyor
    REPORT.start("Isletme takviminde randevu Cancelled gorunuyor")
    business_rows = owner_appointments()
    if not business_rows:
        REPORT.fail(f"appointmentId={appointment_id} isletme listesinde YOK")
        return False
    if business_rows[0].get("status") != "Cancelled":
        REPORT.fail(
            f"ISLETME HALA '{business_rows[0].get('status')}' goruyor -- musteri iptal etti ama "
            "salonun takvimi guncellenmedi. Salon bos yere bekler (no-show)."
        )
        return False
    REPORT.ok("status=Cancelled (salon takvimi guncel)")

    # -- 36 ------------ (G) [BUG FIX REGRESYONU] ?status filtresi RANDEVULARA da uygulaniyor
    #
    # Eskiden status YALNIZCA taleplere uygulaniyordu; randevulara HIC uygulanmiyordu
    # -> ?status=X gonderilse bile TUM randevular donuyordu.
    #
    # DIKKAT -- IKI YONLU dogrulama sart:
    #   G1: ?status=Confirmed -> iptal edilmis randevu GORUNMEMELI
    #   G2: ?status=Cancelled -> iptal edilmis randevu GORUNMELI
    # G1 TEK BASINA YETMEZ: filtre her status icin BOS liste donse bile G1 GECERDI.
    # Ozelligin gercekten calistigini kanitlayan sey G2'dir.
    REPORT.start("Regresyon: appointment-history ?status filtresi randevulara uygulaniyor")
    status, body, confirmed_items = history_items(customer, "Confirmed", expect=(200, 400, 422))
    if status != 200:
        REPORT.fail(
            f"GET /api/customer/appointment-history?status=Confirmed -> HTTP {status}: {body}\n"
            "      -> URUN HATASI (ayrinti icin asagiya bakin): status,"
            " AppointmentRequestStatus enum'una gore dogrulaniyor; 'Confirmed' bir RANDEVU"
            " statusudur (AppointmentStatus) ve o kapidan GECEMEZ."
        )
        return False
    if find_appointment(confirmed_items):
        REPORT.fail(
            "?status=Confirmed IPTAL EDILMIS randevuyu donuyor -- status filtresi randevulara "
            "UYGULANMIYOR (eski bug geri geldi)."
        )
        return False
    status, body, cancelled_items = history_items(customer, "Cancelled", expect=(200, 400, 422))
    if status != 200:
        REPORT.fail(
            f"GET /api/customer/appointment-history?status=Cancelled -> HTTP {status}: {body}"
        )
        return False
    cancelled = find_appointment(cancelled_items)
    if not cancelled:
        REPORT.fail(
            "?status=Cancelled iptal edilmis randevuyu DONMUYOR. Filtre randevulara "
            "'fail-closed' uygulaniyor olabilir (her status icin BOS liste) -- bu durumda "
            "musteri iptal ettigi randevuyu HICBIR filtrede goremez."
        )
        return False
    if cancelled.get("status") != "Cancelled":
        REPORT.fail(f"?status=Cancelled YANLIS status dondu: {cancelled}")
        return False
    REPORT.ok("Confirmed'da YOK, Cancelled'da VAR -- filtre randevulara da uygulaniyor")

    # ==================================================================================
    # FIYAT YONETIMI (adim 37-46)
    #
    # Salonun EN SIK yaptigi is: fiyat/sure guncellemek. Bu bolum uc soruyu sinar:
    #   1. Guncelleme GERCEKTEN kaliyor mu? (200 OK YALAN SOYLEYEBILIR -- yeniden oku)
    #   2. Fiyat degisimi MEVCUT randevulari BOZUYOR mu? (snapshot iddiasinin kaniti)
    #   3. "PATCH ama davranisi PUT" ucu, KISMI gonderimde veriyi SIFIRLIYOR mu?
    #
    # SOZLESME NOTU (artifacts/openapi/rezsaas-api-v1.json'dan dogrulandi):
    #   BusinessVariantUpdateRequest.durationMinutes ve .priceAmount NULLABLE DEGIL
    #   (int32 / double). Yani gonderilmezlerse .NET onlari 0'a DUSURUR -- alan "atlanmis"
    #   olmaz, SIFIR olur. Bu yuzden kismi gonderim yapisal olarak TEHLIKELIDIR.
    # ==================================================================================

    def read_variant(target_service_id: str, target_variant_id: str) -> dict:
        """Varyanti SUNUCUDAN yeniden okur. PATCH'in 200 donmesine ASLA guvenmiyoruz."""
        _, body = owner.get(
            f"/api/business/services/{target_service_id}/variants/{target_variant_id}",
            expect=(200,),
        )
        if not isinstance(body, dict):
            raise StepFailed(f"GET variant beklenmeyen yanit: {body}")
        return body

    def patch_variant(
        target_service_id: str,
        target_variant_id: str,
        body: dict,
        expect: tuple[int, ...] = (200,),
    ) -> tuple[int, object]:
        return owner.patch(
            f"/api/business/services/{target_service_id}/variants/{target_variant_id}",
            body,
            expect=expect,
        )

    def full_variant_body(
        name: str, duration: int, price: float, currency: str, resource_type: str | None
    ) -> dict:
        """TUZAK 2: bu ucun adi PATCH ama davranisi PUT -- 5 alanin HEPSI her istekte gider."""
        return {
            "name": name,
            "durationMinutes": duration,
            "priceAmount": price,
            "currencyCode": currency,
            "requiredResourceTypeId": resource_type,
        }

    def public_services() -> list[dict]:
        _, body = customer.get(
            f"/api/public/businesses/{business_slug}/profile", expect=(200,)
        )
        return list((body or {}).get("services") or []) if isinstance(body, dict) else []

    variant_name = "Sac Kesimi (30dk)"

    # -- 37 ------------------------------- (A) [SESSIZ NO-OP AVI] fiyat guncellemesi KALICI mi
    # Personel adi guncellemesi TAM OLARAK boyle sessizce calismiyordu: 200 OK donuyor,
    # veritabaninda hicbir sey degismiyordu. Ayni tuzak burada da olabilir -> YENIDEN OKU.
    REPORT.start("[A] Varyant FIYATI guncelleniyor ve kaliyor mu (PATCH -> GET)")
    new_price = 750.0
    status, patched = patch_variant(
        service_id,
        variant_id,
        full_variant_body(variant_name, 30, new_price, "TRY", resource_type_id),
    )
    echoed_price = patched.get("priceAmount") if isinstance(patched, dict) else None
    reread = read_variant(service_id, variant_id)
    if reread.get("priceAmount") != new_price:
        REPORT.fail(
            f"SESSIZ NO-OP: PATCH HTTP {status} (yanitta priceAmount={echoed_price!r}) AMA "
            f"YENIDEN OKUNDUGUNDA priceAmount={reread.get('priceAmount')!r} -- "
            f"{new_price} bekleniyordu. Fiyat guncellemesi KALICI DEGIL: salon fiyati "
            "degistirdigini saniyor, katalog eski fiyatta kaliyor."
        )
        finding(
            "Varyant fiyat guncellemesi kalici degil (PATCH 200 doner, GET eski fiyati verir)."
        )
        return False
    if echoed_price != new_price:
        REPORT.fail(
            f"PATCH yaniti priceAmount={echoed_price!r} dondu ama GET {new_price} veriyor -- "
            "yanit govdesi kaydedilen veriyle TUTARSIZ (istemci yanlis fiyat gosterir)."
        )
        return False
    REPORT.ok(f"500 -> {new_price} TRY; GET ile dogrulandi (yanit ve kayit tutarli)")

    # -- 38 -------------------------------------------- (C) SURE guncellemesi KALICI mi
    # Sure fiyattan BAGIMSIZ dogrulanir: biri calisip digeri sessizce dusebilir.
    # Ayrica fiyatin (750) bu PATCH'te BOZULMADIGINI da kontrol ediyoruz.
    REPORT.start("[C] Varyant SURESI guncelleniyor ve kaliyor mu (PATCH -> GET)")
    new_duration = 45
    patch_variant(
        service_id,
        variant_id,
        full_variant_body(variant_name, new_duration, new_price, "TRY", resource_type_id),
    )
    reread = read_variant(service_id, variant_id)
    if reread.get("durationMinutes") != new_duration:
        REPORT.fail(
            f"SESSIZ NO-OP: sure guncellenmedi -- GET durationMinutes="
            f"{reread.get('durationMinutes')!r}, {new_duration} bekleniyordu."
        )
        finding("Varyant sure guncellemesi kalici degil (PATCH 200 doner, GET eski sureyi verir).")
        return False
    if reread.get("priceAmount") != new_price:
        REPORT.fail(
            f"SURE guncellenirken FIYAT BOZULDU: priceAmount={reread.get('priceAmount')!r} "
            f"({new_price} bekleniyordu). Tam govde gonderildigi halde alanlar birbirini eziyor."
        )
        return False
    REPORT.ok(f"30 -> {new_duration}dk; GET ile dogrulandi (fiyat {new_price} bozulmadi)")

    # -- 39 --------------------- (B) [SNAPSHOT] fiyat degisimi MEVCUT randevuyu ETKILEMEMELI
    # Adim 24'te randevunun line fiyatini (500 TRY / 30dk) DOGDUGU ANDA yakalamistik.
    # O gunden beri katalog fiyatini 750'ye, sureyi 45dk'ya cektik.
    # IDDIA: AppointmentLine kendi PriceAmount/CurrencyCode/DurationMinutes alanini TASIR
    #        (talep aninda sunucuda snapshot'lanir) -> randevu DEGISMEMIS olmali.
    # Bu adim o iddianin CANLI KANITI. Degisiyorsa: gecmis randevular retroaktif olarak
    # yeniden fiyatlaniyor demektir -- musteri 500'e aldigi randevunun 750 oldugunu gorur.
    REPORT.start("[B] [SNAPSHOT] Fiyat degisimi MEVCUT randevuyu etkilemedi mi")
    rows = owner_appointments()
    if not rows:
        REPORT.fail(f"appointmentId={appointment_id} isletme listesinde YOK -- snapshot okunamiyor")
        return False
    current_lines = rows[0].get("lines") or []
    current_line = next(
        (line for line in current_lines if line.get("serviceVariantId") == variant_id), None
    )
    if not current_line:
        REPORT.fail(f"Randevunun lines[] icinde variantId={variant_id} YOK: {current_lines}")
        return False
    drifted: list[str] = []
    if current_line.get("priceAmount") != booked_price:
        drifted.append(
            f"priceAmount {booked_price} -> {current_line.get('priceAmount')}"
        )
    if current_line.get("durationMinutes") != booked_duration:
        drifted.append(
            f"durationMinutes {booked_duration} -> {current_line.get('durationMinutes')}"
        )
    if current_line.get("currencyCode") != booked_currency:
        drifted.append(
            f"currencyCode {booked_currency} -> {current_line.get('currencyCode')}"
        )
    if drifted:
        REPORT.fail(
            "URUN HATASI -- SNAPSHOT YOK: katalog fiyati/suresi degisince MEVCUT randevunun "
            f"line'i DA degisti ({'; '.join(drifted)}). AppointmentLine katalogdan CANLI "
            "okuyor demektir. Sonuc: gecmis randevular retroaktif yeniden fiyatlanir; "
            "musteri 500 TRY'ye aldigi randevunun 750 TRY oldugunu gorur."
        )
        finding(
            "Fiyat snapshot'i YOK: katalog fiyati degisince mevcut randevunun line fiyati da degisiyor."
        )
        return False
    REPORT.ok(
        f"katalog 500->750 TRY / 30->45dk oldu; randevu line'i {booked_price} "
        f"{booked_currency}/{booked_duration}dk olarak SABIT kaldi (snapshot DOGRULANDI)"
    )

    # -- 40 ------------------------ (D) [PUT-GIBI-PATCH TUZAGI] KISMI gonderim ne yapiyor?
    # SADECE priceAmount gonderiyoruz; name/durationMinutes/currencyCode/requiredResourceTypeId YOK.
    # Sozlesmede durationMinutes ve priceAmount NULLABLE DEGIL -> .NET onlari 0'a dusurur.
    # BEKLENEN: 400 (name bos + duration 0 dogrulamayi gecemez).
    # KORKULAN: 200 + varyantin adi/suresi/kaynak tipi SIFIRLANMIS -> SESSIZ VERI KAYBI.
    REPORT.start("[D] KISMI PATCH (sadece priceAmount) reddediliyor mu (400 bekleniyor)")
    before = read_variant(service_id, variant_id)
    status, body = patch_variant(
        service_id,
        variant_id,
        {"priceAmount": 1234.0},
        expect=(200, 400, 409, 422),
    )
    after = read_variant(service_id, variant_id)

    if status != 200:
        # Reddedildi. Yine de veriye DOKUNULMADIGINI kanitlayalim -- 400 donup yan etki
        # birakan uclar gordük; "reddedildi" ile "degismedi" AYNI SEY DEGIL.
        if after != before:
            changed = {
                key: (before.get(key), after.get(key))
                for key in before
                if before.get(key) != after.get(key)
            }
            REPORT.fail(
                f"HTTP {status} ile REDDEDILDI ama varyant YINE DE DEGISTI: {changed}. "
                "Basarisiz dogrulama yan etki birakiyor."
            )
            return False
        error_code = body.get("errorCode") if isinstance(body, dict) else None
        REPORT.ok(
            f"HTTP {status} (errorCode={error_code}) -- kismi gonderim reddedildi, veri saglam. "
            "Not: alanlar nullable OLMADIGI icin eksik alanlar 0/null'a duser ve dogrulamaya "
            "TAKILIR; koruma bu kazadan geliyor. UI yine de 5 alani HER ZAMAN gondermeli."
        )
    else:
        # 200 DONDU -> alanlar sifirlanmis mi? Bu bir VERI KAYBI bug'idir.
        corrupted = {
            key: (before.get(key), after.get(key))
            for key in ("name", "durationMinutes", "currencyCode", "requiredResourceTypeId")
            if before.get(key) != after.get(key)
        }
        if corrupted:
            details = "; ".join(
                f"{key}: {old!r} -> {new!r}" for key, (old, new) in corrupted.items()
            )
            REPORT.fail(
                "CIDDI VERI KAYBI BUG'I: kismi PATCH (sadece priceAmount) HTTP 200 dondu ve "
                f"GONDERILMEYEN alanlari EZDI -> {details}. "
                "Sebep: BusinessVariantUpdateRequest'te durationMinutes/priceAmount nullable "
                "DEGIL; gonderilmeyen alanlar 0/null'a duser ve dogrudan yazilir "
                "(ServiceVariantManagementService.cs:143-146 Rename/UpdateDuration/UpdatePricing/"
                "UpdateResourceType kosulsuz cagriliyor). Tek alan guncellemek isteyen her "
                "istemci varyanti SESSIZCE bozar."
            )
            finding(
                "Kismi PATCH varyant alanlarini eziyor (VERI KAYBI): "
                f"{details}"
            )
            return False
        REPORT.fail(
            "HTTP 200 -- kismi gonderim KABUL EDILDI (400 bekleniyordu). Alanlar bu sefer "
            "bozulmadi ama uc, eksik govdeyi sessizce kabul ediyor: sozlesme belirsiz."
        )
        return False

    # -- 41 ---------------------- (F) [PARA BIRIMI] PATCH ile currencyCode DEGISTIRILEBILIYOR mu
    # Bilgi toplama adimi -- testi DUSURMEZ.
    REPORT.start("[F] Varyantin para birimi PATCH ile degistirilebiliyor mu")
    status, body = patch_variant(
        service_id,
        variant_id,
        full_variant_body(variant_name, new_duration, new_price, "USD", resource_type_id),
        expect=(200, 400, 409, 422),
    )
    after = read_variant(service_id, variant_id)
    stored_currency = after.get("currencyCode")
    if status == 200 and stored_currency == "USD":
        REPORT.info(
            "PATCH currencyCode='USD' -> HTTP 200 ve KAYDEDILDI. ISO whitelist YOK: ayni "
            "isletmede varyantlar farkli para biriminde olabilir -> KATALOG TUTARSIZLIGI riski."
        )
        finding(
            "Varyant para birimi serbestce degistirilebiliyor (whitelist yok) -> katalog "
            "tutarsizligi riski. UI'da para birimi secici KOYULMAMALI; her zaman TRY gonderilmeli."
        )
    elif status == 200 and stored_currency != "USD":
        REPORT.info(
            f"SESSIZ NO-OP: PATCH currencyCode='USD' -> HTTP 200 AMA kayitli deger hala "
            f"'{stored_currency}'. Uc, currencyCode'u DOGRULUYOR (bos olamaz) ama HIC UYGULAMIYOR "
            "-- ServiceVariantManagementService.cs:143-146 yalnizca Rename/UpdateDuration/"
            "UpdatePricing/UpdateResourceType cagiriyor; ServiceVariant'ta CurrencyCode'u "
            "degistiren bir metot YOK (setter private, sadece ctor'da atanir)."
        )
        finding(
            "SESSIZ NO-OP -- PATCH .../variants/{id} 'currencyCode' alanini kabul edip DOGRULUYOR "
            "ama ASLA UYGULAMIYOR (200 OK, deger degismez). "
            "src/Modules/RezSaaS.Modules.Catalog/Application/ServiceVariantManagementService.cs:124 "
            "dogrular, :143-146 uygulamaz; Domain/ServiceVariant.cs'te UpdateCurrency yok. "
            "Sozlesme yalan soyluyor: alan zorunlu ama etkisiz."
        )
    else:
        error_code = body.get("errorCode") if isinstance(body, dict) else None
        REPORT.info(
            f"PATCH currencyCode='USD' -> HTTP {status} (errorCode={error_code}) REDDEDILDI. "
            f"Kayitli deger: '{stored_currency}' (degismedi)."
        )

    # Katalogu TEMIZ birak: para birimini TRY'ye geri cek (tuzak 3 -- her zaman TRY).
    patch_variant(
        service_id,
        variant_id,
        full_variant_body(variant_name, new_duration, new_price, "TRY", resource_type_id),
        expect=(200, 400),
    )

    # -- 42 -------------------- (F2) YENI varyant "USD" ile OLUSTURULABILIYOR mu (whitelist var mi)
    # PATCH currency'yi uygulamiyor olabilir; ama CREATE yolu uyguluyor. Asil tutarsizlik
    # riski ORADA. Bilgi toplama adimi -- testi DUSURMEZ.
    REPORT.start("[F] YENI varyant 'USD' para birimiyle olusturulabiliyor mu")
    status, body = owner.post(
        f"/api/business/services/{service_id}/variants",
        full_variant_body("Para Birimi Testi", 30, 99.0, "USD", resource_type_id),
        expect=(201, 400, 409, 422),
    )
    if status == 201 and isinstance(body, dict):
        usd_variant_id = body["id"]
        created = read_variant(service_id, usd_variant_id)
        if created.get("currencyCode") == "USD":
            REPORT.info(
                "HTTP 201 -- 'USD' KABUL EDILDI ve kaydedildi. ISO whitelist YOK "
                "(ServiceVariant.cs:40 sadece trim+upper yapiyor). Ayni isletmede varyant A "
                "'TRY', varyant B 'USD' olabilir -> KATALOG TUTARSIZLIGI. Public profil ve "
                "randevu line'lari bu kodu oldugu gibi tasir."
            )
            finding(
                "CurrencyCode'da ISO whitelist YOK: yeni varyant 'USD' (veya herhangi bir 3-karakter "
                "string) ile olusturulabiliyor -> ayni isletmenin katalogu karisik para birimine "
                "dusebilir. UI para birimi secici ACMAMALI; sabit 'TRY' gondermeli."
            )
        else:
            REPORT.info(
                f"HTTP 201 ama kayitli para birimi '{created.get('currencyCode')}' "
                "-- gonderilen 'USD' degildi."
            )
        # Katalogu temiz birak -- Variant'ta DELETE VAR (Service'te yok).
        owner.request(
            "DELETE",
            f"/api/business/services/{service_id}/variants/{usd_variant_id}",
            expect=(200, 204),
        )
    else:
        error_code = body.get("errorCode") if isinstance(body, dict) else None
        REPORT.info(
            f"HTTP {status} (errorCode={error_code}) -- 'USD' REDDEDILDI. Para birimi "
            "dogrulamasi VAR; katalog tutarsizligi riski yok."
        )

    # ==================================================================================
    # (E) ARSIVLEME (adim 43-46)
    #
    # DIKKAT: ana hizmeti (service_id) ARSIVLEMIYORUZ -- onun uzerinde dogmus bir randevu ve
    # onceki 36 adimin durumu var. Task geregi AYRI bir hizmet acip ONU arsivliyoruz.
    # ==================================================================================

    # -- 43 ------------------- (E1) arsiv testi icin AYRI hizmet + varyant; public'te GORUNUYOR
    # Once "arsivlemeden ONCE GORUNUYOR" olmasini kanitlamaliyiz: yoksa adim 46'nin
    # "gorunmuyor" sonucu HICBIR SEY ISPATLAMAZ (hic gorunmemis de olabilir).
    REPORT.start("[E] Arsiv testi icin ayri hizmet+varyant; public profilde GORUNUYOR mu")
    _, body = owner.post(
        "/api/business/services",
        {"name": f"Arsiv Testi {unique}", "categoryKey": "hair"},
        expect=(201,),
    )
    archive_service_id = body["id"]
    _, body = owner.post(
        f"/api/business/services/{archive_service_id}/variants",
        full_variant_body("Arsiv Varyanti", 30, 250.0, "TRY", resource_type_id),
        expect=(201,),
    )
    archive_variant_id = body["id"]
    listed = public_services()
    if not any(item.get("id") == archive_service_id for item in listed):
        REPORT.fail(
            f"Yeni hizmet (id={archive_service_id}) public profilde GORUNMUYOR "
            f"({len(listed)} hizmet dondu) -- arsivleme testi anlamsizlasir."
        )
        return False
    REPORT.ok(f"serviceId={archive_service_id} public profilde gorunuyor ({len(listed)} hizmet)")

    # -- 44 --------------------------- (E2) VARYANTLI hizmet arsivlenebiliyor mu
    # Fiyat ve sure VARYANTTA yasar -> GERCEK bir hizmetin varyanti HER ZAMAN vardir.
    # Eger arsivleme "once tum varyantlari sil" diyorsa, ozellik pratikte KULLANILAMAZ.
    REPORT.start("[E] VARYANTLI hizmet arsivlenebiliyor mu")
    status, body = owner.post(
        f"/api/business/services/{archive_service_id}/archive",
        expect=(200, 400, 404, 409, 422),
    )
    error_code = body.get("errorCode") if isinstance(body, dict) else None
    variants_block_archive = status != 200
    if variants_block_archive:
        REPORT.fail(
            f"URUN HATASI: HTTP {status} (errorCode={error_code}) -- varyanti OLAN hizmet "
            "ARSIVLENEMIYOR. Ama fiyat/sure VARYANTTA yasar: gercek bir hizmetin varyanti "
            "HER ZAMAN vardir. Yani 'Arsivle' pratikte HICBIR gercek hizmette calismaz; "
            "salonun once TUM varyantlari (yani fiyat gecmisini) SILMESI gerekir. "
            "Kaynak: ServiceManagementService.cs:149-153 -- hasVariants ise ServiceHasVariants "
            "donuyor. Duzeltme: arsivleme varyantlari da ARSIVLEMELI (silmemeli)."
        )
        finding(
            "'Arsivle' varyanti olan hizmette CALISMIYOR (409 SERVICE_HAS_VARIANTS). "
            "ServiceManagementService.cs:149-153. Fiyat/sure varyantta yasadigi icin her gercek "
            "hizmetin varyanti vardir -> ozellik pratikte kullanilamaz; once tum varyantlari "
            "silmek gerekir (fiyat gecmisi yok olur)."
        )
    else:
        REPORT.ok(f"HTTP 200 -- varyantli hizmet dogrudan arsivlendi (status={body.get('status')})")

    # -- 45 ------------------------- (E3) Arsivleme SOFT mu, yoksa HARD DELETE mi?
    # Uc'un adi 'archive'. Adi 'archive' olan bir uc, kaydi KALICI OLARAK SILIYORSA bu
    # geri alinamaz bir veri kaybidir ve UI'daki "Arsivle" butonu YALAN SOYLER.
    # Ayirt edici test: arsivden SONRA GET /api/business/services/{id}
    #    200 + status='Archived'  -> DOGRU (soft archive)
    #    404                      -> HARD DELETE (kayit YOK OLDU)
    REPORT.start("[E] Arsivleme SOFT mu HARD DELETE mi (arsiv sonrasi GET)")
    if variants_block_archive:
        # Arsivleyebilmek icin varyanti silmek ZORUNDAYIZ (E2'nin ta kendisi olan bug).
        owner.request(
            "DELETE",
            f"/api/business/services/{archive_service_id}/variants/{archive_variant_id}",
            expect=(200, 204),
        )
        status, body = owner.post(
            f"/api/business/services/{archive_service_id}/archive",
            expect=(200, 400, 404, 409, 422),
        )
        if status != 200:
            error_code = body.get("errorCode") if isinstance(body, dict) else None
            REPORT.fail(
                f"Varyantlar SILINDIKTEN sonra bile arsivleme basarisiz: HTTP {status} "
                f"(errorCode={error_code}). Hizmet HICBIR SEKILDE arsivlenemiyor. Govde: {body}"
            )
            return False

    status, body = owner.get(
        f"/api/business/services/{archive_service_id}", expect=(200, 404)
    )
    if status == 404:
        REPORT.fail(
            "URUN HATASI -- 'ARSIVLEME' ASLINDA KALICI SILME: arsivden sonra "
            f"GET /api/business/services/{archive_service_id} -> HTTP 404. Kayit YOK OLDU. "
            "Kaynak: ServiceManagementService.cs:155 `dbContext.Services.Remove(service)` "
            "(hard delete). Oysa domain'de Service.Archive() VAR "
            "(Domain/Service.cs:51-54, Status=ServiceStatus.Archived) ama HIC CAGRILMIYOR -- "
            "OLU KOD. Karsilastirma: StaffManagementService.cs:186 staff.Archive() dogru sekilde "
            "cagiriyor, Catalog cagirmiyor. SONUC: (1) arsivleme GERI ALINAMAZ; "
            "(2) ServiceStatus.Archived asla olusamayacagi icin PublicCatalogMenuService.cs:31 "
            "'Status == Active' filtresi OLU; (3) UI'daki 'Arsivle' butonu kullaniciya YALAN "
            "soyler -- aslinda 'Kalici Sil'dir. Duzeltme: Remove yerine service.Archive() cagirin."
        )
        finding(
            "'Arsivle' ucu ASLINDA HARD DELETE: ServiceManagementService.cs:155 "
            "`dbContext.Services.Remove(service)`. Domain'deki Service.Archive() "
            "(Domain/Service.cs:51) HIC cagrilmiyor -- olu kod; ServiceStatus.Archived asla "
            "olusmaz; PublicCatalogMenuService.cs:31'deki 'Status == Active' filtresi bu yuzden "
            "olu. Geri alinamaz veri kaybi + UI'da yaniltici buton."
        )
        # DUSTUK ama devam ediyoruz: adim 46 (public profilde gorunmuyor mu) yine de
        # calisabilir ve BAGIMSIZ bir gercegi olcer. Sonuc yine exit 1 olacak.
        archive_is_hard_delete = True
    else:
        service_status = body.get("status") if isinstance(body, dict) else None
        if service_status != "Archived":
            REPORT.fail(
                f"Arsivden sonra GET 200 dondu ama status='{service_status}' -- 'Archived' "
                f"bekleniyordu. Govde: {body}"
            )
            return False
        archive_is_hard_delete = False
        REPORT.ok("HTTP 200 status='Archived' -- dogru soft archive (kayit korunuyor)")

    # -- 46 ------------------------- (E4) arsivlenen hizmet public profilde GORUNMEMELI
    # Bu adim, arsivin HARD DELETE olmasindan BAGIMSIZ olarak anlamli: musteri arsivlenen
    # hizmeti gormemeli. (Hard delete'te de gorunmez -- ama yanlis sebeple.)
    REPORT.start("[E] Arsivlenen hizmet public profilde GORUNMUYOR mu")
    listed = public_services()
    still_there = next((i for i in listed if i.get("id") == archive_service_id), None)
    if still_there:
        REPORT.fail(
            f"URUN HATASI: arsivlenen hizmet (id={archive_service_id}) public profilde HALA "
            f"GORUNUYOR: {still_there}. Musteri artik sunulmayan bir hizmeti secip randevu "
            "talep edebilir."
        )
        finding("Arsivlenen hizmet public profilde gorunmeye devam ediyor.")
        return False
    reason = (
        "(ama KAYIT SILINDIGI icin -- bkz. adim 45)"
        if archive_is_hard_delete
        else "(soft archive filtresi calisiyor)"
    )
    REPORT.ok(f"public profilde YOK {reason} ({len(listed)} hizmet kaldi)")

    # ==================================================================================
    # PERSONEL YONETIMI (adim 47-53)
    #
    # DIKKAT (koddan/sozlesmeden dogrulandi):
    #   - Personel SUBE ALTINDA nested: .../branches/{branchId}/staff/{staffId}.
    #   - Personel = TAKVIM KAYNAGI (login DEGIL) -> sadece displayName; userAccountId
    #     dogrulanmaz, UI'da GOSTERILMEZ.
    #   - Yetkinlik atamasinin GET'i YOK (write-only) -> burada okunmaz/sinanmaz.
    #   - Personel bazli calisma saati YOK; ama IZIN/MUSAITSIZLIK OKUNABILIR (liste var).
    #
    # Bu blok, personel yasam dongusunun yazdiktan SONRA gercekten kaldigini (rename),
    # izin ekle/oku/sil dongusunu, iznin slotlari bloklayip bloklamadigini ve arsivlemenin
    # aktif randevu + public gorunurluk davranisini sinar.
    #
    # NEDEN SONA EKLENDI: bu adimlar personeli ARSIVLER (geri donusu olmayan durum) ve
    # aktif randevu icin YENI bir randevu dogurur. Onceki 46 adimin AYNEN calismasini
    # garanti etmek icin hepsi tamamlandiktan SONRA calisirlar. Adim 24'te dogan randevu
    # 32'de IPTAL edildiginden "aktif randevu" senaryosu (D) icin YENI bir Confirmed
    # randevu doguruyoruz -- ayni personel, ayni varyant.
    # ==================================================================================

    def public_profile() -> dict:
        _, body = customer.get(
            f"/api/public/businesses/{business_slug}/profile", expect=(200,)
        )
        return body if isinstance(body, dict) else {}

    def public_staff_ids() -> set:
        ids = set()
        for branch in public_profile().get("branches") or []:
            for member in branch.get("staffMembers") or []:
                if member.get("id"):
                    ids.add(member["id"])
        return ids

    def read_staff() -> dict:
        _, body = owner.get(
            f"/api/business/branches/{branch_id}/staff/{staff_id}", expect=(200,)
        )
        if not isinstance(body, dict):
            raise StepFailed(f"GET staff beklenmeyen yanit: {body}")
        return body

    def staff_slots(date_iso: str) -> list[dict]:
        _, body = customer.get(
            f"/api/public/businesses/{business_slug}/slots",
            query={
                "branchSlug": branch_slug,
                "date": date_iso,
                "serviceVariantIds": variant_id,
            },
            expect=(200,),
        )
        return list(body.get("slots") or []) if isinstance(body, dict) else []

    def slots_with_staff(slots: list[dict], target_staff_id: str) -> int:
        count = 0
        for slot in slots:
            if any(c.get("id") == target_staff_id for c in slot.get("staffCandidates") or []):
                count += 1
        return count

    def find_appointment_row(target_id: str) -> dict | None:
        moment = datetime.now(timezone.utc)
        _, body = owner.get(
            "/api/business/appointments",
            query={
                "fromUtc": (moment - timedelta(days=1)).strftime("%Y-%m-%dT%H:%M:%SZ"),
                "toUtc": (moment + timedelta(days=60)).strftime("%Y-%m-%dT%H:%M:%SZ"),
            },
            expect=(200,),
        )
        rows = (body.get("appointments") or []) if isinstance(body, dict) else []
        return next((r for r in rows if r.get("appointmentId") == target_id), None)

    # -- 47 --------------------- (A) [RENAME REGRESYONU] ad guncellemesi KALICI mi (PATCH->GET)
    # f757cee'de duzeltilen bug: PATCH 200 doner ama ad degismezdi. 200'e GUVENMIYORUZ.
    REPORT.start("[A] Personel adi PATCH ile degisiyor ve KALIYOR mu (rename regresyonu)")
    new_staff_name = "Ayse Usta (Kidemli)"
    _, patched = owner.patch(
        f"/api/business/branches/{branch_id}/staff/{staff_id}",
        {"displayName": new_staff_name},
        expect=(200,),
    )
    echoed_name = patched.get("displayName") if isinstance(patched, dict) else None
    reread_staff = read_staff()
    if reread_staff.get("displayName") != new_staff_name:
        REPORT.fail(
            f"SESSIZ NO-OP / REGRESYON: PATCH HTTP 200 (yanit displayName={echoed_name!r}) AMA "
            f"YENIDEN OKUNDUGUNDA displayName={reread_staff.get('displayName')!r} -- "
            f"{new_staff_name!r} bekleniyordu. Personel adi guncellemesi KALICI DEGIL "
            "(f757cee regresyonu geri geldi)."
        )
        finding(
            "Personel adi guncellemesi kalici degil (PATCH 200 doner, GET eski adi verir) "
            "-- f757cee regresyonu geri geldi."
        )
        return False
    if echoed_name != new_staff_name:
        REPORT.fail(
            f"PATCH yaniti displayName={echoed_name!r} ama GET {new_staff_name!r} veriyor "
            "-- yanit govdesi kayitla tutarsiz."
        )
        return False
    REPORT.ok(f"'Ayse Usta' -> {new_staff_name!r}; GET ile dogrulandi")

    # -- 48 --------------------------------- (B) izin EKLE + GET ile DOGRULA
    REPORT.start("[B] Personele izin ekleniyor ve GET ile dogrulaniyor")
    leave_day = (datetime.now(timezone.utc) + timedelta(days=20)).date()
    leave_start = f"{leave_day.isoformat()}T00:00:00Z"
    leave_end = f"{(leave_day + timedelta(days=1)).isoformat()}T00:00:00Z"
    _, created = owner.post(
        f"/api/business/staff/{staff_id}/unavailable",
        {"startUtc": leave_start, "endUtc": leave_end, "reason": "Yillik izin"},
        expect=(201,),
    )
    unavailable_id = created.get("id") if isinstance(created, dict) else None
    if not unavailable_id:
        REPORT.fail(f"POST unavailable yanitinda id YOK: {created}")
        return False
    _, listed_leave = owner.get(f"/api/business/staff/{staff_id}/unavailable", expect=(200,))
    leave_rows = listed_leave if isinstance(listed_leave, list) else []
    leave_match = next((r for r in leave_rows if r.get("id") == unavailable_id), None)
    if not leave_match:
        REPORT.fail(
            f"Eklenen izin (id={unavailable_id}) GET listesinde YOK "
            f"({len(leave_rows)} kayit): {leave_rows}"
        )
        return False
    if leave_match.get("reason") != "Yillik izin":
        REPORT.fail(f"Izin kaydinin reason'i beklenmedik: {leave_match}")
        return False
    REPORT.ok(f"unavailableId={unavailable_id} eklendi, GET dogruladi ({len(leave_rows)} kayit)")

    # -- 49 --------------------------------- (B) izin SIL + GET ile GITTIGINI DOGRULA
    REPORT.start("[B] Izin DELETE ile siliniyor ve GET ile gittigi dogrulaniyor")
    owner.request(
        "DELETE",
        f"/api/business/staff/{staff_id}/unavailable/{unavailable_id}",
        expect=(200, 204),
    )
    _, listed_leave = owner.get(f"/api/business/staff/{staff_id}/unavailable", expect=(200,))
    leave_rows = listed_leave if isinstance(listed_leave, list) else []
    if any(r.get("id") == unavailable_id for r in leave_rows):
        REPORT.fail(
            f"Silinen izin (id={unavailable_id}) GET listesinde HALA VAR: {leave_rows}"
        )
        return False
    REPORT.ok(f"izin silindi, GET listesinde yok ({len(leave_rows)} kayit kaldi)")

    # -- 50 ------------------- (C) [IZIN SLOT'U BLOKLUYOR MU?] YARIN tam gun izin -> public slot
    # KURULUM (acikca): sube tz Europe/Istanbul, calisma 09:00-19:00 yerel (= 06:00-16:00Z).
    # YARIN icin UTC tam gun [00:00Z, +1gun 00:00Z) izin veriyoruz; bu, yarinin tum yerel
    # calisma saatlerini kapsar. Tek personel oldugu icin YARIN personel adayli slot KALMAMALI.
    REPORT.start("[C] Personele YARIN tam gun izin -> public slotlar YARIN bloklaniyor mu")
    tomorrow = (datetime.now(timezone.utc) + timedelta(days=1)).date()
    before_slots = staff_slots(tomorrow.isoformat())
    before_count = slots_with_staff(before_slots, staff_id)
    block_start = f"{tomorrow.isoformat()}T00:00:00Z"
    block_end = f"{(tomorrow + timedelta(days=1)).isoformat()}T00:00:00Z"
    owner.post(
        f"/api/business/staff/{staff_id}/unavailable",
        {"startUtc": block_start, "endUtc": block_end, "reason": "Tam gun izin"},
        expect=(201,),
    )
    after_slots = staff_slots(tomorrow.isoformat())
    after_count = slots_with_staff(after_slots, staff_id)
    c_detail = (
        f"YARIN={tomorrow.isoformat()} izin(UTC)=[{block_start},{block_end}) "
        f"personel adayli slot: once={before_count} sonra={after_count} "
        f"(tz Europe/Istanbul, 09:00-19:00 yerel = 06:00-16:00Z izin araligindadir)"
    )
    if before_count == 0:
        REPORT.info(f"Baz slot yok -> izin etkisi olculemez. {c_detail}")
        finding(
            "[C] Personel izni slot etkisi OLCULEMEDI: izinden ONCE de yarin icin personel "
            "adayli slot yoktu (baz sifir)."
        )
    elif after_count == 0:
        REPORT.ok(f"izin personeli YARINki TUM slotlardan cikardi. {c_detail}")
    elif after_count < before_count:
        REPORT.ok(f"izin personelin YARINki slotlarini AZALTTI. {c_detail}")
    else:
        REPORT.info(f"izin YARINki slotlari DEGISTIRMEDI. {c_detail}")
        finding(
            "[C] Personel tam-gun izni public slot aramasini DEGISTIRMEDI -- izin/musaitsizlik "
            "slot motoruna yansimiyor olabilir (urun riski; slot motoru StaffUnavailable'i "
            "dikkate almiyor olabilir, dogrulanmali)."
        )

    # -- 51 ------------------------- (D-hazirlik) arsiv testi icin YENI Confirmed randevu
    # Adim 24'un randevusu 32'de iptal edildi; 'aktif randevu' senaryosu icin taze randevu lazim.
    REPORT.start("[D] Arsiv testi icin YENI Confirmed randevu doguruluyor")
    appt2_date = (datetime.now(timezone.utc) + timedelta(days=6)).date().isoformat()
    d_slots = staff_slots(appt2_date)
    d_cutoff = datetime.now(timezone.utc) + timedelta(hours=3)
    chosen2 = None
    for slot in d_slots:
        start = datetime.fromisoformat(slot["startUtc"].replace("Z", "+00:00"))
        if start > d_cutoff and any(
            c.get("id") == staff_id for c in slot.get("staffCandidates") or []
        ):
            chosen2 = slot
            break
    if not chosen2:
        REPORT.fail(
            f"Arsiv testi icin uygun slot yok (tarih={appt2_date}, {len(d_slots)} slot; "
            f"personel={staff_id} adayli ve response buffer disinda slot bulunamadi)."
        )
        return False
    start2 = chosen2["startUtc"]
    _, body = customer.post(
        f"/api/public/businesses/{business_slug}/appointment-requests",
        {
            "branchSlug": branch_slug,
            "serviceVariantIds": [variant_id],
            "staffMemberId": staff_id,
            "startUtc": start2,
        },
        headers={"Idempotency-Key": str(uuid.uuid4())},
        expect=(201,),
    )
    req2_id = body.get("appointmentRequestId") if isinstance(body, dict) else None
    _, body = owner.post(
        f"/api/business/appointment-requests/{req2_id}/approve",
        headers={"Idempotency-Key": str(uuid.uuid4())},
        expect=(200,),
    )
    appt2_id = body.get("appointmentId") if isinstance(body, dict) else None
    if not appt2_id:
        REPORT.fail(f"Onay appointmentId donmedi: {body}")
        return False
    row2 = find_appointment_row(appt2_id)
    if not row2 or row2.get("status") != "Confirmed":
        REPORT.fail(f"Yeni randevu Confirmed degil / listede yok: {row2}")
        return False
    REPORT.ok(
        f"appointmentId={appt2_id} Confirmed (start={start2}, "
        f"personel={row2.get('staffMemberDisplayName')!r})"
    )

    # -- 52 -------------- (D) [ARSIVLEME + AKTIF RANDEVU] backend aktif randevuyu koruyor mu?
    # BEKLENTI (analiz): ArchiveAsync gelecekteki randevu kontrolu YAPMIYOR -> 200 doner ve
    # randevu sahipsiz/arsivli personele bagli kalir. Backend 409 REDDEDIYORSA analiz yanilir.
    # [D] REGRESYON: aktif randevusu OLAN personel arsivlenemez -> 409.
    # (Bu bir bug'di: ArchiveAsync hicbir kontrol yapmadan arsivliyordu, randevu sahipsiz
    #  kaliyordu. Duzeltildi: IStaffAppointmentGuard ile gelecek randevu kontrol ediliyor.)
    REPORT.start("[D] Aktif randevulu personel arsivlenemez (409 STAFF_HAS_UPCOMING bekleniyor)")
    staff_in_public_before = staff_id in public_staff_ids()  # adim (E) icin BAZ
    status, body = owner.post(
        f"/api/business/branches/{branch_id}/staff/{staff_id}/archive",
        expect=(200, 400, 404, 409, 422),
    )
    d_error_code = body.get("errorCode") if isinstance(body, dict) else None
    row2_after = find_appointment_row(appt2_id)

    if status == 409 and d_error_code == "STAFF_HAS_UPCOMING_APPOINTMENTS":
        # Dogru davranis: reddedildi, randevu SAGLAM.
        if not (row2_after and row2_after.get("status") == "Confirmed"):
            REPORT.fail(
                "409 dondu AMA aktif randevu artik Confirmed degil/kayip -- beklenmeyen yan etki."
            )
            return False
        REPORT.ok(
            "409 STAFF_HAS_UPCOMING_APPOINTMENTS -- gelecek randevulu personel arsivlenmedi, "
            "randevu Confirmed olarak saglam kaldi."
        )
    elif status == 200 and row2_after and row2_after.get("status") == "Confirmed":
        orphan_name = row2_after.get("staffMemberDisplayName")
        REPORT.fail(
            "REGRESYON -- ARSIVLEME AKTIF RANDEVU KONTROLU YAPMIYOR: gelecekte Confirmed "
            f"randevusu (appointmentId={appt2_id}) OLAN personel HTTP 200 ile arsivlendi "
            f"(staffMemberDisplayName={orphan_name!r}) -> randevu SAHIPSIZ. "
            "StaffManagementService.ArchiveAsync gelecek randevu kontrolunu kaybetmis."
        )
        finding("REGRESYON: personel arsivleme aktif randevu kontrolunu kaybetti.")
        return False
    else:
        REPORT.fail(
            f"Beklenmeyen sonuc: HTTP {status} (errorCode={d_error_code}). "
            "Aktif randevulu personel arsivleme 409 STAFF_HAS_UPCOMING_APPOINTMENTS bekliyordu."
        )
        return False

    # Simdi randevuyu IPTAL ET -> personel artik arsivlenebilmeli.
    REPORT.start("[D] Randevu iptal edilince ayni personel ARSIVLENEBILIYOR")
    cancel_status, _ = owner.post(
        f"/api/business/appointments/{appt2_id}/cancel",
        {"reason": "E2E arsivleme testi icin iptal"},
        expect=(200, 204),
    )
    status, body = owner.post(
        f"/api/business/branches/{branch_id}/staff/{staff_id}/archive",
        expect=(200, 400, 404, 409, 422),
    )
    if status == 200:
        REPORT.ok(
            "randevu iptal edildikten sonra personel HTTP 200 ile arsivlendi "
            "(engel kalkti -> kontrol dogru calisiyor)."
        )
        archived_ok = True
    else:
        d_error_code = body.get("errorCode") if isinstance(body, dict) else None
        REPORT.fail(
            f"Randevu iptal edildi ama personel HALA arsivlenemiyor: HTTP {status} "
            f"(errorCode={d_error_code}). Engel gereginden fazla genis olabilir."
        )
        return False

    # -- 53 --------------- (E) [ARSIVLI PERSONEL PUBLIC'TE] arsivlenen personel gizli mi?
    REPORT.start("[E] Arsivlenen personel public profilde GORUNMUYOR mu")
    if not archived_ok:
        REPORT.info(
            "Personel arsivlenemedi (adim [D] reddedildi) -> 'arsivli personel public'te gizli mi' "
            "sorusu bu kosumda test edilemez."
        )
    else:
        staff_in_public_after = staff_id in public_staff_ids()
        if not staff_in_public_before:
            REPORT.info(
                f"Personel ({staff_id}) arsivden ONCE de public profilde YOKTU -> gizlenme "
                f"kaniti uretilemiyor. Arsivden sonra public'te: {staff_in_public_after}"
            )
        elif staff_in_public_after:
            REPORT.fail(
                f"URUN HATASI: arsivlenen personel ({staff_id}) public profilde HALA GORUNUYOR. "
                "Musteri artik calismayan personeli secip randevu talep edebilir."
            )
            finding("Arsivlenen personel public profilde gorunmeye devam ediyor.")
            return False
        else:
            REPORT.ok("arsivden ONCE public'te vardi, SONRA yok -- dogru gizlendi")

    # ==================================================================================
    # KURULUM YUZEYI REGRESYONLARI (adim 54-60)
    #
    # Buraya kadar cekirdek dongu (randevu) + katalog + personel dogrulandi. Bu blok,
    # salon KURULUM yuzeyinin dort kritik davranisini CANLI API'ye karsi sinar:
    #   - Sube TimeZoneId dogrulama + Windows->IANA normalizasyonu (fix 62c8088)
    #   - Isletme profil ayarlari round-trip + cancellationCutoffHours nullable korumasi
    #   - Calisma saati (gun KAPALI) -> public slot bloklama
    #   - slotIntervalMinutes ayari -> slot sikligi
    #   - Kaynak out-of-service -> slot dususu
    #
    # NEDEN SONA EKLENDI ve NEDEN AYRI 'SLOT LAB' SUBESI: onceki 53 adimin durumunu
    # (ozellikle ana subenin fixture'larini ve adim 52-53'te ARSIVLENEN ana personeli)
    # bozmamak icin B/C/E slot testleri KENDI subesini+kaynagini+personelini kurar; ana
    # subeye ve ana personele DOKUNMAZ. Business seviyesindeki variant/skill/resource-type
    # yeniden kullanilir (bunlar sube-bagimsizdir).
    # ==================================================================================

    # Bu blok owner uzerinde YOGUN business cagrisi yapar. TUM /api/business/* uclari
    # BookingRateLimitPolicyNames.BusinessDecisions limitini paylasir: DAKIKADA 60 istek
    # (BookingSecurityOptions.BusinessDecisionPermitLimit=60, Window=1dk). Onceki 53 adim bu
    # tavanin ALTINDA kaliyordu; bu blok +~30 owner cagrisi ekledigi icin 1 dakikalik pencere
    # dolabiliyor. 429 URUN HATASI DEGIL -- kasitli koruma. Bu yuzden bu bloktaki owner (ve
    # public slot arayan customer) cagrilarini, pencere dolarsa bekleyip yeniden denenecek
    # sekilde sararak varsayilan olarak retry_on_rate_limit ekliyoruz. (Kimlik cagrilarindaki
    # ayni yaklasimin business/public limitlere uygulanmasidir; bkz. Session.request.)
    def _wrap_with_retry(session: Session, retries: int = 3) -> None:
        original = session.request

        def retrying(method: str, path: str, **kwargs):
            kwargs.setdefault("retry_on_rate_limit", retries)
            return original(method, path, **kwargs)

        session.request = retrying  # type: ignore[assignment]

    _wrap_with_retry(owner)
    _wrap_with_retry(customer)

    # -- 54 ------------------ (TZ1) [LANSMAN BLOKAJI] gecersiz TimeZoneId reddediliyor mu
    # 62c8088: sube olustururken TimeZoneId artik DOGRULANIYOR. Gecersiz ("Istanbul")
    # -> 400 BRANCH_INVALID_TIMEZONE olmali. 201 donerse fix GERI GITMIS demektir.
    REPORT.start("[TZ] Gecersiz TimeZoneId ('Istanbul') sube olusturmada reddediliyor mu (400)")
    status, body = owner.post(
        "/api/business/branches",
        {
            "slug": f"tz-bad-{unique}",
            "displayName": "TZ Gecersiz",
            "timeZoneId": "Istanbul",  # IANA DEGIL -> reddedilmeli
            "city": "Istanbul",
            "district": "Kadikoy",
            "addressLine": "TZ Testi 1",
        },
        expect=(201, 400, 409, 422),
    )
    tz_err = body.get("errorCode") if isinstance(body, dict) else None
    if status == 201:
        REPORT.fail(
            "REGRESYON (62c8088 geri geldi): timeZoneId='Istanbul' KABUL EDILDI (HTTP 201). "
            "IANA olmayan gecersiz zaman dilimi 400 BRANCH_INVALID_TIMEZONE ile reddedilmeliydi. "
            "Sonuc: randevu saatleri yanlis zaman diliminde hesaplanabilir. Govde: " + str(body)
        )
        finding(
            "Sube TimeZoneId dogrulamasi yok: gecersiz 'Istanbul' kabul ediliyor (62c8088 regresyonu)."
        )
        return False
    if status != 400 or tz_err != "BRANCH_INVALID_TIMEZONE":
        REPORT.fail(
            f"HTTP {status} (errorCode={tz_err}) -- 400 BRANCH_INVALID_TIMEZONE bekleniyordu. "
            f"Govde: {body}"
        )
        return False
    REPORT.ok("HTTP 400 BRANCH_INVALID_TIMEZONE -- gecersiz zaman dilimi reddedildi")

    # -- 55 ----------------- (TZ2) Windows ID kabul + IANA'ya NORMALIZE (62c8088 canli kaniti)
    # timeZoneId="Turkey Standard Time" (Windows) -> 201 (kabul) VE olusan subenin GET'inde
    # timeZoneId artik "Europe/Istanbul" (IANA'ya normalize) olmali. Normalize olmuyorsa NOT dus.
    REPORT.start("[TZ] Windows 'Turkey Standard Time' kabul + IANA'ya normalize (Europe/Istanbul)")
    status, body = owner.post(
        "/api/business/branches",
        {
            "slug": f"tz-win-{unique}",
            "displayName": "TZ Windows",
            "timeZoneId": "Turkey Standard Time",
            "city": "Istanbul",
            "district": "Kadikoy",
            "addressLine": "TZ Testi 2",
        },
        expect=(201, 400, 409, 422),
    )
    if status != 201:
        tz_err = body.get("errorCode") if isinstance(body, dict) else None
        REPORT.info(
            f"Windows ID 'Turkey Standard Time' -> HTTP {status} (errorCode={tz_err}) reddedildi. "
            "62c8088 yalnizca IANA'yi dogruluyor; Windows ID'lerini IANA'ya normalize ETMIYOR olabilir."
        )
        finding(
            "Sube olusturmada Windows zaman dilimi ID'si ('Turkey Standard Time') IANA'ya "
            "normalize edilmiyor, reddediliyor -- 62c8088 normalizasyonu Windows ID'lerini kapsamiyor."
        )
    else:
        tz_branch_id = body["id"]
        _, detail = owner.get(f"/api/business/branches/{tz_branch_id}", expect=(200,))
        stored_tz = detail.get("timeZoneId") if isinstance(detail, dict) else None
        if stored_tz == "Europe/Istanbul":
            REPORT.ok(
                f"HTTP 201; GET timeZoneId='{stored_tz}' -- Windows ID IANA'ya NORMALIZE edildi "
                "(62c8088 canli kaniti)"
            )
        else:
            REPORT.info(
                f"HTTP 201 ama GET timeZoneId='{stored_tz}' -- 'Europe/Istanbul' bekleniyordu; "
                "Windows ID kabul edildi AMA IANA'ya normalize EDILMEDI."
            )
            finding(
                f"Sube TimeZoneId Windows->IANA normalizasyonu eksik: 'Turkey Standard Time' "
                f"-> '{stored_tz}' saklandi (Europe/Istanbul bekleniyordu)."
            )

    # -- 56 ------------------ (AYAR) profil round-trip + cancellationCutoffHours nullable korumasi
    # Serit B: cancellationCutoffHours NULLABLE; gonderilmezse KORUNUR, diger alanlar ise
    # gonderilmezse SIFIRLANIR ("PATCH ama davranisi PUT"). Iki iddiayi da sinariz:
    #   1) TUM alanlar + cutoff=24 gonder -> GET: cutoff=24 VE displayName/seoTitle DEGISMEMIS
    #   2) cutoff'u ATLA (digerlerini gonder) -> GET: cutoff HALA 24 (nullable korumasi)
    REPORT.start("[AYAR] Profil round-trip + cancellationCutoffHours nullable korumasi")
    _, before = owner.get("/api/business/settings/profile", expect=(200,))
    if not isinstance(before, dict):
        REPORT.fail(f"GET settings/profile beklenmeyen yanit: {before}")
        return False
    if "cancellationCutoffHours" not in before:
        REPORT.fail(
            "GET /api/business/settings/profile yanitinda cancellationCutoffHours alani YOK "
            "-- Serit B alani GET'te DONMUYOR; UI iptal politikasini gosteremez."
        )
        finding("cancellationCutoffHours GET settings/profile yanitinda yer almiyor (Serit B).")
        return False
    marker_seo = f"E2E SEO {unique}"
    base_name = before.get("displayName")
    # (1) TUM alanlar; seoTitle'a benzersiz marker, cutoff=24
    owner.patch(
        "/api/business/settings/profile",
        {
            "displayName": base_name,
            "description": before.get("description") or "",
            "publicRules": before.get("publicRules") or "",
            "seoTitle": marker_seo,
            "seoDescription": before.get("seoDescription") or "",
            "staffDisplayPolicy": before.get("staffDisplayPolicy") or "ShowNames",
            "cancellationCutoffHours": 24,
        },
        expect=(200,),
    )
    _, mid = owner.get("/api/business/settings/profile", expect=(200,))
    if not isinstance(mid, dict) or mid.get("cancellationCutoffHours") != 24:
        REPORT.fail(
            f"PATCH sonrasi GET cancellationCutoffHours="
            f"{mid.get('cancellationCutoffHours') if isinstance(mid, dict) else mid!r} -- 24 bekleniyordu."
        )
        return False
    if mid.get("seoTitle") != marker_seo or mid.get("displayName") != base_name:
        REPORT.fail(
            f"PATCH (cutoff=24) diger alanlari BOZDU: displayName={mid.get('displayName')!r} "
            f"seoTitle={mid.get('seoTitle')!r} (beklenen displayName={base_name!r} seoTitle={marker_seo!r})."
        )
        return False
    # (2) cancellationCutoffHours'u BILEREK ATLA -> nullable korumasi geregi 24 KALMALI
    owner.patch(
        "/api/business/settings/profile",
        {
            "displayName": base_name,
            "description": mid.get("description") or "",
            "publicRules": mid.get("publicRules") or "",
            "seoTitle": marker_seo,
            "seoDescription": mid.get("seoDescription") or "",
            "staffDisplayPolicy": mid.get("staffDisplayPolicy") or "ShowNames",
            # cancellationCutoffHours GONDERILMIYOR
        },
        expect=(200,),
    )
    _, after = owner.get("/api/business/settings/profile", expect=(200,))
    if not isinstance(after, dict) or after.get("cancellationCutoffHours") != 24:
        REPORT.fail(
            "cancellationCutoffHours NULLABLE KORUMASI CALISMIYOR: alan gonderilmeyince deger "
            f"{after.get('cancellationCutoffHours') if isinstance(after, dict) else after!r} oldu "
            "(24 korunmaliydi). Serit B'deki 'gonderilmezse koru' davranisi bozulmus."
        )
        finding(
            "cancellationCutoffHours gonderilmeyince korunmuyor -- Serit B nullable korumasi bozuk."
        )
        return False
    if after.get("seoTitle") != marker_seo or after.get("displayName") != base_name:
        REPORT.fail(
            f"Ikinci PATCH digerlerini bozdu: displayName={after.get('displayName')!r} "
            f"seoTitle={after.get('seoTitle')!r}."
        )
        return False
    REPORT.ok(
        "cutoff 24'e set edildi (displayName/seoTitle korundu); sonra ATLANINCA 24 KORUNDU "
        "(GET'te donuyor + nullable korumasi calisiyor)"
    )

    # -- SLOT LAB kurulumu (adim 57): ana subeye/personele DOKUNMADAN slot-etkileyen
    # davranislari sinamak icin izole bir sube + kaynak + personel. variant_id/skill_id/
    # resource_type_id business seviyesinde oldugu icin yeniden kullanilir.
    # -- 57 ------------------------------------------------------------------------------
    REPORT.start("[SLOT-LAB] Izole sube+kaynak+personel kur; baz slotlar dogruluyor")
    lab_slug = f"slotlab-{unique}"
    _, body = owner.post(
        "/api/business/branches",
        {
            "slug": lab_slug,
            "displayName": "Slot Lab Sube",
            "timeZoneId": "Europe/Istanbul",
            "city": "Istanbul",
            "district": "Kadikoy",
            "addressLine": "Slot Lab 1",
        },
        expect=(201,),
    )
    lab_branch_id = body["id"]
    for day in days:  # 7 gun 09:00-19:00 acik (days adim 13'te tanimli)
        owner.put(
            f"/api/business/branches/{lab_branch_id}/working-hours/{day}",
            {"opensAt": "09:00", "closesAt": "19:00", "isClosed": False},
            expect=(200,),
        )
    _, body = owner.post(
        f"/api/business/branches/{lab_branch_id}/resources",
        {"resourceTypeId": resource_type_id, "displayName": "Lab Koltuk"},
        expect=(201,),
    )
    lab_resource_id = body["id"]
    _, body = owner.post(
        f"/api/business/branches/{lab_branch_id}/staff",
        {"displayName": "Lab Usta", "userAccountId": None},
        expect=(201,),
    )
    lab_staff_id = body["id"]
    owner.post(
        f"/api/business/staff/{lab_staff_id}/skills", {"skillId": skill_id}, expect=(200,)
    )

    def lab_slots(date_iso: str) -> list[dict]:
        _, b = customer.get(
            f"/api/public/businesses/{business_slug}/slots",
            query={"branchSlug": lab_slug, "date": date_iso, "serviceVariantIds": variant_id},
            expect=(200,),
        )
        return list(b.get("slots") or []) if isinstance(b, dict) else []

    lab_date = (datetime.now(timezone.utc) + timedelta(days=2)).date()
    lab_date_iso = lab_date.isoformat()
    base_slots = lab_slots(lab_date_iso)
    base_count = len(base_slots)
    if base_count == 0:
        REPORT.fail(
            f"Slot lab BAZ slot uretmedi (tarih={lab_date_iso}) -- fixture kurulumu eksik "
            "(calisma saati / kaynak / personel yetkinligi); sonraki slot testleri anlamsiz olur."
        )
        return False
    REPORT.ok(f"lab subesi hazir; {base_count} baz slot (tarih={lab_date_iso})")

    # -- 58 --------- (CALISMA-SAATI) hedef gun KAPALI -> o gun slot DONMEZ; ACINCA geri gelir
    REPORT.start("[CALISMA-SAATI] Lab subesi hedef gun KAPALI -> slot bloklaniyor, ACINCA geri geliyor")
    target_day_name = lab_date.strftime("%A")  # or. 'Saturday' -- PUT gun ADI bekliyor (adim 13)
    owner.put(
        f"/api/business/branches/{lab_branch_id}/working-hours/{target_day_name}",
        {"opensAt": "09:00", "closesAt": "19:00", "isClosed": True},
        expect=(200,),
    )
    closed_slots = lab_slots(lab_date_iso)
    if closed_slots:
        # State'i temiz birak (yeniden ac) sonra dus.
        owner.put(
            f"/api/business/branches/{lab_branch_id}/working-hours/{target_day_name}",
            {"opensAt": "09:00", "closesAt": "19:00", "isClosed": False},
            expect=(200,),
        )
        REPORT.fail(
            f"URUN HATASI: {target_day_name} isClosed=true yapildi ama slot aramada "
            f"{lab_date_iso} icin HALA {len(closed_slots)} slot donuyor. Calisma saati (kapali gun) "
            "public slot motoruna yansimiyor -- musteri kapali gune randevu talep edebilir."
        )
        finding(
            f"Kapali gun (working-hours {target_day_name} isClosed=true) public slot aramasini bloklamiyor."
        )
        return False
    owner.put(
        f"/api/business/branches/{lab_branch_id}/working-hours/{target_day_name}",
        {"opensAt": "09:00", "closesAt": "19:00", "isClosed": False},
        expect=(200,),
    )
    reopened = lab_slots(lab_date_iso)
    if not reopened:
        REPORT.fail(
            f"Gun ({target_day_name}) tekrar ACILDI ama slot GERI GELMEDI "
            f"(tarih={lab_date_iso}, 0 slot) -- calisma saati acma islemi slotlara yansimiyor."
        )
        return False
    REPORT.ok(f"{target_day_name} KAPALI iken 0 slot; ACILINCA {len(reopened)} slot geri geldi")

    # -- 59 -------------- (SLOT-AYARI) slotIntervalMinutes slot sikligini etkiliyor mu +
    #                       null maxPublicSlots audit REGRESYONU
    # PATCH .../slot-settings ile araligi buyut (60). Slot ARDISIK ARALIGI 60 dk olmali VE
    # toplam slot sayisi AZALMALI. AMA once bir SOZLESME-GECERLI cagriyi test ediyoruz:
    # maxPublicSlots contract'ta NULLABLE ve GET null donduruyor; dogal/savunmaci istemci
    # (oku-hepsini-geri-gonder) onu null gonderir. Bu 500 verirse GERCEK URUN HATASIDIR.
    def min_gap_minutes(slots: list[dict]) -> float | None:
        starts = sorted(
            datetime.fromisoformat(s["startUtc"].replace("Z", "+00:00")) for s in slots
        )
        gaps = [(b - a).total_seconds() / 60 for a, b in zip(starts, starts[1:])]
        return min(gaps) if gaps else None

    REPORT.start("[SLOT-AYARI] slotIntervalMinutes slot sikligini etkiliyor + null maxPublicSlots audit")
    _, lab_branch = owner.get(f"/api/business/branches/{lab_branch_id}", expect=(200,))
    orig_interval = lab_branch.get("slotIntervalMinutes") if isinstance(lab_branch, dict) else None
    orig_maxslots = lab_branch.get("maxPublicSlots") if isinstance(lab_branch, dict) else None
    before_iv = lab_slots(lab_date_iso)

    # (1) SOZLESME-GECERLI round-trip: GET'ten okunan maxPublicSlots'u (null) geri gonder.
    status, body = owner.patch(
        f"/api/business/branches/{lab_branch_id}/slot-settings",
        {"slotIntervalMinutes": 60, "maxPublicSlots": orig_maxslots},
        expect=(200, 400, 500),
    )
    null_max_bug = status == 500 and orig_maxslots is None

    # (2) DAVRANISSAL dogrulama: ozelligin KENDISI calisiyor mu? null bug'i olsa bile somut
    #     maxPublicSlots ile PATCH'i dene (bug yoksa bu, 1. cagriyla ayni etkiyi verir).
    if status != 200:
        status, body = owner.patch(
            f"/api/business/branches/{lab_branch_id}/slot-settings",
            {"slotIntervalMinutes": 60, "maxPublicSlots": 200},
            expect=(200, 400, 500),
        )
    after_iv = lab_slots(lab_date_iso) if status == 200 else []
    _, lab_after = owner.get(f"/api/business/branches/{lab_branch_id}", expect=(200,))
    stored_interval = lab_after.get("slotIntervalMinutes") if isinstance(lab_after, dict) else None
    gap = min_gap_minutes(after_iv)
    interval_works = (
        status == 200
        and stored_interval == 60
        and len(after_iv) < len(before_iv)
        and gap is not None
        and gap >= 59.9
    )

    if null_max_bug:
        REPORT.fail(
            "URUN HATASI -- PATCH /api/business/branches/{id}/slot-settings null maxPublicSlots ile "
            "HTTP 500: BranchManagementService.cs:229 audit payload'ini ELLE string-interpolate "
            "ediyor; command.MaxPublicSlots null oldugunda '\"maxPublicSlots\":}' seklinde GECERSIZ "
            "JSON uretiyor, Postgres reddediyor, audit SaveChangesAsync patliyor ve TUM PATCH "
            "rollback oluyor (slot ayari HIC kaydedilmiyor). maxPublicSlots sozlesmede NULLABLE ve "
            "GET null donduruyor: subeyi round-trip'leyen (oku-hepsini-geri-gonder) HER istemci bu "
            "500'u alir; slotIntervalMinutes null olsaydi ayni satir onu da bozardi. Duzeltme: audit "
            "payload'ini elle degil JSON serializer ile uret (null -> 'null'). "
            + (
                f"NOT: somut maxPublicSlots=200 ile ozellik CALISIYOR "
                f"(slot {len(before_iv)}->{len(after_iv)}, en kucuk aralik={gap:.0f}dk); "
                "hata YALNIZCA null maxPublicSlots audit'inde."
                if interval_works
                else "Somut maxPublicSlots=200 ile de dogrulanamadi (ayrica slot etkisi olculemedi)."
            )
        )
        finding(
            "PATCH slot-settings null maxPublicSlots ile HTTP 500 (sozlesme NULLABLE, GET null "
            "donduruyor): BranchManagementService.cs:229 audit JSON'unu elle string-interpolate "
            "ediyor, null -> gecersiz JSON -> audit SaveChanges patliyor -> tum PATCH rollback. "
            "JSON serializer kullanilmali."
        )
    elif interval_works:
        REPORT.ok(
            f"slotIntervalMinutes ->60; slot sayisi {len(before_iv)}->{len(after_iv)}, en kucuk "
            f"ardisik aralik={gap:.0f}dk (araligi seyreltti). null maxPublicSlots 500'u bu kosumda YOK."
        )
    else:
        REPORT.fail(
            f"URUN HATASI: slotIntervalMinutes=60 uygulanamadi/etkilemedi "
            f"(HTTP {status}, GET slotIntervalMinutes={stored_interval!r}, "
            f"slot {len(before_iv)}->{len(after_iv)}, en kucuk aralik={gap}). "
            f"Yanit: {body}"
        )
        finding("slotIntervalMinutes degisimi public slot araligini/sayisini degistirmiyor.")

    # E adimi icin lab subesinde slot BIRAK (somut maxPublicSlots ile -- null bug'ina takilmadan).
    safe_interval = orig_interval if isinstance(orig_interval, int) and orig_interval > 0 else 15
    owner.patch(
        f"/api/business/branches/{lab_branch_id}/slot-settings",
        {"slotIntervalMinutes": safe_interval, "maxPublicSlots": 200},
        expect=(200, 400, 500),
    )

    # -- 60 ---------------- (KAYNAK) kaynak out-of-service -> slotlar azalir; restore -> geri gelir
    # variant_id gerekli kaynak tipi (resource_type_id) tasiyor ve lab subesinde TEK kaynak var;
    # o kaynak out-of-service olunca slot motoru kaynak adayi bulamamali -> 0 slot. Degismezse NOT dus.
    REPORT.start("[KAYNAK] Kaynak out-of-service -> slotlar azalir/sifirlanir; restore -> geri gelir")
    before_oos = lab_slots(lab_date_iso)
    owner.post(
        f"/api/business/branches/{lab_branch_id}/resources/{lab_resource_id}/out-of-service",
        expect=(200, 204),
    )
    after_oos = lab_slots(lab_date_iso)
    oos_blocked = len(after_oos) < len(before_oos)
    if not oos_blocked:
        REPORT.info(
            f"Kaynak out-of-service edildi ama slot sayisi DEGISMEDI (once={len(before_oos)} "
            f"sonra={len(after_oos)}). variant_id gerekli kaynak tipi ({resource_type_id}) tasiyor "
            "ve subede TEK kaynak vardi; o devre disi kalinca slotlar dusmeliydi. Slot motoru kaynak "
            "musaitligini (out-of-service) dikkate almiyor olabilir."
        )
        finding(
            "Kaynak out-of-service public slot aramasini degistirmiyor -- slot motoru kaynak "
            "musaitligini dikkate almiyor olabilir (urun riski; dogrulanmali)."
        )
    # restore
    owner.post(
        f"/api/business/branches/{lab_branch_id}/resources/{lab_resource_id}/restore",
        expect=(200, 204),
    )
    restored_oos = lab_slots(lab_date_iso)
    if oos_blocked:
        if len(restored_oos) <= len(after_oos):
            REPORT.fail(
                f"Kaynak RESTORE edildi ama slotlar geri gelmedi (out-of-service={len(after_oos)} "
                f"slot, restore={len(restored_oos)} slot). Restore islemi slotlara yansimiyor."
            )
            finding("Kaynak restore public slot aramasina yansimiyor (out-of-service geri alinamiyor).")
            return False
        REPORT.ok(
            f"out-of-service: {len(before_oos)}->{len(after_oos)} slot (kaynak adayi kalmadi); "
            f"restore: ->{len(restored_oos)} slot (geri geldi)"
        )
    else:
        REPORT.info(
            f"restore sonrasi slot sayisi={len(restored_oos)} (out-of-service etkisi olculemedi)."
        )

    return True


def main() -> int:
    parser = argparse.ArgumentParser(
        description="RezSaaS uctan uca duman testi (bagimliliksiz).",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--api-url",
        default=DEFAULT_API_URL,
        help=f"Kosan API'nin adresi (varsayilan: {DEFAULT_API_URL})",
    )
    parser.add_argument(
        "--bootstrap-token",
        default=DEFAULT_BOOTSTRAP_TOKEN,
        help="Platform admin bootstrap token'i (SHA256'si API ayarinda olmali)",
    )
    parser.add_argument(
        "--seed-business",
        action="store_true",
        help=(
            "Organization.Business satirini DOGRUDAN Postgres'e yazar. "
            "Bilinen urun boslugu icin TEST KOSUM ARACI kestirmesi -- urun akisi degildir."
        ),
    )
    parser.add_argument(
        "--admin-email",
        default=DEFAULT_ADMIN_EMAIL,
        help="Platform admin e-postasi (bootstrap zaten yapildiysa MEVCUT admin'inki)",
    )
    parser.add_argument(
        "--admin-password",
        default=PASSWORD,
        help="Platform admin parolasi",
    )
    parser.add_argument(
        "--admin-totp-secret",
        default=None,
        help=(
            "Admin'in TOTP sharedKey'i (base32). Normalde ilk kosuda otomatik saklanir; "
            "sadece durum dosyasi kayipsa gerekir."
        ),
    )
    parser.add_argument(
        "--print-token-hash",
        action="store_true",
        help="Sadece bootstrap token'in SHA256 hex'ini yaz ve cik.",
    )
    args = parser.parse_args()

    if args.print_token_hash:
        print(hashlib.sha256(args.bootstrap_token.encode()).hexdigest())
        return 0

    try:
        ok = run(
            args.api_url,
            args.bootstrap_token,
            args.seed_business,
            args.admin_email,
            args.admin_password,
            args.admin_totp_secret,
        )
    except ApiError as error:
        REPORT.fail(str(error))
        ok = False
    except StepFailed as error:
        REPORT.fail(str(error))
        ok = False
    except KeyboardInterrupt:
        print("\nIptal edildi.")
        return 130

    passed = REPORT.summary()
    if ok and passed:
        print("\nSONUC: CEKIRDEK DONGU CALISIYOR -- randevu uctan uca dogdu.\n")
        return 0

    if CORE_LOOP_PROVEN:
        print(
            "\nSONUC: CEKIRDEK DONGU CALISIYOR (randevu uctan uca dogdu) AMA bir REGRESYON\n"
            "       kontrolu URUN HATASI yakaladi. Ayrintiler icin yukaridaki FAIL satirina\n"
            "       ve README-e2e-smoke.md 'Bulunan hatalar' basligina bakin.\n"
        )
    else:
        print("\nSONUC: DUMAN TESTI DUSTU. Yukaridaki ilk FAIL satiri kok sebeptir.\n")
    return 1


if __name__ == "__main__":
    sys.exit(main())
