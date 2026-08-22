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
        "Free Windows software for the church operator desk. Search and project sermons and Scripture offline in English, French, and Kiswahili — with English, French, and Swahili songbooks included.",
      secondaryCta: "Install",
    },
    download: {
      button: "Download for Windows",
      heading: "Download for Windows",
      pageTitle: "Download for Windows",
      pageSubtitle:
        "Version {version}, released {date}. Windows 10 / 11.",
      note: "Opens the v1.0.2 GitHub release. Download MessageFlowMediaSetup.exe, run it on Windows 10 or 11 (64-bit), then press Win+P and choose Extend before you project. English, French, and Swahili libraries are included.",
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
          body: "384 sermons, 499 hymns (Recueil de cantiques français, Tabernacle Dinanga), and Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 sermons, 281 hymns from PowerPoint (Nyimbo za Kiswahili), and the SWHULB Bible.",
        },
      ],
    },
    install: {
      title: "Install",
      steps: [
        { n: "01", title: "Open Download and save MessageFlowMediaSetup.exe from the v1.0.2 GitHub release." },
        { n: "02", title: "Run the installer on Windows 10 or 11 (64-bit). Prefer drive D: for the church media disk." },
        { n: "03", title: "Connect the projector or TV, press Win+P, and choose Extend." },
        { n: "04", title: "Launch MessageFlow Media, pick a language, search, then press Ctrl+P to project." },
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
        "Logiciel Windows gratuit pour le pupitre de projection. Recherchez et projetez prédications et Écritures hors ligne en anglais, français et kiswahili — cantiques anglais, français et swahili inclus.",
      secondaryCta: "Installation",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Télécharger pour Windows",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Ouvre la version v1.0.2 sur GitHub. Téléchargez MessageFlowMediaSetup.exe, installez-le sous Windows 10 ou 11 (64 bits), puis appuyez sur Win+P et choisissez Étendre avant de projeter. Les bibliothèques anglaise, française et swahili sont incluses.",
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
          body: "384 prédications, 499 cantiques (Recueil de cantiques français, Tabernacle Dinanga) et Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 prédications, 281 cantiques (Nyimbo za Kiswahili) et la Bible SWHULB.",
        },
      ],
    },
    install: {
      title: "Installation",
      steps: [
        { n: "01", title: "Ouvrez Télécharger et enregistrez MessageFlowMediaSetup.exe depuis GitHub v1.0.2." },
        { n: "02", title: "Exécutez l'installateur sous Windows 10 ou 11 (64 bits). Préférez le disque D: pour le pupitre." },
        { n: "03", title: "Branchez le projecteur ou le téléviseur, appuyez sur Win+P, puis choisissez Étendre." },
        { n: "04", title: "Lancez MessageFlow Media, choisissez la langue, recherchez, puis Ctrl+P pour projeter." },
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
        "Programu ya bure ya Windows kwa dawati la kuonyesha kanisani. Tafuta na uonyeshe mahubiri na Maandiko nje ya mtandao kwa Kiingereza, Kifaransa na Kiswahili — nyimbo za Kiingereza, Kifaransa na Kiswahili zimo.",
      secondaryCta: "Sakinisha",
    },
    download: {
      button: "Pakua kwa ajili ya Windows",
      heading: "Pakua kwa ajili ya Windows",
      pageTitle: "Pakua kwa ajili ya Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Inafungua toleo la v1.0.2 kwenye GitHub. Pakua MessageFlowMediaSetup.exe, isakinishe kwenye Windows 10 au 11 (biti 64), kisha bonyeza Win+P na uchague Extend kabla ya kuonyesha. Maktaba za Kiingereza, Kifaransa na Kiswahili zimo.",
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
          body: "Mahubiri 384, nyimbo 499 (Recueil de cantiques français, Tabernacle Dinanga), na Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "Mahubiri 622, nyimbo 281 (Nyimbo za Kiswahili), na Biblia ya SWHULB.",
        },
      ],
    },
    install: {
      title: "Sakinisha",
      steps: [
        { n: "01", title: "Fungua Pakua na uhifadhi MessageFlowMediaSetup.exe kutoka GitHub v1.0.2." },
        { n: "02", title: "Sakinisha kwenye Windows 10 au 11 (biti 64). Pendekeza diski D: kwa kompyuta ya kanisa." },
        { n: "03", title: "Unganisha projekta au TV, bonyeza Win+P, kisha chagua Extend." },
        { n: "04", title: "Fungua MessageFlow Media, chagua lugha, tafuta, kisha Ctrl+P ili kuonyesha." },
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
