import { Suspense } from 'react';
import Link from 'next/link';
import { getCustomerAppointmentHistory } from '@/features/customer/api/get-appointment-history';
import { AppointmentHistoryList } from '@/features/customer/components/AppointmentHistoryList';
import { AppointmentHistorySkeleton } from '@/features/customer/components/AppointmentHistorySkeleton';
import { EmptyState } from '@/shared/ui/empty-state';
import { Button } from '@/shared/ui/button';

export default function CustomerAppointmentsPage() {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-[var(--rs-ink)]">Randevularım</h1>
        <p className="mt-2 text-[var(--rs-muted)]">Randevu geçmişinizi ve gelecek rezervasyonlarınızı görüntüleyin</p>
      </div>

      <Suspense fallback={<AppointmentHistorySkeleton />}>
        <AppointmentsContent />
      </Suspense>
    </div>
  );
}

async function AppointmentsContent() {
  const history = await getCustomerAppointmentHistory(50);

  if (history.kind === 'unavailable') {
    return (
      <EmptyState
        title="Randevular yüklenemedi"
        description={history.reason}
      />
    );
  }

  if (!history.items || history.items.length === 0) {
    return (
      <EmptyState
        title="Henüz bir randevunuz yok"
        description="İlk rezervasyonunuzu yapmak için işletmeleri keşfetmeye başlayın"
        action={
          <Link href="/kesfet">
            <Button>İşletmeleri Keşfet</Button>
          </Link>
        }
      />
    );
  }

  return <AppointmentHistoryList items={history.items} />;
}