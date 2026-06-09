export function formatBranchDateTime(
  valueUtc: string,
  branchTimeZoneId: string
) {
  const value = new Date(valueUtc);

  if (Number.isNaN(value.getTime())) {
    return "Zaman bilgisi okunamıyor";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: branchTimeZoneId
  }).format(value);
}
