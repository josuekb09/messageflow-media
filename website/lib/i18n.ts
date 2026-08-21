export const locales = ["en", "fr", "sw"] as const;

export type Locale = (typeof locales)[number];

export const localeNames: Record<Locale, string> = {
  en: "English",
  fr: "Français",
  sw: "Kiswahili",
};

export const localeHtmlLang: Record<Locale, string> = {
  en: "en",
  fr: "fr",
  sw: "sw",
};

export const defaultLocale: Locale = "en";

export const LOCALE_COOKIE = "messageflow-locale";
export const LOCALE_STORAGE_KEY = "messageflow-locale";

export function isLocale(value: string | null | undefined): value is Locale {
  return value === "en" || value === "fr" || value === "sw";
}

export type Dictionary = {
  nav: {
    features: string;
    product: string;
    install: string;
    download: string;
  };
  header: {
    languageLabel: string;
  };
  hero: {
    eyebrow: string;
    title: string;
    subtitle: string;
    secondaryCta: string;
  };
  download: {
    button: string;
    heading: string;
    pageTitle: string;
    pageSubtitle: string;
    note: string;
  };
  product: {
    title: string;
    videoTitle: string;
    screenshotsTitle: string;
    englishUi: string;
    frenchUi: string;
    swahiliUi: string;
  };
  features: {
    title: string;
    items: { title: string; body: string }[];
  };
  install: {
    title: string;
    steps: { n: string; title: string }[];
  };
  footer: {
    blurb: string;
    product: string;
    release: string;
    copyright: string;
  };
};

