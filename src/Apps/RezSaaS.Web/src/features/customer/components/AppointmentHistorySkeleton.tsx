export function AppointmentHistorySkeleton() {
  return (
    <div className="grid gap-4">
      {[...Array(3)].map((_, index) => (
        <div
          key={index}
          className="bg-white border border-gray-200 rounded-lg p-5 animate-pulse"
        >
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-3">
              <div className="h-6 w-20 bg-gray-200 rounded-full" />
              <div className="h-6 w-16 bg-gray-200 rounded-full" />
              <div className="h-5 w-32 bg-gray-200 rounded" />
            </div>

            <div className="space-y-2">
              <div className="h-7 w-3/4 bg-gray-200 rounded" />
              <div className="h-5 w-1/2 bg-gray-200 rounded" />
            </div>

            <div className="grid gap-3 md:grid-cols-3">
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-3">
                <div className="h-4 w-12 bg-gray-200 rounded mb-2" />
                <div className="h-5 w-20 bg-gray-200 rounded" />
              </div>
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-3">
                <div className="h-4 w-12 bg-gray-200 rounded mb-2" />
                <div className="h-5 w-16 bg-gray-200 rounded" />
              </div>
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-3">
                <div className="h-4 w-12 bg-gray-200 rounded mb-2" />
                <div className="h-5 w-20 bg-gray-200 rounded" />
              </div>
            </div>

            <div className="pt-2">
              <div className="h-10 w-40 bg-gray-200 rounded" />
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}