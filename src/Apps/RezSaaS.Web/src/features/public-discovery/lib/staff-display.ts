// PublicStaffDisplayPolicy (backend: Organization/Domain/PublicStaffDisplayPolicy.cs)
// enum'u .ToString() ile serialize edilir, yani tel uzerinde "ShowNames" | "HideNames".
//
// DIKKAT: profil yaniti policy'yi ZATEN uygular (HideNames -> staffMembers: []).
// Ama /slots yanitindaki staffCandidates policy'ye TABI DEGIL -- PublicSlotSearchComposer
// personel adini kosulsuz doner. Yani slot verisindeki isim, isletme "gizle" demis olsa bile
// gelir. Bu yuzden gorunurluk karari tek bir yerde toplaniyor ve slot personel adi
// EKRANA HIC BASILMIYOR.
const showNamesPolicy = "ShowNames";

export function showStaffNames(policy?: string | null) {
  // Bilinmeyen/eksik policy GIZLE tarafina duser: isim sizdirmak, isim gostermemekten
  // pahali bir hata.
  return policy?.toLowerCase() === showNamesPolicy.toLowerCase();
}
