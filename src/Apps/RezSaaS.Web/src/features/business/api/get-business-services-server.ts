import { cookies } from "next/headers";
import {
  mapDefined,
  toCatalogResourceType,
  toCatalogService,
  toCatalogVariant,
  type CatalogResourceType,
  type ServiceWithVariants
} from "../lib/service-catalog";
import { createServerApiClient } from "@/shared/api/server-client";
import type { BusinessTenantContext } from "./get-business-context";
import "server-only";

// Fiyat ve sure Service'te DEGIL, ServiceVariant'ta yasar. Dolayisiyla ust listedeki
// "kac secenek" ve "fiyat araligi" sutunlari varyant verisi OLMADAN cizilemez.
export type ServicesState =
  | {
      readonly kind: "ready";
      readonly resourceTypes: CatalogResourceType[];
      readonly services: ServiceWithVariants[];
      readonly tenantId: string;
    }
  | {
      readonly kind: "unavailable";
      readonly reason: string;
      readonly resourceTypes: [];
      readonly services: [];
    };

function unavailable(reason: string): ServicesState {
  return { kind: "unavailable", reason, resourceTypes: [], services: [] };
}

export async function getBusinessServicesServer(
  tenant: BusinessTenantContext
): Promise<ServicesState> {
  const tenantId = tenant.tenantId;

  if (!tenantId) {
    return unavailable("İşletme bilgisi doğrulanamadı.");
  }

  try {
    const cookieHeader = (await cookies()).toString();
    const client = createServerApiClient(cookieHeader, tenantId);

    const [servicesResult, resourceTypesResult] = await Promise.all([
      client.GET("/api/business/services"),
      client.GET("/api/business/resource-types")
    ]);

    if (!servicesResult.response.ok) {
      return unavailable("Hizmetler şu anda alınamadı.");
    }

    const services = mapDefined(servicesResult.data ?? [], toCatalogService);

    // N+1 KACINILMAZ: varyantlari toplu donen bir uc YOK, yalnizca hizmet basina uc var.
    // Paralel cekiyoruz. Yan faydasi: tum varyantlar zaten bellekte oldugu icin ekranda
    // bir hizmeti ACMAK EK ISTEK GEREKTIRMEZ (accordion tercihinin gerekcesi).
    const withVariants = await Promise.all(
      services.map(async (service): Promise<ServiceWithVariants> => {
        const { data, response } = await client.GET(
          "/api/business/services/{serviceId}/variants",
          { params: { path: { serviceId: service.id } } }
        );

        return response.ok
          ? {
              ...service,
              variants: mapDefined(data ?? [], toCatalogVariant),
              variantsUnavailable: false
            }
          : { ...service, variants: [], variantsUnavailable: true };
      })
    );

    return {
      kind: "ready",
      // Kaynak turleri cekilemezse varyant formu yine calisir; secili tur KORUNUR
      // (form, listede olmayan id'yi "Bilinmeyen tür" olarak tasir ve geri gonderir).
      resourceTypes: resourceTypesResult.response.ok
        ? mapDefined(resourceTypesResult.data ?? [], toCatalogResourceType)
        : [],
      services: withVariants,
      tenantId
    };
  } catch {
    return unavailable("Hizmetler şu anda yüklenemedi.");
  }
}
