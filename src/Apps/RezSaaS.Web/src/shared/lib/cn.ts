import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

// twMerge cakisan Tailwind siniflarini cozer: cn("p-2", "p-4") -> "p-4".
// shadcn componentleri bu davranisa GUVENIR: variant + className override kalibi
// bunsuz calismaz (kullanicinin verdigi className, variant'in sinifini ezemez).
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
