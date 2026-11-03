/**
 * Photos and static content of the public web. `null` photos show the design's placeholder until the client sends
 * them: put the file in `web/public/fotos/` and write its path here (e.g. "/fotos/zoila.jpg").
 */
export const FOTOS = {
  zoila: null as string | null,
  zoilaConAlumnas: null as string | null,
  competicion: null as string | null,
  dandoClase: null as string | null,
  claseAcademia: null as string | null,
  grabandoClase: null as string | null,
  spa: null as string | null,
  trabajos: [null, null, null, null] as (string | null)[],
  grupoFormacion: null as string | null,
  alumnasPracticando: null as string | null,
  entregaDiplomas: null as string | null,
  graduaciones: [null, null, null, null, null] as (string | null)[],
};

/** Gallery of each course on the Formaciones page, by slug. Missing slugs show placeholders. */
export const GALERIAS: Record<string, (string | null)[]> = {};

export const TRAYECTORIA = [
  "17 años de experiencia en el cuidado de las uñas",
  "Campeona internacional",
  "Venezolana-ecuatoriana afincada en Barcelona",
  "Máster educadora con +5.000 alumnas certificadas en +10 países",
  "Especialista en uñas de salón y técnicas Dual y Gel",
];

export const TRAYECTORIA_MOVIL = [
  "17 años de experiencia",
  "Campeona internacional",
  "Venezolana-ecuatoriana afincada en Barcelona",
  "+5.000 alumnas certificadas en +10 países",
  "Especialista en uñas de salón, Dual y Gel",
];

export const TESTIMONIOS = [
  { texto: "[Testimonio de una alumna sobre la formación y lo que consiguió después]", nombre: "[Nombre de la alumna]", detalle: "[Formación · ciudad]" },
  { texto: "[Testimonio de una alumna sobre la formación y lo que consiguió después]", nombre: "[Nombre de la alumna]", detalle: "[Formación · ciudad]" },
  { texto: "[Testimonio de una alumna sobre la formación y lo que consiguió después]", nombre: "[Nombre de la alumna]", detalle: "[Formación · ciudad]" },
];
