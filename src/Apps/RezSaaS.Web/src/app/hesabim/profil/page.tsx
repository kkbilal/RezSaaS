"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";

export default function CustomerProfilePage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [isEditing, setIsEditing] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  async function handleSave() {
    setIsSaving(true);
    try {
      // API endpoint implementation placeholder
      // const result = await apiClient.PATCH("/api/customer/profile", {
      //   body: { name: name.trim(), email: email.trim(), phone: phone.trim() },
      // });
      
      showToast("Profil güncellenirken bir hata oluştu. API endpoint henüz mevcut değil.");
      return;
      
      /* 
      if (!result.response.ok) {
        showToast("Profil güncellenirken bir hata oluştu.");
        return;
      }

      showToast("Profil başarıyla güncellendi.");
      setIsEditing(false);
      router.refresh();
      */
    } catch {
      showToast("Profil güncellenemedi. Lütfen tekrar dene.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDeleteAccount() {
    setIsDeleting(true);
    try {
      // API endpoint implementation placeholder
      showToast("Hesap silme talebi oluşturulurken bir hata oluştu. API endpoint henüz mevcut değil.");
      setShowDeleteDialog(false);
      return;
      
      /*
      const result = await apiClient.POST("/api/customer/profile/delete-request");

      if (!result.response.ok) {
        showToast("Hesap silme talebi oluşturulurken bir hata oluştu.");
        setShowDeleteDialog(false);
        return;
      }

      showToast("Hesap silme talebi oluşturuldu. E-posta adresinizi kontrol edin.");
      setShowDeleteDialog(false);
      router.refresh();
      */
    } catch {
      showToast("Hesap silme talebi oluşturulamadı. Lütfen tekrar dene.");
      setShowDeleteDialog(false);
    } finally {
      setIsDeleting(false);
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Profil Ayarları</h1>
        <p className="mt-2 text-gray-600">
          Kişisel bilgilerinizi yönetin ve hesap ayarlarınızı düzenleyin
        </p>
      </div>

      <div className="max-w-2xl space-y-6">
        <Card className="p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl font-semibold text-gray-900">
              Kişisel Bilgiler
            </h2>
            {!isEditing && (
              <Button onClick={() => setIsEditing(true)} variant="secondary">
                Düzenle
              </Button>
            )}
          </div>

          <div className="space-y-4">
            <FormField label="Ad Soyad">
              <TextInput
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={!isEditing}
                placeholder="Adınız ve soyadınız"
              />
            </FormField>

            <FormField 
              label="E-posta"
              hint="E-posta değişikliği için doğrulama gerekir"
            >
              <TextInput
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={!isEditing}
                placeholder="ornek@email.com"
              />
            </FormField>

            <FormField 
              label="Telefon"
              hint="Rezervasyon bildirimleri için kullanılır"
            >
              <TextInput
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                disabled={!isEditing}
                placeholder="+90 555 123 4567"
              />
            </FormField>
          </div>

          {isEditing && (
            <div className="mt-6 flex gap-3">
              <Button
                onClick={handleSave}
                disabled={isSaving}
                className="flex-1"
              >
                {isSaving ? "Kaydediliyor..." : "Kaydet"}
              </Button>
              <Button
                onClick={() => setIsEditing(false)}
                variant="ghost"
                disabled={isSaving}
              >
                İptal
              </Button>
            </div>
          )}
        </Card>

        <Card className="p-6 border-red-200">
          <div className="space-y-4">
            <h2 className="text-xl font-semibold text-gray-900">
              Tehlikeli Bölge
            </h2>
            <p className="text-gray-600 text-sm">
              Hesabınızı silmek tüm verilerinizin kalıcı olarak silinmesine neden olur.
              Bu işlem geri alınamaz.
            </p>
            <Button
              onClick={() => setShowDeleteDialog(true)}
              variant="danger"
              className="w-full sm:w-auto"
            >
              Hesabı Sil
            </Button>
          </div>
        </Card>
      </div>

      {toast && (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-gray-200 bg-white px-5 py-3 text-sm text-gray-900 shadow-lg">
          {toast}
        </div>
      )}

      {showDeleteDialog && (
        <DialogOverlay onEscapeKeyDown={() => setShowDeleteDialog(false)}>
          <DialogPanel titleId="delete-dialog-title">
            <h2 
              id="delete-dialog-title" 
              className="text-xl font-semibold text-gray-900 mb-4"
            >
              Hesabı Silmek İstediğinize Emin Misiniz?
            </h2>
            <div className="space-y-4">
              <p className="text-gray-600">
                Bu işlem geri alınamaz. Hesabınız ve tüm verileriniz kalıcı olarak silinecek:
              </p>
              <ul className="list-disc list-inside space-y-2 text-sm text-gray-600">
                <li>Rezervasyon geçmişiniz</li>
                <li>Kişisel bilgileriniz</li>
                <li>İtiraz kayıtlarınız</li>
                <li>Diğer tüm hesap verileri</li>
              </ul>
              <p className="text-sm text-gray-600 font-medium">
                Onaylamak için e-posta adresinize doğrulama bağlantısı gönderilecek.
              </p>
              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleDeleteAccount}
                  disabled={isDeleting}
                  variant="danger"
                  className="flex-1"
                >
                  {isDeleting ? "İşleniyor..." : "Hesabı Sil"}
                </Button>
                <Button
                  onClick={() => setShowDeleteDialog(false)}
                  variant="ghost"
                  disabled={isDeleting}
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
