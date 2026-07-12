"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import {
  CATALOG_CURRENCY,
  DURATION_MAX,
  NAME_MAX,
  NAME_MIN,
  parseDurationInput,
  parsePriceInput,
  type CatalogResourceType,
  type CatalogVariant,
  type VariantFormValues
} from "@/features/business/lib/service-catalog";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";

/**
 * VARYANT FORMU -- fiyatin ve surenin YASADIGI yer.
 *
 * Bu form TEK BIR PARCA olarak varyantin TAMAMINI tasir. Boyle olmasinin sebebi
 * kozmetik degil: guncelleme ucu "PATCH" adini tasir ama PUT gibi davranir; gonderilmeyen
 * alan korunmaz, SIFIRLANIR. "Sadece fiyati duzenle" diye kucuk bir form yazsaydik,
 * kaydedildiginde varyantin suresi 0'a duser (400) ve kaynak turu sessizce silinirdi.
 *
 * Form her zaman dort alani da toplar; besinci alan (para birimi) sabittir ve
 * buildVariantPayload icinde eklenir. Ekran ham API cagrisi YAPMAZ -- gonderimi
 * ebeveyn, business-service-client uzerinden yapar.
 */

/** Radix Select bos string degeri kabul etmez; "kaynak turu yok" icin sentinel. */
const NO_RESOURCE_TYPE = "none";

type VariantFormShape = {
  duration: string;
  name: string;
  price: string;
  resourceTypeId: string;
};

// Sayilari string olarak tutuyoruz: salon sahibi "400,50" yazar ve <input type="number">
// virgullu girdiyi sessizce BOSA dusurur. Ayristirmayi biz yapiyoruz.
const variantSchema = z.object({
  duration: z
    .string()
    .refine((value) => parseDurationInput(value) !== null, {
      message: `Süreyi dakika olarak yaz (1 - ${DURATION_MAX}).`
    }),
  name: z
    .string()
    .trim()
    .min(NAME_MIN, `Seçenek adı en az ${NAME_MIN} karakter olmalı.`)
    .max(NAME_MAX, `Seçenek adı en fazla ${NAME_MAX} karakter olabilir.`),
  price: z.string().refine((value) => parsePriceInput(value) !== null, {
    message: "Geçerli bir fiyat yaz. Örnek: 400 veya 400,50"
  }),
  resourceTypeId: z.string()
});

export type ServiceVariantFormProps = {
  /** Duzenlenen varyant. Yoksa yeni varyant olusturulur. */
  variant?: CatalogVariant | null;
  resourceTypes: readonly CatalogResourceType[];
  submitting: boolean;
  onCancel: () => void;
  onSubmit: (values: VariantFormValues) => void;
};

export function ServiceVariantForm({
  onCancel,
  onSubmit,
  resourceTypes,
  submitting,
  variant
}: ServiceVariantFormProps) {
  const isEdit = Boolean(variant);

  const form = useForm<VariantFormShape>({
    defaultValues: {
      duration: variant ? String(variant.durationMinutes) : "30",
      name: variant?.name ?? "",
      price: variant ? String(variant.priceAmount).replace(".", ",") : "",
      resourceTypeId: variant?.requiredResourceTypeId ?? NO_RESOURCE_TYPE
    },
    resolver: zodResolver(variantSchema)
  });

  const selectedResourceTypeId = form.watch("resourceTypeId");

  // Kaynak turleri cekilemediyse (ya da tur sonradan silindiyse) varyantin tasidigi id
  // listede olmayabilir. Onu ATMAYIZ: secenek olarak geri ekleyip kaydederken aynen
  // geri gondeririz. Aksi halde "sadece fiyati degistirdim" diyen kullanici, farkinda
  // olmadan varyantin kaynak turunu de silmis olurdu.
  const options: CatalogResourceType[] = [...resourceTypes];
  const hasSelected =
    selectedResourceTypeId === NO_RESOURCE_TYPE ||
    options.some((option) => option.id === selectedResourceTypeId);

  if (!hasSelected) {
    options.unshift({
      displayName: "Bilinmeyen tür (korunuyor)",
      id: selectedResourceTypeId
    });
  }

  function handleSubmit(shape: VariantFormShape) {
    const durationMinutes = parseDurationInput(shape.duration);
    const priceAmount = parsePriceInput(shape.price);

    // zod zaten dogruladi; burasi tip daraltmasi icin.
    if (durationMinutes === null || priceAmount === null) {
      return;
    }

    onSubmit({
      durationMinutes,
      name: shape.name,
      priceAmount,
      requiredResourceTypeId:
        shape.resourceTypeId === NO_RESOURCE_TYPE ? null : shape.resourceTypeId
    });
  }

  return (
    <Form {...form}>
      <form className="space-y-5" onSubmit={form.handleSubmit(handleSubmit)}>
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Seçenek adı</FormLabel>
              <FormControl>
                <Input
                  autoComplete="off"
                  className="min-h-11"
                  placeholder="Örn. Kısa saç"
                  {...field}
                />
              </FormControl>
              <FormDescription>
                Müşteri bu adı görür. Örn. &quot;Kısa saç&quot;, &quot;Uzun saç&quot;.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid gap-5 sm:grid-cols-2">
          <FormField
            control={form.control}
            name="duration"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Süre (dakika)</FormLabel>
                <FormControl>
                  <Input
                    className="min-h-11"
                    inputMode="numeric"
                    placeholder="30"
                    {...field}
                  />
                </FormControl>
                <FormDescription>Randevu bu kadar yer kaplar.</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="price"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Fiyat ({CATALOG_CURRENCY})</FormLabel>
                <FormControl>
                  <Input
                    className="min-h-11"
                    inputMode="decimal"
                    placeholder="400,00"
                    {...field}
                  />
                </FormControl>
                <FormDescription>
                  Kuruş yazabilirsin. Örn. 400,50
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="resourceTypeId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Gerekli ekipman türü</FormLabel>
              <Select
                onValueChange={field.onChange}
                value={field.value}
              >
                <FormControl>
                  <SelectTrigger className="min-h-11 w-full">
                    <SelectValue placeholder="Seç" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value={NO_RESOURCE_TYPE}>Gerekmiyor</SelectItem>
                  {options.map((option) => (
                    <SelectItem key={option.id} value={option.id}>
                      {option.displayName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormDescription>
                Bu seçenek belirli bir koltuk/ekipman gerektiriyorsa seç.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        {isEdit ? (
          // TUZAK 4: fiyat degisimi MEVCUT randevulari ETKILEMEZ -- randevu satiri fiyati
          // talep aninda sunucuda snapshot'lar. "Mevcut randevular etkilenecek" demek
          // YANLIS olurdu. Bilgi gorunur etikette; tooltip'te degil.
          <p className="rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground">
            Mevcut randevuların fiyatı değişmez. Yeni fiyat, bundan sonra alınan
            randevularda geçerli olur.
          </p>
        ) : null}

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button
            className="min-h-11"
            disabled={submitting}
            onClick={onCancel}
            type="button"
            variant="outline"
          >
            Vazgeç
          </Button>
          <Button className="min-h-11" disabled={submitting} type="submit">
            {submitting ? "Kaydediliyor…" : isEdit ? "Değişiklikleri kaydet" : "Seçeneği ekle"}
          </Button>
        </div>
      </form>
    </Form>
  );
}
