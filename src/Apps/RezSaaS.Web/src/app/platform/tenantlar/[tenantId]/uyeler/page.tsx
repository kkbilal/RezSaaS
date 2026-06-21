"use client";

import { useState, useEffect } from "react";
import { useRouter, useParams } from "next/navigation";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";

type MembershipStatus = "Active" | "Suspended" | "Revoked";
type MembershipRole = "BusinessOwner" | "BranchManager" | "Staff";

interface Membership {
  membershipId: string;
  userId: string;
  displayName: string;
  email: string;
  role: MembershipRole;
  status: MembershipStatus;
  createdAt: string;
  lastActivityAt: string | null;
}

export default function TenantMembershipPage() {
  const router = useRouter();
  const params = useParams();
  const tenantId = params.tenantId as string;

  const [memberships, setMemberships] = useState<Membership[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  
  // Add member form state
  const [showAddMemberDialog, setShowAddMemberDialog] = useState(false);
  const [newMemberEmail, setNewMemberEmail] = useState("");
  const [newMemberRole, setNewMemberRole] = useState<MembershipRole>("Staff");
  const [newMemberDisplayName, setNewMemberDisplayName] = useState("");
  const [isAddingMember, setIsAddingMember] = useState(false);
  const [emailValid, setEmailValid] = useState<boolean | null>(null);
  
  // Suspend/Revoke dialog state
  const [showActionDialog, setShowActionDialog] = useState(false);
  const [actionMembership, setActionMembership] = useState<Membership | null>(null);
  const [actionType, setActionType] = useState<"suspend" | "revoke">("suspend");
  const [actionReason, setActionReason] = useState("");
  const [isExecutingAction, setIsExecutingAction] = useState(false);
  
  const [toast, setToast] = useState<string | null>(null);

  // Load memberships
  useEffect(() => {
    loadMemberships();
  }, [tenantId]);

  async function loadMemberships() {
    setIsLoading(true);
    try {
      // TODO: Implement backend endpoint: GET /api/admin/tenants/{tenantId}/memberships
      // const result = await apiClient.GET("/api/admin/tenants/{tenantId}/memberships", {
      //   params: {
      //     path: { tenantId },
      //   },
      // });

      // Placeholder: simulate memberships
      await new Promise((resolve) => setTimeout(resolve, 800));
      
      // TODO: Replace with real API response
      // if (result.response.ok && result.data) {
      //   setMemberships(result.data.memberships);
      // }
      
      // Placeholder: demo data
      setMemberships([
        {
          membershipId: "1",
          userId: "user1",
          displayName: "Demo Owner",
          email: "owner@demo.com",
          role: "BusinessOwner",
          status: "Active",
          createdAt: "2026-01-15T10:00:00Z",
          lastActivityAt: "2026-06-20T14:30:00Z",
        },
        {
          membershipId: "2",
          userId: "user2",
          displayName: "Demo Manager",
          email: "manager@demo.com",
          role: "BranchManager",
          status: "Active",
          createdAt: "2026-02-01T10:00:00Z",
          lastActivityAt: "2026-06-19T16:45:00Z",
        },
      ]);
    } catch {
      showToast("Üyelikler yüklenirken bir hata oluştu");
    } finally {
      setIsLoading(false);
    }
  }

  // Validate member email
  async function validateMemberEmail() {
    if (!newMemberEmail || !newMemberEmail.includes("@")) {
      setEmailValid(null);
      setNewMemberDisplayName("");
      return;
    }

    try {
      // TODO: Implement backend endpoint: GET /api/admin/users?email={email}
      // const result = await apiClient.GET("/api/admin/users", {
      //   params: {
      //     query: { email: newMemberEmail },
      //   },
      // });

      // Placeholder: simulate user lookup
      await new Promise((resolve) => setTimeout(resolve, 500));
      
      // TODO: Replace with real API response
      // if (result.response.ok && result.data) {
      //   setEmailValid(true);
      //   setNewMemberDisplayName(result.data.displayName || "");
      // } else {
      //   setEmailValid(false);
      //   setNewMemberDisplayName("");
      // }
      
      setEmailValid(true); // Placeholder: always valid for demo
      setNewMemberDisplayName("Demo Kullanıcı");
    } catch {
      setEmailValid(null);
    }
  }

  // Add member
  async function handleAddMember() {
    if (!newMemberEmail || emailValid !== true) {
      showToast("Lütfen geçerli bir e-posta adresi girin");
      return;
    }

    setIsAddingMember(true);
    try {
      // TODO: Implement backend endpoint: POST /api/admin/tenants/{tenantId}/memberships
      // const result = await apiClient.POST("/api/admin/tenants/{tenantId}/memberships", {
      //   params: {
      //     path: { tenantId },
      //   },
      //   body: {
      //     userEmail: newMemberEmail.trim(),
      //     role: newMemberRole,
      //   },
      // });

      // Placeholder: simulate member addition
      await new Promise((resolve) => setTimeout(resolve, 1000));
      
      // TODO: Replace with real API response
      // if (!result.response.ok) {
      //   const errorData = await result.response.json().catch(() => ({}));
      //   showToast(`Üye eklenemedi: ${errorData.message || "Bilinmeyen hata"}`);
      //   return;
      // }

      showToast("Üye ekleme API endpoint henüz mevcut değil. Demo: Başarılı");
      setShowAddMemberDialog(false);
      setNewMemberEmail("");
      setNewMemberRole("Staff");
      setNewMemberDisplayName("");
      setEmailValid(null);
      // loadMemberships(); // Reload after successful addition
    } catch {
      showToast("Üye eklenirken bir hata oluştu");
    } finally {
      setIsAddingMember(false);
    }
  }

  // Open action dialog
  function openActionDialog(membership: Membership, action: "suspend" | "revoke") {
    // Check if this is the last BusinessOwner
    const activeBusinessOwners = memberships.filter(
      (m) => m.role === "BusinessOwner" && m.status === "Active"
    );
    if (action === "suspend" || action === "revoke") {
      if (membership.role === "BusinessOwner" && activeBusinessOwners.length === 1) {
        showToast("Son aktif işletme sahibi askıya alınamaz veya iptal edilemez");
        return;
      }
    }
    
    setActionMembership(membership);
    setActionType(action);
    setActionReason("");
    setShowActionDialog(true);
  }

  // Execute action (suspend or revoke)
  async function handleExecuteAction() {
    if (!actionMembership || actionReason.length < 10) {
      showToast("Lütfen en az 10 karakterlik bir neden girin");
      return;
    }

    setIsExecutingAction(true);
    try {
      const endpoint = actionType === "suspend"
        ? "/api/admin/tenants/{tenantId}/memberships/{membershipId}/suspend"
        : "/api/admin/tenants/{tenantId}/memberships/{membershipId}/revoke";

      // TODO: Implement backend endpoint
      // const result = await apiClient.POST(endpoint, {
      //   params: {
      //     path: { tenantId, membershipId: actionMembership.membershipId },
      //   },
      //   body: {
      //     reason: actionReason.trim(),
      //   },
      // });

      // Placeholder: simulate action
      await new Promise((resolve) => setTimeout(resolve, 1000));
      
      // TODO: Replace with real API response
      // if (!result.response.ok) {
      //   const errorData = await result.response.json().catch(() => ({}));
      //   showToast(`${actionType === "suspend" ? "Askıya alma" : "İptal etme"} başarısız: ${errorData.message || "Bilinmeyen hata"}`);
      //   return;
      // }

      showToast(`${actionType === "suspend" ? "Askıya alma" : "İptal etme"} API endpoint henüz mevcut değil. Demo: Başarılı`);
      setShowActionDialog(false);
      // loadMemberships(); // Reload after successful action
    } catch {
      showToast("İşlem sırasında bir hata oluştu");
    } finally {
      setIsExecutingAction(false);
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  function getStatusBadge(status: MembershipStatus) {
    const styles = {
      Active: "bg-green-100 text-green-800",
      Suspended: "bg-yellow-100 text-yellow-800",
      Revoked: "bg-red-100 text-red-800",
    };
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${styles[status]}`}>
        {status === "Active" ? "Aktif" : status === "Suspended" ? "Askıda" : "İptal"}
      </span>
    );
  }

  function getRoleBadge(role: MembershipRole) {
    const styles = {
      BusinessOwner: "bg-purple-100 text-purple-800",
      BranchManager: "bg-blue-100 text-blue-800",
      Staff: "bg-gray-100 text-gray-800",
    };
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${styles[role]}`}>
        {role === "BusinessOwner" ? "İşletme Sahibi" : role === "BranchManager" ? "Şube Yöneticisi" : "Personel"}
      </span>
    );
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-1/3"></div>
          <div className="h-64 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Tenant Üyeleri</h1>
        <p className="mt-2 text-gray-600">
          Tenant üyelerini yönetin, yeni üye ekleyin ve üyelik durumlarını kontrol edin
        </p>
      </div>

      <div className="space-y-6">
        <Card className="p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl font-semibold text-gray-900">
              Üyelik Listesi
            </h2>
            <Button onClick={() => setShowAddMemberDialog(true)}>
              Yeni Üye Ekle
            </Button>
          </div>

          {memberships.length === 0 ? (
            <div className="text-center py-12 text-gray-600">
              <p className="text-lg font-medium mb-2">Henüz üye yok</p>
              <p>İlk üyeyi eklemek için "Yeni Üye Ekle" butonunu kullanın</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Üye
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Rol
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Durum
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Katılma Tarihi
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Son Aktivite
                    </th>
                    <th className="text-right py-3 px-4 font-medium text-gray-900">
                      İşlemler
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {memberships.map((membership) => (
                    <tr key={membership.membershipId} className="border-b border-gray-100">
                      <td className="py-3 px-4">
                        <div>
                          <div className="font-medium text-gray-900">
                            {membership.displayName}
                          </div>
                          <div className="text-sm text-gray-600">
                            {membership.email}
                          </div>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        {getRoleBadge(membership.role)}
                      </td>
                      <td className="py-3 px-4">
                        {getStatusBadge(membership.status)}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {new Date(membership.createdAt).toLocaleDateString("tr-TR")}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {membership.lastActivityAt
                          ? new Date(membership.lastActivityAt).toLocaleString("tr-TR")
                          : "Hiç"}
                      </td>
                      <td className="py-3 px-4 text-right">
                        {membership.status === "Active" && (
                          <div className="flex gap-2 justify-end">
                            <Button
                              variant="ghost"
                              onClick={() => openActionDialog(membership, "suspend")}
                            >
                              Askıya Al
                            </Button>
                            <Button
                              variant="danger"
                              onClick={() => openActionDialog(membership, "revoke")}
                            >
                              İptal Et
                            </Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>

      {toast && (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-gray-200 bg-white px-5 py-3 text-sm text-gray-900 shadow-lg">
          {toast}
        </div>
      )}

      {/* Add Member Dialog */}
      {showAddMemberDialog && (
        <DialogOverlay onEscapeKeyDown={() => setShowAddMemberDialog(false)}>
          <DialogPanel titleId="add-member-dialog">
            <h2
              id="add-member-dialog"
              className="text-xl font-semibold text-gray-900 mb-4"
            >
              Yeni Üye Ekle
            </h2>
            <div className="space-y-4">
              <FormField
                label="Üye E-postası"
                hint="Eklemek istediğiniz kullanıcının e-posta adresi"
              >
                <TextInput
                  type="email"
                  value={newMemberEmail}
                  onChange={(e) => {
                    setNewMemberEmail(e.target.value);
                    setEmailValid(null);
                    setNewMemberDisplayName("");
                  }}
                  onBlur={validateMemberEmail}
                  placeholder="ornek@email.com"
                  required
                />
                {emailValid === true && newMemberDisplayName && (
                  <p className="mt-2 text-sm text-green-600">
                    ✓ {newMemberDisplayName}
                  </p>
                )}
                {emailValid === false && (
                  <p className="mt-2 text-sm text-red-600">
                    ✗ Bu e-posta ile kayıtlı aktif kullanıcı bulunamadı
                  </p>
                )}
              </FormField>

              <FormField label="Rol">
                <select
                  value={newMemberRole}
                  onChange={(e) => setNewMemberRole(e.target.value as MembershipRole)}
                  className="w-full min-h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-900 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500 outline-none"
                >
                  <option value="Staff">Personel</option>
                  <option value="BranchManager">Şube Yöneticisi</option>
                  <option value="BusinessOwner">İşletme Sahibi</option>
                </select>
              </FormField>

              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleAddMember}
                  disabled={isAddingMember || emailValid !== true}
                  className="flex-1"
                >
                  {isAddingMember ? "Ekleniyor..." : "Üye Ekle"}
                </Button>
                <Button
                  onClick={() => setShowAddMemberDialog(false)}
                  variant="ghost"
                  disabled={isAddingMember}
                >
                  İptal
                </Button>
              </div>
            </div>
          </DialogPanel>
        </DialogOverlay>
      )}

      {/* Suspend/Revoke Dialog */}
      {showActionDialog && actionMembership && (
        <DialogOverlay onEscapeKeyDown={() => setShowActionDialog(false)}>
          <DialogPanel
            titleId={`action-dialog-${actionMembership.membershipId}`}
          >
            <h2
              id={`action-dialog-${actionMembership.membershipId}`}
              className="text-xl font-semibold text-gray-900 mb-4"
            >
              {actionType === "suspend" ? "Üyeliği Askıya Al" : "Üyeliği İptal Et"}
            </h2>
            <div className="space-y-4">
              <div className="rounded-lg bg-gray-50 p-4 space-y-2">
                <p className="text-sm">
                  <span className="font-medium">Üye:</span> {actionMembership.displayName}
                </p>
                <p className="text-sm">
                  <span className="font-medium">E-posta:</span> {actionMembership.email}
                </p>
                <p className="text-sm">
                  <span className="font-medium">Rol:</span> {getRoleBadge(actionMembership.role)}
                </p>
              </div>

              <FormField
                label="Neden"
                hint="En az 10 karakter (audit kaydı için)"
              >
                <textarea
                  value={actionReason}
                  onChange={(e) => setActionReason(e.target.value)}
                  placeholder="Bu işlemin nedenini açıklayın..."
                  className="min-h-24 w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-900 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500 outline-none"
                  maxLength={500}
                />
                <p className="mt-1 text-xs text-gray-600">
                  {actionReason.length}/500
                </p>
              </FormField>

              {actionType === "revoke" && (
                <div className="rounded-lg bg-red-50 border border-red-200 p-4">
                  <p className="text-sm font-medium text-red-900">
                    ⚠️ Dikkat: İptal edilen üyelik geri alınamaz
                  </p>
                  <p className="text-sm text-red-700 mt-1">
                    Bu kullanıcı tenant'a erişimini kalıcı olarak kaybedecektir.
                  </p>
                </div>
              )}

              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleExecuteAction}
                  disabled={isExecutingAction || actionReason.length < 10}
                  variant={actionType === "revoke" ? "danger" : undefined}
                  className="flex-1"
                >
                  {isExecutingAction
                    ? actionType === "suspend"
                      ? "Askıya Alınıyor..."
                      : "İptal Ediliyor..."
                    : actionType === "suspend"
                    ? "Askıya Al"
                    : "İptal Et"}
                </Button>
                <Button
                  onClick={() => setShowActionDialog(false)}
                  variant="ghost"
                  disabled={isExecutingAction}
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