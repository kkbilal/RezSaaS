"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";

export default function TenantProvisioningPage() {
  const router = useRouter();
  const [businessName, setBusinessName] = useState("");
  const [businessSlug, setBusinessSlug] = useState("");
  const [ownerEmail, setOwnerEmail] = useState("");
  const [category, setCategory] = useState("");
  const [initialNotes, setInitialNotes] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const [isCheckingSlug, setIsCheckingSlug] = useState(false);
  const [slugAvailable, setSlugAvailable] = useState<boolean | null>(null);
  const [ownerValid, setOwnerValid] = useState<boolean | null>(null);
  const [ownerDisplayName, setOwnerDisplayName] = useState("");
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [toast, setToast] = useState<string | null>(null);

  // Auto-generate slug from business name
  const handleBusinessNameChange = (value: string) => {
    setBusinessName(value);
    const slug = value
      .toLowerCase()
      .trim()
      .replace(/[^\w\s-]/g, "")
      .replace(/[\s_-]+/g, "-")
      .replace(/^-+|-+$/g, "");
    setBusinessSlug(slug);
    setSlugAvailable(null);
  };

  // Check slug availability
  // NOTE: Backend endpoint not implemented yet
  async function checkSlugAvailability() {
    if (!businessSlug || businessSlug.length < 3) {
      setSlugAvailable(null);
      return;
    }

    setIsCheckingSlug(true);
    try {
      // TODO: Implement backend endpoint: GET /api/admin/tenants/check-slug?slug={slug}
      // const result = await apiClient.GET("/api/admin/tenants/check-slug", {
      //   params: {
      //     query: { slug: businessSlug },
      //   },
      // });

      // Placeholder: simulate availability check
      await new Promise((resolve) => setTimeout(resolve, 500));
      
      // TODO: Replace with real API response
      // if (result.response.ok && result.data) {
      //   setSlugAvailable(result.data.available);
      // }
      
      setSlugAvailable(true); // Placeholder: always available for demo
    } catch {
      setSlugAvailable(null);
    } finally {
      setIsCheckingSlug(false);
    }
  }

  // Validate owner email
  // NOTE: Backend endpoint not implemented yet
  async function validateOwnerEmail() {
    if (!ownerEmail || !ownerEmail.includes("@")) {
      setOwnerValid(null);
      setOwnerDisplayName("");
      return;
    }

    try {
      // TODO: Implement backend endpoint: GET /api/admin/users?email={email}
      // const result = await apiClient.GET("/api/admin/users", {
      //   params: {
      //     query: { email: ownerEmail },
      //   },
      // });

      // Placeholder: simulate user lookup
      await new Promise((resolve) => setTimeout(resolve, 500));
      
      // TODO: Replace with real API response
      // if (result.response.ok && result.data) {
      //   setOwnerValid(true);
      //   setOwnerDisplayName(result.data.displayName || "");
      // } else {
      //   setOwnerValid(false);
      //   setOwnerDisplayName("");
      // }
      
      setOwnerValid(true); // Placeholder: always valid for demo
      setOwnerDisplayName("Demo Kullanıcı");
    } catch {
      setOwnerValid(null);
    }
  }

  // Validate form
  function validateForm() {
    const newErrors: Record<string, string> = {};

    if (!businessName || businessName.length < 3 || businessName.length > 100) {
      newErrors.businessName = "İşletme adı 3-100 karakter olmalıdır";
    }

    if (!businessSlug || businessSlug.length < 3 || businessSlug.length > 50) {
      newErrors.businessSlug = "Slug 3-50 karakter olmalıdır";
    } else if (!/^[a-z0-9-]+$/.test(businessSlug)) {
      newErrors.businessSlug = "Sadece küçük harf, rakam ve tire kullanılabilir";
    } else if (businessSlug.startsWith("-") || businessSlug.endsWith("-")) {
      newErrors.businessSlug = "Slug tire ile başlayamaz veya bitemez";
    } else if (slugAvailable === false) {
      newErrors.businessSlug = "Bu slug zaten kullanılıyor";
    }

    if (!ownerEmail || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(ownerEmail)) {
      newErrors.ownerEmail = "Geçerli bir e-posta adresi girin";
    } else if (ownerValid === false) {
      newErrors.ownerEmail = "Bu e-posta ile kayıtlı aktif kullanıcı bulunamadı";
    }

    if (initialNotes && initialNotes.length > 500) {
      newErrors.initialNotes = "Notlar en fazla 500 karakter olabilir";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  // Create tenant
  // NOTE: Backend endpoint not implemented yet
  async function handleCreateTenant() {
    if (!validateForm()) {
      return;
    }

    setShowCreateDialog(false);
    setIsCreating(true);

    try {
      // TODO: Implement backend endpoint: POST /api/admin/tenants
      // const result = await apiClient.POST("/api/admin/tenants", {
      //   body: {
      //     businessName: businessName.trim(),
      //     businessSlug: businessSlug.trim(),
      //     ownerEmail: ownerEmail.trim(),
      //     category: category.trim() || null,
      //     initialNotes: initialNotes.trim() || null,
      //   },
      // });

      // Placeholder: simulate tenant creation
      await new Promise((resolve) => setTimeout(resolve, 1500));
      
      // TODO: Replace with real API response
      // if (!result.response.ok) {
      //   const errorData = await result.response.json().catch(() => ({}));
      //   showToast(
      //     `Tenant oluşturulamadı: ${errorData.message || "Bilinmeyen hata"}`
      //   );
      //   return;
      // }

      // Placeholder: show success message
      showToast("Tenant oluşturma API endpoint henüz mevcut değil. Demo: Başarılı");
      // router.push(`/platform/tenantlar/${result.data?.tenantId}`);
    } catch {
      showToast("Tenant oluşturulurken bir hata oluştu");
    } finally {
      setIsCreating(false);
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Yeni Tenant Oluştur</h1>
        <p className="mt-2 text-gray-600">
          İşletme bilgilerini girerek yeni tenant hesabı oluşturun
        </p>
      </div>

      <div className="max-w-2xl space-y-6">
        <Card className="p-6">
          <div className="space-y-4">
            <FormField label="İşletme Adı" error={errors.businessName}>
              <TextInput
                type="text"
                value={businessName}
                onChange={(e) => handleBusinessNameChange(e.target.value)}
                placeholder="Örnek: İstanbul Hair Salon"
                required
              />
            </FormField>

            <FormField
              label="Business Slug"
              hint="URL'de kullanılacak benzersiz tanımlayıcı (otomatik oluşturulur)"
              error={errors.businessSlug}
            >
              <div className="flex gap-2">
                <TextInput
                  type="text"
                  value={businessSlug}
                  onChange={(e) => {
                    setBusinessSlug(e.target.value);
                    setSlugAvailable(null);
                  }}
                  onBlur={checkSlugAvailability}
                  placeholder="istanbul-hair-salon"
                  required
                  className="flex-1"
                />
                {isCheckingSlug && (
                  <div className="flex items-center px-4 text-sm text-gray-600">
                    Kontrol ediliyor...
                  </div>
                )}
                {slugAvailable === true && (
                  <div className="flex items-center px-4 text-sm text-green-600">
                    ✓ Kullanılabilir
                  </div>
                )}
                {slugAvailable === false && (
                  <div className="flex items-center px-4 text-sm text-red-600">
                    ✗ Zaten kullanılıyor
                  </div>
                )}
              </div>
            </FormField>

            <FormField
              label="İşletme Sahibi E-postası"
              hint="İşletme sahibi olarak atanacak aktif kullanıcı hesabı"
              error={errors.ownerEmail}
            >
              <div>
                <TextInput
                  type="email"
                  value={ownerEmail}
                  onChange={(e) => {
                    setOwnerEmail(e.target.value);
                    setOwnerValid(null);
                    setOwnerDisplayName("");
                  }}
                  onBlur={validateOwnerEmail}
                  placeholder="ornek@email.com"
                  required
                />
                {ownerValid === true && ownerDisplayName && (
                  <p className="mt-2 text-sm text-green-600">
                    ✓ {ownerDisplayName}
                  </p>
                )}
                {ownerValid === false && (
                  <p className="mt-2 text-sm text-red-600">
                    ✗ Bu e-posta ile kayıtlı aktif kullanıcı bulunamadı
                  </p>
                )}
              </div>
            </FormField>

            <FormField label="Kategori" hint="İsteğe bağlı">
              <TextInput
                type="text"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                placeholder="Örnek: Kuaför, Berber, Spa"
              />
            </FormField>

            <FormField
              label="Başlangıç Notları"
              hint="İsteğe bağlı, en fazla 500 karakter"
              error={errors.initialNotes}
            >
              <textarea
                value={initialNotes}
                onChange={(e) => setInitialNotes(e.target.value)}
                placeholder="Onboarding ile ilgili notlar..."
                className="min-h-24 w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-900 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500 outline-none"
                maxLength={500}
              />
              <p className="mt-1 text-xs text-gray-600">
                {initialNotes.length}/500
              </p>
            </FormField>
          </div>

          <div className="mt-6 flex gap-3">
            <Button
              onClick={() => {
                if (validateForm()) {
                  setShowCreateDialog(true);
                }
              }}
              disabled={isCreating}
              className="flex-1"
            >
              {isCreating ? "Oluşturuluyor..." : "Tenant Oluştur"}
            </Button>
            <Button
              onClick={() => router.back()}
              variant="ghost"
              disabled={isCreating}
            >
              İptal
            </Button>
          </div>
        </Card>

        <Card className="p-6 bg-blue-50 border-blue-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            Onboarding Kontrol Listesi
          </h3>
          <ul className="space-y-2 text-sm text-gray-700">
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled checked />
              <span>Tenant hesabı oluşturuldu</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>İşletme profili bilgileri girildi</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>Şube/lokasyon tanımlandı</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>Hizmetler oluşturuldu</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>Personel eklendi</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>Çalışma saatleri ayarlandı</span>
            </li>
            <li className="flex items-center gap-2">
              <input type="checkbox" disabled />
              <span>Ödeme ayarları yapılandırıldı</span>
            </li>
          </ul>
        </Card>
      </div>

      {toast && (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-gray-200 bg-white px-5 py-3 text-sm text-gray-900 shadow-lg">
          {toast}
        </div>
      )}

      {showCreateDialog && (
        <DialogOverlay onEscapeKeyDown={() => setShowCreateDialog(false)}>
          <DialogPanel titleId="create-tenant-dialog">
            <h2
              id="create-tenant-dialog"
              className="text-xl font-semibold text-gray-900 mb-4"
            >
              Tenant Oluşturmak İstediğinize Emin Misiniz?
            </h2>
            <div className="space-y-4">
              <div className="rounded-lg bg-gray-50 p-4 space-y-2">
                <p className="text-sm">
                  <span className="font-medium">İşletme Adı:</span> {businessName}
                </p>
                <p className="text-sm">
                  <span className="font-medium">Business Slug:</span> {businessSlug}
                </p>
                <p className="text-sm">
                  <span className="font-medium">İşletme Sahibi:</span> {ownerEmail}
                  {ownerDisplayName && ` (${ownerDisplayName})`}
                </p>
                {category && (
                  <p className="text-sm">
                    <span className="font-medium">Kategori:</span> {category}
                  </p>
                )}
              </div>
              <p className="text-sm text-gray-600">
                Bu işlem geri alınamaz. Tenant oluşturulduktan sonra işletme sahibi
                olarak {ownerEmail} hesabı atanacaktır.
              </p>
              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleCreateTenant}
                  disabled={isCreating}
                  className="flex-1"
                >
                  {isCreating ? "Oluşturuluyor..." : "Tenant Oluştur"}
                </Button>
                <Button
                  onClick={() => setShowCreateDialog(false)}
                  variant="ghost"
                  disabled={isCreating}
                >
                  İptal
                </Button>
              </div>
            </div>
          </DialogPanel>
        </DialogOverlay>
      )}
    </div>
  );
}