export const dictionaries: Record<Locale, Dictionary> = {
  en: {
    nav: {
      features: "Features",
      product: "Product",
      install: "Install",
      download: "Download",
    },
    header: {
      languageLabel: "Language",
    },
    hero: {
      eyebrow: "v{version} · {date} · {platform}",
      title: "Welcome to MessageFlow Media",
      subtitle:
        "The ultimate free desktop presentation suite for churches. Search and project sermons, Bibles, and songs offline with ease.",
      secondaryCta: "Install",
    },
    download: {
      button: "Download for Windows",
      heading: "Download for Windows",
      pageTitle: "Download for Windows",
      pageSubtitle:
        "Version {version}, released {date}. Windows 10 / 11.",
      note: "Download MessageFlowMediaSetup.exe, then run the installer.",
    },
    product: {
      title: "Product",
      videoTitle: "Demo video",
      screenshotsTitle: "Interface",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Features",
      items: [
        {
          title: "Multilingual Library",
          body: "Access KJV Bible, Louis Segond, Swahili Biblia Takatifu, and sermons of Brother William Marrion Branham.",
        },
        {
          title: "Keyboard-Driven",
          body: "Navigate verses, paragraphs, and songs using simple keyboard shortcuts. Built for live service.",
        },
        {
          title: "Local-First",
          body: "Works offline. Your library is on your PC. No account, no cloud, no ads.",
        },
      ],
    },
    install: {
      title: "Install",
      steps: [
        { n: "01", title: "Download MessageFlowMediaSetup.exe." },
        { n: "02", title: "Run the installer." },
        { n: "03", title: "Connect projector (Win+P -> Extend)." },
        { n: "04", title: "Launch and project." },
      ],
    },
    footer: {
      blurb:
        "The ultimate free desktop presentation suite for churches.",
      product: "Product",
      release: "Release",
      copyright: "Copyright © 2026 MessageFlow Media.",
    },
  },
  fr: {
    nav: {
      features: "Fonctionnalités",
      product: "Produit",
      install: "Installation",
      download: "Télécharger",
    },
    header: {
      languageLabel: "Langue",
    },
    hero: {
      eyebrow: "v{version} · {date} · {platform}",
      title: "Bienvenue sur MessageFlow Media",
      subtitle:
        "La suite de présentation de bureau gratuite ultime pour les églises. Recherchez et projetez des prédications, des Bibles et des chants hors ligne en toute simplicité.",
      secondaryCta: "Installation",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Télécharger pour Windows",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Téléchargez MessageFlowMediaSetup.exe, puis exécutez le programme.",
    },
    product: {
      title: "Produit",
      videoTitle: "Vidéo de démonstration",
      screenshotsTitle: "Interface",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Fonctionnalités",
      items: [
        {
          title: "Bibliothèque Multilingue",
          body: "Accédez à la Bible Louis Segond, KJV, Swahili Biblia Takatifu, et aux prédications de Frère William Marrion Branham.",
        },
        {
          title: "Contrôle Clavier",
          body: "Naviguez entre les versets, paragraphes et chants avec des raccourcis clavier simples. Conçu pour le direct.",
        },
        {
          title: "100% Hors Ligne",
          body: "Fonctionne sans internet. Votre bibliothèque est sur votre PC. Aucun compte, aucune publicité.",
        },
      ],
    },
    install: {
      title: "Installation",
      steps: [
        { n: "01", title: "Téléchargez MessageFlowMediaSetup.exe." },
        { n: "02", title: "Exécutez le programme." },
        { n: "03", title: "Connectez votre projecteur (Win+P -> Étendre)." },
        { n: "04", title: "Lancez et projetez." },
      ],
    },
    footer: {
      blurb:
        "La suite de présentation de bureau gratuite ultime pour les églises.",
      product: "Produit",
      release: "Version",
      copyright: "Copyright © 2026 MessageFlow Media.",
    },
  },
  sw: {
    nav: {
      features: "Vipengele",
      product: "Bidhaa",
      install: "Sakinisha",
      download: "Pakua",
    },
    header: {
      languageLabel: "Lugha",
    },
    hero: {
      eyebrow: "v{version} · {date} · {platform}",
      title: "Karibu kwenye MessageFlow Media",
      subtitle:
        "Programu bora ya bure ya kompyuta kwa ajili ya vyumba vya vyombo vya habari kanisani. Tafuta na uonyeshe mahubiri, Bibilia, na nyimbo nje ya mtandao.",
      secondaryCta: "Sakinisha",
    },
    download: {
      button: "Pakua kwa ajili ya Windows",
      heading: "Pakua kwa ajili ya Windows",
      pageTitle: "Pakua kwa ajili ya Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Pakua MessageFlowMediaSetup.exe, kisha fungua programu.",
    },
    product: {
      title: "Bidhaa",
      videoTitle: "Video ya onyesho",
      screenshotsTitle: "Kiolesura",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Vipengele",
      items: [
        {
          title: "Maktaba ya Lugha Nyingi",
          body: "Pata Bibilia ya KJV, Louis Segond, Swahili Biblia Takatifu (SWHULB), na mahubiri ya Ndugu William Marrion Branham.",
        },
        {
          title: "Udhibiti wa Kibodi",
          body: "Nenda kwenye mistari, aya, na nyimbo kwa kutumia vibodi. Imetengenezwa kwa ajili ya ibada.",
        },
        {
          title: "Nje ya Mtandao",
          body: "Inafanya kazi bila mtandao. Maktaba yako iko kwenye kompyuta yako. Hakuna akaunti, hakuna matangazo.",
        },
      ],
    },
    install: {
      title: "Sakinisha",
      steps: [
        { n: "01", title: "Pakua MessageFlowMediaSetup.exe." },
        { n: "02", title: "Fungua programu." },
        { n: "03", title: "Unganisha projekta (Win+P -> Extend)." },
        { n: "04", title: "Fungua na uonyeshe maandishi." },
      ],
    },
    footer: {
      blurb:
        "Programu bora ya bure ya kompyuta kwa ajili ya vyumba vya vyombo vya habari kanisani.",
      product: "Bidhaa",
      release: "Toleo",
      copyright: "Hakimiliki © 2026 MessageFlow Media.",
    },
  },
};

export function interpolate(template: string, values: Record<string, string>) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => values[key] ?? "");
}
