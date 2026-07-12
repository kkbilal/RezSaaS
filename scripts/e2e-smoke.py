#!/usr/bin/env python3
"""
RezSaaS -- UCTAN UCA DUMAN TESTI (end-to-end smoke test).

Urunun cekirdek dongusunu GERCEK bir API'ye karsi bastan sona kosturur:

    platform admin -> tenant + owner provision -> salon kurulumu
    (sube, calisma saati, kaynak, personel, yetkinlik, hizmet, varyant)
    -> musteri kaydi -> slot arama -> randevu TALEBI -> salon ONAYI
    -> RANDEVU dogar -> dogrulanir

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


# --------------------------------------------------------------------------------------
# Cikti yardimcilari
# --------------------------------------------------------------------------------------

TOTAL_STEPS = 25


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
        print("-" * 78)
        print(f"  gecti={len(passed)}  kaldi={len(failed)}  atlandi={len(skipped)}")
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
    ) -> tuple[int, object]:
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

        try:
            with self.opener.open(req, timeout=30) as response:
                status = response.status
                raw = response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as error:
            status = error.code
            raw = error.read().decode("utf-8", errors="replace")
        except urllib.error.URLError as error:
            raise StepFailed(
                f"API'ye baglanilamadi ({url}): {error.reason}. API kosuyor mu?"
            ) from error

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
            "/api/auth/login", login_body, query={"useCookies": "true"}, expect=(200, 401)
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
            "/api/auth/login", login_body, query={"useCookies": "true"}, expect=(200,)
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
    )
    REPORT.ok(owner_email)

    # -- 9 ------------------------------------------- owner girisi + userAccountId alma
    REPORT.start("Owner girisi + userAccountId")
    owner.post(
        "/api/auth/login",
        {"email": owner_email, "password": PASSWORD},
        query={"useCookies": "true"},
        expect=(200,),
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
    REPORT.ok(
        f"status={match.get('status')} start={match.get('startUtc')} "
        f"staff={match.get('staffMemberDisplayName')} kaynak={match.get('resourceDisplayName')}"
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
