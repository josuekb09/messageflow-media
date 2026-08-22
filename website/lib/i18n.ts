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
  library: {
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
        "Free Windows software for the church operator desk. Search and project sermons and Scripture offline in English, French, and Kiswahili — with English and French songbooks included.",
      secondaryCta: "Install",
    },
    download: {
      button: "Download for Windows",
      heading: "Download for Windows",
      pageTitle: "Download for Windows",
      pageSubtitle:
        "Version {version}, released {date}. Windows 10 / 11.",
      note: "Download MessageFlowMediaSetup.exe, then run the installer. English and French songbooks are included; the Swahili songbook is forthcoming.",
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
          title: "Multilingual library",
          body: "1,208 English, 384 French, and 622 Swahili sermons of Brother William Marrion Branham. Bibles: KJV, Louis Segond, and SWHULB.",
        },
        {
          title: "Operator workflow",
          body: "Built for live church projection. Search, prepare, and project verses, sermon paragraphs, and hymns from the operator desk with keyboard shortcuts.",
        },
        {
          title: "Works offline",
          body: "The library lives on your PC. No account, no cloud, no ads.",
        },
      ],
    },
    library: {
      title: "What's included",
      items: [
        {
          title: "English",
          body: "1,208 sermons, English songs, and the King James Version (KJV).",
        },
        {
          title: "Français",
          body: "384 sermons, 866 hymns (Dinanga and Chants des aigles), and Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 sermons and the SWHULB Bible. Swahili songbook forthcoming.",
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
        "Free Windows desktop software for church projection — sermons, Bibles, and hymns, fully offline.",
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
        "Logiciel Windows gratuit pour le pupitre de projection. Recherchez et projetez prédications et Écritures hors ligne en anglais, français et kiswahili — cantiques anglais et français inclus.",
      secondaryCta: "Installation",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Télécharger pour Windows",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Téléchargez MessageFlowMediaSetup.exe, puis exécutez le programme. Les recueils anglais et français sont inclus ; le recueil swahili est à venir.",
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
          title: "Bibliothèque multilingue",
          body: "1 208 prédications en anglais, 384 en français et 622 en kiswahili de Frère William Marrion Branham. Bibles : KJV, Louis Segond et SWHULB.",
        },
        {
          title: "Pupitre de projection",
          body: "Conçu pour le direct. Recherchez, préparez et projetez versets, paragraphes de prédication et cantiques depuis le pupitre avec des raccourcis clavier.",
        },
        {
          title: "Hors ligne",
          body: "La bibliothèque reste sur votre PC. Aucun compte, aucun cloud, aucune publicité.",
        },
      ],
    },
    library: {
      title: "Contenu de la bibliothèque",
      items: [
        {
          title: "English",
          body: "1 208 prédications, chants anglais et King James Version (KJV).",
        },
        {
          title: "Français",
          body: "384 prédications, 866 cantiques (Dinanga et Chants des aigles) et Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 prédications et la Bible SWHULB. Recueil swahili à venir.",
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
        "Logiciel Windows gratuit pour la projection à l'église — prédications, Bibles et cantiques, entièrement hors ligne.",
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
        "Programu ya bure ya Windows kwa dawati la kuonyesha kanisani. Tafuta na uonyeshe mahubiri na Maandiko nje ya mtandao kwa Kiingereza, Kifaransa na Kiswahili — nyimbo za Kiingereza na Kifaransa zimo.",
      secondaryCta: "Sakinisha",
    },
    download: {
      button: "Pakua kwa ajili ya Windows",
      heading: "Pakua kwa ajili ya Windows",
      pageTitle: "Pakua kwa ajili ya Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Pakua MessageFlowMediaSetup.exe, kisha fungua programu. Nyimbo za Kiingereza na Kifaransa zimo; kitabu cha nyimbo za Kiswahili kinakuja.",
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
          title: "Maktaba ya lugha nyingi",
          body: "Mahubiri 1,208 ya Kiingereza, 384 ya Kifaransa, na 622 ya Kiswahili ya Ndugu William Marrion Branham. Biblia: KJV, Louis Segond, na SWHULB.",
        },
        {
          title: "Kazi ya operator",
          body: "Imetengenezwa kwa ajili ya kuonyesha ibada moja kwa moja. Tafuta, andaa, na uonyeshe mistari, aya za mahubiri, na nyimbo kutoka dawati kwa vibodi.",
        },
        {
          title: "Nje ya mtandao",
          body: "Maktaba yako iko kwenye kompyuta yako. Hakuna akaunti, hakuna wingu, hakuna matangazo.",
        },
      ],
    },
    library: {
      title: "Kilichomo kwenye maktaba",
      items: [
        {
          title: "English",
          body: "Mahubiri 1,208, nyimbo za Kiingereza, na King James Version (KJV).",
        },
        {
          title: "Français",
          body: "Mahubiri 384, nyimbo 866 (Dinanga na Chants des aigles), na Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "Mahubiri 622 na Biblia ya SWHULB. Kitabu cha nyimbo za Kiswahili kinakuja.",
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
        "Programu ya bure ya Windows kwa kuonyesha kanisani — mahubiri, Biblia, na nyimbo, nje ya mtandao.",
      product: "Bidhaa",
      release: "Toleo",
      copyright: "Hakimiliki © 2026 MessageFlow Media.",
    },
  },
};

export function interpolate(template: string, values: Record<string, string>) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => values[key] ?? "");
}
