"use client";

import { useState, useEffect } from "react";
import { apiClient } from "@/shared/api/client";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { FormField, TextInput } from "@/shared/ui/form-field";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";

interface Business {
  tenantId: string;
  businessName: string;
  businessSlug: string;
  category: string;
  status: "Active" | "Suspended";
  ownerEmail: string;
  totalBookings: number;
  createdAt: string;
}

interface Booking {
  bookingId: string;
  businessName: string;
  customerEmail: string;
  status: string;
  appointmentStart: string;
  serviceName: string;
}

export default function AdminPage() {
  const [activeTab, setActiveTab] = useState<"businesses" | "bookings" | "users">("businesses");
  
  // Businesses state
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [isLoadingBusinesses, setIsLoadingBusinesses] = useState(true);
  const [showAddBusinessDialog, setShowAddBusinessDialog] = useState(false);
  const [newBusinessName, setNewBusinessName] = useState("");
  const [newBusinessSlug, setNewBusinessSlug] = useState("");
  const [newOwnerEmail, setNewOwnerEmail] = useState("");
  const [isAddingBusiness, setIsAddingBusiness] = useState(false);
  
  // Bookings state
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoadingBookings, setIsLoadingBookings] = useState(false);
  
  // Users state
  const [users, setUsers] = useState<any[]>([]);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  
  const [toast, setToast] = useState<string | null>(null);

  // Load businesses
  useEffect(() => {
    loadBusinesses();
  }, []);

  async function loadBusinesses() {
    setIsLoadingBusinesses(true);
    try {
      // TODO: Implement backend endpoint: GET /api/admin/businesses
      // const result = await apiClient.GET("/api/admin/businesses");
      
      // Placeholder: simulate businesses
      await new Promise((resolve) => setTimeout(resolve, 500));
      setBusinesses([
        {
          tenantId: "1",
          businessName: "Demo Barber Shop",
          businessSlug: "demo-barber-shop",
          category: "Berber",
          status: "Active",
          ownerEmail: "owner@demo.com",
          totalBookings: 150,
          createdAt: "2026-01-15T10:00:00Z",
        },
        {
          tenantId: "2",
          businessName: "Test Spa",
          businessSlug: "test-spa",
          category: "Spa",
          status: "Active",
          ownerEmail: "spa@test.com",
          totalBookings: 89,
          createdAt: "2026-02-01T10:00:00Z",
        },
      ]);
    } catch {
      showToast("İşletmeler yüklenirken hata oluştu");
    } finally {
      setIsLoadingBusinesses(false);
    }
  }

  async function loadBookings() {
    setIsLoadingBookings(true);
    try {
      // TODO: Implement backend endpoint: GET /api/admin/bookings
      // const result = await apiClient.GET("/api/admin/bookings");
      
      // Placeholder: simulate bookings
      await new Promise((resolve) => setTimeout(resolve, 500));
      setBookings([
        {
          bookingId: "1",
          businessName: "Demo Barber Shop",
          customerEmail: "customer@example.com",
          status: "Confirmed",
          appointmentStart: "2026-06-21T14:00:00Z",
          serviceName: "Saç Kesimi",
        },
        {
          bookingId: "2",
          businessName: "Test Spa",
          customerEmail: "user2@example.com",
          status: "PendingApproval",
          appointmentStart: "2026-06-22T10:00:00Z",
          serviceName: "Masaj",
        },
      ]);
    } catch {
      showToast("Randevular yüklenirken hata oluştu");
    } finally {
      setIsLoadingBookings(false);
    }
  }

  async function loadUsers() {
    setIsLoadingUsers(true);
    try {
      // TODO: Implement backend endpoint: GET /api/admin/users
      // const result = await apiClient.GET("/api/admin/users");
      
      // Placeholder: simulate users
      await new Promise((resolve) => setTimeout(resolve, 500));
      setUsers([
        {
          userId: "1",
          email: "admin@example.com",
          displayName: "Admin User",
          createdAt: "2026-01-01T00:00:00Z",
        },
        {
          userId: "2",
          email: "customer@example.com",
          displayName: "Customer User",
          createdAt: "2026-01-15T00:00:00Z",
        },
      ]);
    } catch {
      showToast("Kullanıcılar yüklenirken hata oluştu");
    } finally {
      setIsLoadingUsers(false);
    }
  }

  // Add business
  async function handleAddBusiness() {
    if (!newBusinessName || !newBusinessSlug || !newOwnerEmail) {
      showToast("Lütfen tüm alanları doldurun");
      return;
    }

    setIsAddingBusiness(true);
    try {
      // TODO: Implement backend endpoint: POST /api/admin/businesses
      // const result = await apiClient.POST("/api/admin/businesses", {
      //   body: {
      //     businessName: newBusinessName.trim(),
      //     businessSlug: newBusinessSlug.trim(),
      //     ownerEmail: newOwnerEmail.trim(),
      //   },
      // });
      
      // Placeholder: simulate adding business
      await new Promise((resolve) => setTimeout(resolve, 1000));
      
      showToast("İşletme başarıyla eklendi (demo)");
      setShowAddBusinessDialog(false);
      setNewBusinessName("");
      setNewBusinessSlug("");
      setNewOwnerEmail("");
      loadBusinesses();
    } catch {
      showToast("İşletme eklenirken hata oluştu");
    } finally {
      setIsAddingBusiness(false);
    }
  }

  // Toggle business status
  async function toggleBusinessStatus(business: Business) {
    const newStatus = business.status === "Active" ? "Suspended" : "Active";
    try {
      // TODO: Implement backend endpoint: POST /api/admin/businesses/{tenantId}/toggle-status
      // const result = await apiClient.POST(`/api/admin/businesses/${business.tenantId}/toggle-status`, {
      //   body: { status: newStatus },
      // });
      
      // Placeholder: simulate status toggle
      await new Promise((resolve) => setTimeout(resolve, 500));
      
      showToast(`İşletme durumu ${newStatus === "Active" ? "aktif" : "askıya alındı"} (demo)`);
      loadBusinesses();
    } catch {
      showToast("Durum değiştirilirken hata oluştu");
    }
  }

  function showToast(message: string) {
    setToast(message);
    window.setTimeout(() => setToast(null), 3200);
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Yönetici Paneli</h1>
        <p className="mt-2 text-gray-600">
          Platform genelini yönetin
        </p>
      </div>

      {/* Tabs */}
      <div className="flex gap-4 mb-6 border-b border-gray-200">
        <button
          onClick={() => {
            setActiveTab("businesses");
            if (businesses.length === 0) loadBusinesses();
          }}
          className={`pb-3 px-4 font-medium transition-colors ${
            activeTab === "businesses"
              ? "text-indigo-600 border-b-2 border-indigo-600"
              : "text-gray-600 hover:text-gray-900"
          }`}
        >
          İşletmeler
        </button>
        <button
          onClick={() => {
            setActiveTab("bookings");
            if (bookings.length === 0) loadBookings();
          }}
          className={`pb-3 px-4 font-medium transition-colors ${
            activeTab === "bookings"
              ? "text-indigo-600 border-b-2 border-indigo-600"
              : "text-gray-600 hover:text-gray-900"
          }`}
        >
          Randevular
        </button>
        <button
          onClick={() => {
            setActiveTab("users");
            if (users.length === 0) loadUsers();
          }}
          className={`pb-3 px-4 font-medium transition-colors ${
            activeTab === "users"
              ? "text-indigo-600 border-b-2 border-indigo-600"
              : "text-gray-600 hover:text-gray-900"
          }`}
        >
          Kullanıcılar
        </button>
      </div>

      {/* Businesses Tab */}
      {activeTab === "businesses" && (
        <Card className="p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl font-semibold text-gray-900">
              İşletmeler ({businesses.length})
            </h2>
            <Button onClick={() => setShowAddBusinessDialog(true)}>
              Yeni İşletme Ekle
            </Button>
          </div>

          {isLoadingBusinesses ? (
            <div className="text-center py-12 text-gray-600">
              Yükleniyor...
            </div>
          ) : businesses.length === 0 ? (
            <div className="text-center py-12 text-gray-600">
              <p className="text-lg font-medium mb-2">Henüz işletme yok</p>
              <p>İlk işletmeyi eklemek için "Yeni İşletme Ekle" butonunu kullanın</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      İşletme
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Kategori
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Durum
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Toplam Randevu
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Sahibi
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Kayıt Tarihi
                    </th>
                    <th className="text-right py-3 px-4 font-medium text-gray-900">
                      İşlemler
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {businesses.map((business) => (
                    <tr key={business.tenantId} className="border-b border-gray-100">
                      <td className="py-3 px-4">
                        <div>
                          <div className="font-medium text-gray-900">
                            {business.businessName}
                          </div>
                          <div className="text-sm text-gray-600">
                            /{business.businessSlug}
                          </div>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {business.category}
                      </td>
                      <td className="py-3 px-4">
                        <span
                          className={`px-2 py-1 rounded-full text-xs font-medium ${
                            business.status === "Active"
                              ? "bg-green-100 text-green-800"
                              : "bg-yellow-100 text-yellow-800"
                          }`}
                        >
                          {business.status === "Active" ? "Aktif" : "Askıda"}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-900">
                        {business.totalBookings}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {business.ownerEmail}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {new Date(business.createdAt).toLocaleDateString("tr-TR")}
                      </td>
                      <td className="py-3 px-4 text-right">
                        <Button
                          variant="ghost"
                          onClick={() => toggleBusinessStatus(business)}
                        >
                          {business.status === "Active" ? "Askıya Al" : "Aktif Et"}
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {/* Bookings Tab */}
      {activeTab === "bookings" && (
        <Card className="p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Tüm Randevular ({bookings.length})
          </h2>

          {isLoadingBookings ? (
            <div className="text-center py-12 text-gray-600">
              Yükleniyor...
            </div>
          ) : bookings.length === 0 ? (
            <div className="text-center py-12 text-gray-600">
              <p className="text-lg font-medium mb-2">Henüz randevu yok</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      İşletme
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Müşteri
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Hizmet
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Durum
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Tarih
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {bookings.map((booking) => (
                    <tr key={booking.bookingId} className="border-b border-gray-100">
                      <td className="py-3 px-4 font-medium text-gray-900">
                        {booking.businessName}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {booking.customerEmail}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {booking.serviceName}
                      </td>
                      <td className="py-3 px-4">
                        <span
                          className={`px-2 py-1 rounded-full text-xs font-medium ${
                            booking.status === "Confirmed"
                              ? "bg-green-100 text-green-800"
                              : "bg-yellow-100 text-yellow-800"
                          }`}
                        >
                          {booking.status === "Confirmed"
                            ? "Onaylandı"
                            : booking.status === "PendingApproval"
                            ? "Bekliyor"
                            : booking.status}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {new Date(booking.appointmentStart).toLocaleString("tr-TR")}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {/* Users Tab */}
      {activeTab === "users" && (
        <Card className="p-6">
          <h2 className="text-xl font-semibold text-gray-900 mb-6">
            Kullanıcılar ({users.length})
          </h2>

          {isLoadingUsers ? (
            <div className="text-center py-12 text-gray-600">
              Yükleniyor...
            </div>
          ) : users.length === 0 ? (
            <div className="text-center py-12 text-gray-600">
              <p className="text-lg font-medium mb-2">Henüz kullanıcı yok</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200">
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Kullanıcı
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      E-posta
                    </th>
                    <th className="text-left py-3 px-4 font-medium text-gray-900">
                      Kayıt Tarihi
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.userId} className="border-b border-gray-100">
                      <td className="py-3 px-4 font-medium text-gray-900">
                        {user.displayName}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {user.email}
                      </td>
                      <td className="py-3 px-4 text-sm text-gray-600">
                        {new Date(user.createdAt).toLocaleDateString("tr-TR")}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {toast && (
        <div className="fixed bottom-5 left-1/2 z-50 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-gray-200 bg-white px-5 py-3 text-sm text-gray-900 shadow-lg">
          {toast}
        </div>
      )}

      {/* Add Business Dialog */}
      {showAddBusinessDialog && (
        <DialogOverlay onEscapeKeyDown={() => setShowAddBusinessDialog(false)}>
          <DialogPanel titleId="add-business-dialog">
            <h2
              id="add-business-dialog"
              className="text-xl font-semibold text-gray-900 mb-4"
            >
              Yeni İşletme Ekle
            </h2>
            <div className="space-y-4">
              <FormField label="İşletme Adı">
                <TextInput
                  type="text"
                  value={newBusinessName}
                  onChange={(e) => setNewBusinessName(e.target.value)}
                  placeholder="Örnek: İstanbul Hair Salon"
                  required
                />
              </FormField>

              <FormField label="Business Slug">
                <TextInput
                  type="text"
                  value={newBusinessSlug}
                  onChange={(e) => setNewBusinessSlug(e.target.value)}
                  placeholder="istanbul-hair-salon"
                  required
                />
              </FormField>

              <FormField label="İşletme Sahibi E-postası">
                <TextInput
                  type="email"
                  value={newOwnerEmail}
                  onChange={(e) => setNewOwnerEmail(e.target.value)}
                  placeholder="ornek@email.com"
                  required
                />
              </FormField>

              <div className="flex gap-3 pt-4">
                <Button
                  onClick={handleAddBusiness}
                  disabled={isAddingBusiness}
                  className="flex-1"
                >
                  {isAddingBusiness ? "Ekleniyor..." : "İşletme Ekle"}
                </Button>
                <Button
                  onClick={() => setShowAddBusinessDialog(false)}
                  variant="ghost"
                  disabled={isAddingBusiness}
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