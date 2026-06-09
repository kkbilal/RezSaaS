import type { components } from "./rezsaas-api.generated";

export type ApiSchema<Name extends keyof components["schemas"]> =
  components["schemas"][Name];
