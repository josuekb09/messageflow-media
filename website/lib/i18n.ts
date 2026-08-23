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
    feedback: string;
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
    lead: string;
    steps: {
      n: string;
      title: string;
      body: string;
      labels: {
        primary: string;
        secondary: string;
        action: string;
        badge: string;
      };
    }[];
  };
  footer: {
    blurb: string;
    product: string;
    release: string;
    copyright: string;
  };
  feedback: {
    title: string;
    pageSubtitle: string;
    lead: string;
    calloutTitle: string;
    calloutBody: string;
    calloutCta: string;
    nameLabel: string;
    nameOptional: string;
    namePlaceholder: string;
    emailLabel: string;
    emailPlaceholder: string;
    categoryLabel: string;
    categories: {
      comment: string;
      feature: string;
      bug: string;
    };
    messageLabel: string;
    messagePlaceholder: string;
    submit: string;
    submitting: string;
    again: string;
    success: string;
    error: string;
    errorConfig: string;
    errorValidation: string;
    emailInvalid: string;
    messageRequired: string;
  };
};

export const dictionaries: Record<Locale, Dictionary> = {
  en: {
    nav: {
      features: "Features",
      product: "Product",
      install: "Install",
      download: "Download",
      feedback: "Feedback",
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
      note: "Downloads MessageFlowMediaSetup.exe. Run it on Windows 10 or 11 (64-bit), then press Win+P and choose Extend before you project. English, French, and Swahili libraries are included.",
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
          body: "384 sermons, 499 hymns, and Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 sermons, 281 hymns, and the SWHULB Bible.",
        },
      ],
    },
    install: {
      title: "Install",
      lead: "Four steps from download to live projection.",
      steps: [
        {
          n: "01",
          title: "Download",
          body: "Save MessageFlowMediaSetup.exe for Windows 10 or 11 (64-bit).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "Windows 10 / 11",
            action: "Download",
            badge: "Downloads",
          },
        },
        {
          n: "02",
          title: "Install",
          body: "Run the setup file. Choose a disk with enough space for the offline library.",
          labels: {
            primary: "MessageFlow Media",
            secondary: "Setup",
            action: "Install",
            badge: "Next",
          },
        },
        {
          n: "03",
          title: "Extend the display",
          body: "Connect the projector or TV, press Win+P, and choose Extend.",
          labels: {
            primary: "Laptop",
            secondary: "Projector",
            action: "Extend",
            badge: "Win+P",
          },
        },
        {
          n: "04",
          title: "Search and project",
          body: "Open MessageFlow Media, pick a language, then press Ctrl+P.",
          labels: {
            primary: "Search",
            secondary: "Screen",
            action: "Project",
            badge: "Ctrl+P",
          },
        },
      ],
    },
    footer: {
      blurb:
        "Free Windows desktop software for church projection — sermons, Bibles, and hymns, fully offline.",
      product: "Product",
      release: "Release",
      copyright: "MessageFlow 2026, All rights reserved",
    },
    feedback: {
      title: "Feedback & Support",
      pageSubtitle:
        "Send a comment, feature request, or bug report. We read every message.",
      lead: "The Windows app stays offline. Use this form to reach the MessageFlow team.",
      calloutTitle: "Questions or ideas?",
      calloutBody:
        "Send feedback from this site — comments, feature requests, or bug reports. The Windows app itself stays fully offline.",
      calloutCta: "Open the form",
      nameLabel: "Name",
      nameOptional: "(optional)",
      namePlaceholder: "Your name",
      emailLabel: "Email",
      emailPlaceholder: "you@example.com",
      categoryLabel: "Category",
      categories: {
        comment: "Comment",
        feature: "Feature request",
        bug: "Bug report",
      },
      messageLabel: "Message",
      messagePlaceholder: "How can we help?",
      submit: "Send message",
      submitting: "Sending…",
      again: "Send another message",
      success: "Thank you. Your message was sent.",
      error: "Something went wrong. Please try again in a moment.",
      errorConfig: "Feedback is not configured yet. Please try again later.",
      errorValidation: "Please check the highlighted fields.",
      emailInvalid: "Enter a valid email address.",
      messageRequired: "Enter a message.",
    },
  },
  fr: {
    nav: {
      features: "Fonctionnalités",
      product: "Produit",
      install: "Installation",
      download: "Télécharger",
      feedback: "Avis",
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
      note: "Télécharge MessageFlowMediaSetup.exe. Installez-le sous Windows 10 ou 11 (64 bits), puis appuyez sur Win+P et choisissez Étendre avant de projeter. Les bibliothèques anglaise, française et swahili sont incluses.",
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
          body: "384 prédications, 499 cantiques et Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "622 prédications, 281 cantiques et la Bible SWHULB.",
        },
      ],
    },
    install: {
      title: "Installation",
      lead: "Quatre étapes, du téléchargement à la projection.",
      steps: [
        {
          n: "01",
          title: "Télécharger",
          body: "Enregistrez MessageFlowMediaSetup.exe pour Windows 10 ou 11 (64 bits).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "Windows 10 / 11",
            action: "Télécharger",
            badge: "Fichier",
          },
        },
        {
          n: "02",
          title: "Installer",
          body: "Exécutez le fichier. Choisissez un disque avec assez d'espace pour la bibliothèque hors ligne.",
          labels: {
            primary: "MessageFlow Media",
            secondary: "Installation",
            action: "Installer",
            badge: "Suivant",
          },
        },
        {
          n: "03",
          title: "Étendre l'écran",
          body: "Branchez le projecteur ou le téléviseur, appuyez sur Win+P, puis choisissez Étendre.",
          labels: {
            primary: "PC",
            secondary: "Projecteur",
            action: "Étendre",
            badge: "Win+P",
          },
        },
        {
          n: "04",
          title: "Rechercher et projeter",
          body: "Ouvrez MessageFlow Media, choisissez la langue, puis appuyez sur Ctrl+P.",
          labels: {
            primary: "Rechercher",
            secondary: "Écran",
            action: "Projeter",
            badge: "Ctrl+P",
          },
        },
      ],
    },
    footer: {
      blurb:
        "Logiciel Windows gratuit pour la projection à l'église — prédications, Bibles et cantiques, entièrement hors ligne.",
      product: "Produit",
      release: "Version",
      copyright: "MessageFlow 2026, All rights reserved",
    },
    feedback: {
      title: "Avis et assistance",
      pageSubtitle:
        "Envoyez un commentaire, une demande de fonctionnalité ou un signalement de bug. Nous lisons chaque message.",
      lead: "L'application Windows reste hors ligne. Utilisez ce formulaire pour contacter l'équipe MessageFlow.",
      calloutTitle: "Une question ou une idée ?",
      calloutBody:
        "Envoyez un avis depuis ce site — commentaires, demandes de fonctionnalités ou signalements de bugs. L'application Windows reste entièrement hors ligne.",
      calloutCta: "Ouvrir le formulaire",
      nameLabel: "Nom",
      nameOptional: "(facultatif)",
      namePlaceholder: "Votre nom",
      emailLabel: "E-mail",
      emailPlaceholder: "vous@exemple.com",
      categoryLabel: "Catégorie",
      categories: {
        comment: "Commentaire",
        feature: "Demande de fonctionnalité",
        bug: "Signalement de bug",
      },
      messageLabel: "Message",
      messagePlaceholder: "Comment pouvons-nous vous aider ?",
      submit: "Envoyer",
      submitting: "Envoi…",
      again: "Envoyer un autre message",
      success: "Merci. Votre message a été envoyé.",
      error: "Une erreur s'est produite. Veuillez réessayer dans un instant.",
      errorConfig: "L'envoi d'avis n'est pas encore configuré. Veuillez réessayer plus tard.",
      errorValidation: "Veuillez vérifier les champs indiqués.",
      emailInvalid: "Saisissez une adresse e-mail valide.",
      messageRequired: "Saisissez un message.",
    },
  },
  sw: {
    nav: {
      features: "Vipengele",
      product: "Bidhaa",
      install: "Sakinisha",
      download: "Pakua",
      feedback: "Maoni",
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
      note: "Inapakua MessageFlowMediaSetup.exe. Isakinishe kwenye Windows 10 au 11 (biti 64), kisha bonyeza Win+P na uchague Extend kabla ya kuonyesha. Maktaba za Kiingereza, Kifaransa na Kiswahili zimo.",
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
          body: "Mahubiri 384, nyimbo 499, na Louis Segond.",
        },
        {
          title: "Kiswahili",
          body: "Mahubiri 622, nyimbo 281, na Biblia ya SWHULB.",
        },
      ],
    },
    install: {
      title: "Sakinisha",
      lead: "Hatua nne kutoka kupakua hadi kuonyesha.",
      steps: [
        {
          n: "01",
          title: "Pakua",
          body: "Hifadhi MessageFlowMediaSetup.exe kwa Windows 10 au 11 (biti 64).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "Windows 10 / 11",
            action: "Pakua",
            badge: "Faili",
          },
        },
        {
          n: "02",
          title: "Sakinisha",
          body: "Fungua faili ya setup. Chagua diski yenye nafasi ya kutosha kwa maktaba nje ya mtandao.",
          labels: {
            primary: "MessageFlow Media",
            secondary: "Usakinishaji",
            action: "Sakinisha",
            badge: "Ifuatayo",
          },
        },
        {
          n: "03",
          title: "Panua skrini",
          body: "Unganisha projekta au TV, bonyeza Win+P, kisha chagua Extend.",
          labels: {
            primary: "Kompyuta",
            secondary: "Projekta",
            action: "Panua",
            badge: "Win+P",
          },
        },
        {
          n: "04",
          title: "Tafuta na uonyeshe",
          body: "Fungua MessageFlow Media, chagua lugha, kisha bonyeza Ctrl+P.",
          labels: {
            primary: "Tafuta",
            secondary: "Skrini",
            action: "Onyesha",
            badge: "Ctrl+P",
          },
        },
      ],
    },
    footer: {
      blurb:
        "Programu ya bure ya Windows kwa kuonyesha kanisani — mahubiri, Biblia, na nyimbo, nje ya mtandao.",
      product: "Bidhaa",
      release: "Toleo",
      copyright: "MessageFlow 2026, All rights reserved",
    },
    feedback: {
      title: "Maoni na msaada",
      pageSubtitle:
        "Tuma maoni, ombi la kipengele, au ripoti ya hitilafu. Tunasoma kila ujumbe.",
      lead: "Programu ya Windows inabaki nje ya mtandao. Tumia fomu hii kuwasiliana na timu ya MessageFlow.",
      calloutTitle: "Una swali au wazo?",
      calloutBody:
        "Tuma maoni kutoka kwenye tovuti hii — maoni, maombi ya vipengele, au ripoti za hitilafu. Programu ya Windows inabaki nje ya mtandao kabisa.",
      calloutCta: "Fungua fomu",
      nameLabel: "Jina",
      nameOptional: "(si lazima)",
      namePlaceholder: "Jina lako",
      emailLabel: "Barua pepe",
      emailPlaceholder: "wewe@mfano.com",
      categoryLabel: "Aina",
      categories: {
        comment: "Maoni",
        feature: "Ombi la kipengele",
        bug: "Ripoti ya hitilafu",
      },
      messageLabel: "Ujumbe",
      messagePlaceholder: "Tunawezaje kusaidia?",
      submit: "Tuma ujumbe",
      submitting: "Inatuma…",
      again: "Tuma ujumbe mwingine",
      success: "Asante. Ujumbe wako umetumwa.",
      error: "Kumetokea hitilafu. Tafadhali jaribu tena baadaye kidogo.",
      errorConfig: "Maoni bado hayajawekwa. Tafadhali jaribu tena baadaye.",
      errorValidation: "Tafadhali kagua sehemu zilizoonyeshwa.",
      emailInvalid: "Weka anwani sahihi ya barua pepe.",
      messageRequired: "Weka ujumbe.",
    },
  },
};

export function interpolate(template: string, values: Record<string, string>) {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => values[key] ?? "");
}
