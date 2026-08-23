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
    library: string;
    product: string;
    install: string;
    support: string;
    download: string;
  };
  header: {
    languageLabel: string;
    menu: string;
    close: string;
  };
  hero: {
    badge: string;
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
    lead: string;
    videoTitle: string;
    screenshotsTitle: string;
    englishUi: string;
    frenchUi: string;
    swahiliUi: string;
  };
  features: {
    title: string;
    lead: string;
    items: { title: string; body: string }[];
  };
  library: {
    title: string;
    lead: string;
    items: {
      title: string;
      sermons: string;
      sermonsLabel: string;
      songs: string;
      bible: string;
    }[];
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
    successDetail: string;
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
      library: "Library",
      product: "Interface",
      install: "Install",
      support: "Support",
      download: "Download for Windows",
    },
    header: {
      languageLabel: "Language",
      menu: "Open menu",
      close: "Close menu",
    },
    hero: {
      badge: "v{version} Live · Built for Windows 10 / 11",
      title: "The modern standard for church media projection",
      subtitle:
        "Lightning-fast offline Windows software to search and project sermons, Bibles, and multilingual songbooks in English, French, and Kiswahili — no internet required.",
      secondaryCta: "Watch the demo",
    },
    download: {
      button: "Download for Windows",
      heading: "Ready for Sunday morning",
      pageTitle: "Download for Windows",
      pageSubtitle: "Version {version}, released {date}. Windows 10 / 11.",
      note: "Downloads MessageFlowMediaSetup.exe (~563 MB). Install on Windows 10 or 11 (64-bit), then press Win+P and choose Extend before you project.",
    },
    product: {
      title: "The operator desk, in every language",
      lead: "A quiet dark interface for live service — English, French, and Kiswahili, without leaving the desk.",
      videoTitle: "Product demo",
      screenshotsTitle: "Interface gallery",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Built for the live service, not the cloud",
      lead: "Everything the operator needs stays on the PC in the booth — private, fast, and free of accounts.",
      items: [
        {
          title: "100% offline and secure",
          body: "No internet during service. Your library lives on this computer. No cloud sync. No ads. No accounts.",
        },
        {
          title: "Multilingual mastery",
          body: "English, French, and Kiswahili across sermons, Bibles, and structured songbooks — native, not bolted on.",
        },
        {
          title: "Instant operator workflow",
          body: "Ctrl+F to search, Ctrl+P to project, arrow keys to move. Dual-screen projection without fighting the OS.",
        },
      ],
    },
    library: {
      title: "What's in the box",
      lead: "One installer. Three languages. The full operator library, ready offline.",
      items: [
        {
          title: "English",
          sermons: "1,208",
          sermonsLabel: "sermons",
          songs: "English songbook",
          bible: "KJV Bible",
        },
        {
          title: "Français",
          sermons: "384",
          sermonsLabel: "prédications",
          songs: "499 Dinanga hymns",
          bible: "Louis Segond",
        },
        {
          title: "Kiswahili",
          sermons: "622",
          sermonsLabel: "mahubiri",
          songs: "281 official hymns",
          bible: "SWHULB Bible",
        },
      ],
    },
    install: {
      title: "From download to projection",
      lead: "Four steps. No account. No network required after install.",
      steps: [
        {
          n: "01",
          title: "Download",
          body: "Save MessageFlowMediaSetup.exe (~563 MB) for Windows 10 or 11 (64-bit).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "~563 MB · 64-bit",
            action: "Download",
            badge: "Setup",
          },
        },
        {
          n: "02",
          title: "Install",
          body: "Run the wizard. Choose a disk with room for the offline library.",
          labels: {
            primary: "MessageFlow Media",
            secondary: "Setup wizard",
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
        "Free Windows software for the church operator desk — sermons, Bibles, and hymns, fully offline.",
      product: "Product",
      release: "Release",
      copyright: "© 2026 MessageFlow Media. All rights reserved.",
    },
    feedback: {
      title: "Feedback & support",
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
      success: "Your message was successfully sent.",
      successDetail:
        "Thank you for your feedback. We will get back to you if a reply is needed.",
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
      library: "Bibliothèque",
      product: "Interface",
      install: "Installation",
      support: "Assistance",
      download: "Télécharger pour Windows",
    },
    header: {
      languageLabel: "Langue",
      menu: "Ouvrir le menu",
      close: "Fermer le menu",
    },
    hero: {
      badge: "v{version} disponible · Windows 10 / 11",
      title: "Le standard moderne de la projection média à l'église",
      subtitle:
        "Logiciel Windows hors ligne, rapide, pour rechercher et projeter prédications, Bibles et recueils de cantiques en anglais, français et kiswahili — sans connexion Internet.",
      secondaryCta: "Voir la démo",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Prêt pour le dimanche matin",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Télécharge MessageFlowMediaSetup.exe (~563 Mo). Installez-le sous Windows 10 ou 11 (64 bits), puis appuyez sur Win+P et choisissez Étendre.",
    },
    product: {
      title: "Le pupitre, dans chaque langue",
      lead: "Une interface sombre et calme pour le direct — anglais, français et kiswahili, sans quitter le pupitre.",
      videoTitle: "Démonstration",
      screenshotsTitle: "Galerie d'interface",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Conçu pour le direct, pas pour le cloud",
      lead: "Tout ce dont l'opérateur a besoin reste sur le PC de la régie — privé, rapide, sans compte.",
      items: [
        {
          title: "100 % hors ligne et sûr",
          body: "Pas d'internet pendant le culte. La bibliothèque reste sur cet ordinateur. Pas de cloud, pas de publicité, pas de compte.",
        },
        {
          title: "Maîtrise multilingue",
          body: "Anglais, français et kiswahili pour les prédications, les Bibles et les recueils structurés — natif, pas ajouté après coup.",
        },
        {
          title: "Flux opérateur instantané",
          body: "Ctrl+F pour chercher, Ctrl+P pour projeter, flèches pour avancer. Projection double écran sans lutter avec Windows.",
        },
      ],
    },
    library: {
      title: "Ce qui est inclus",
      lead: "Un installateur. Trois langues. Toute la bibliothèque opérateur, hors ligne.",
      items: [
        {
          title: "English",
          sermons: "1 208",
          sermonsLabel: "prédications",
          songs: "Recueil anglais",
          bible: "Bible KJV",
        },
        {
          title: "Français",
          sermons: "384",
          sermonsLabel: "prédications",
          songs: "499 cantiques Dinanga",
          bible: "Louis Segond",
        },
        {
          title: "Kiswahili",
          sermons: "622",
          sermonsLabel: "prédications",
          songs: "281 cantiques officiels",
          bible: "Bible SWHULB",
        },
      ],
    },
    install: {
      title: "Du téléchargement à la projection",
      lead: "Quatre étapes. Aucun compte. Aucun réseau après l'installation.",
      steps: [
        {
          n: "01",
          title: "Télécharger",
          body: "Enregistrez MessageFlowMediaSetup.exe (~563 Mo) pour Windows 10 ou 11 (64 bits).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "~563 Mo · 64 bits",
            action: "Télécharger",
            badge: "Fichier",
          },
        },
        {
          n: "02",
          title: "Installer",
          body: "Lancez l'assistant. Choisissez un disque avec assez d'espace pour la bibliothèque hors ligne.",
          labels: {
            primary: "MessageFlow Media",
            secondary: "Assistant",
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
        "Logiciel Windows gratuit pour le pupitre de projection — prédications, Bibles et cantiques, entièrement hors ligne.",
      product: "Produit",
      release: "Version",
      copyright: "© 2026 MessageFlow Media. Tous droits réservés.",
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
      success: "Votre message a été envoyé avec succès.",
      successDetail:
        "Merci pour votre avis. Nous vous répondrons si une réponse est nécessaire.",
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
      library: "Maktaba",
      product: "Kiolesura",
      install: "Sakinisha",
      support: "Msaada",
      download: "Pakua kwa Windows",
    },
    header: {
      languageLabel: "Lugha",
      menu: "Fungua menyu",
      close: "Funga menyu",
    },
    hero: {
      badge: "v{version} Iko hai · Windows 10 / 11",
      title: "Kiwango cha kisasa cha kuonyesha media kanisani",
      subtitle:
        "Programu ya Windows nje ya mtandao, yenye kasi, kutafuta na kuonyesha mahubiri, Biblia, na vitabu vya nyimbo kwa Kiingereza, Kifaransa na Kiswahili — bila intaneti.",
      secondaryCta: "Tazama onyesho",
    },
    download: {
      button: "Pakua kwa Windows",
      heading: "Tayari kwa ibada ya Jumapili",
      pageTitle: "Pakua kwa Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Inapakua MessageFlowMediaSetup.exe (~563 MB). Isakinishe kwenye Windows 10 au 11 (biti 64), kisha bonyeza Win+P na uchague Extend.",
    },
    product: {
      title: "Dawati la operator, katika kila lugha",
      lead: "Kiolesura cheusi, tulivu, kwa ibada moja kwa moja — Kiingereza, Kifaransa na Kiswahili, bila kuondoka dawati.",
      videoTitle: "Onyesho la bidhaa",
      screenshotsTitle: "Matunzio ya kiolesura",
      englishUi: "English",
      frenchUi: "Français",
      swahiliUi: "Kiswahili",
    },
    features: {
      title: "Imetengenezwa kwa ibada, si wingu",
      lead: "Kila kitu operator anahitaji kiko kwenye kompyuta ya dawati — faragha, kasi, bila akaunti.",
      items: [
        {
          title: "100% nje ya mtandao na salama",
          body: "Hakuna intaneti wakati wa ibada. Maktaba yako iko kwenye kompyuta hii. Hakuna wingu. Hakuna matangazo. Hakuna akaunti.",
        },
        {
          title: "Ustadi wa lugha nyingi",
          body: "Kiingereza, Kifaransa na Kiswahili katika mahubiri, Biblia, na vitabu vya nyimbo vilivyopangwa — asili, si nyongeza.",
        },
        {
          title: "Kazi ya operator papo hapo",
          body: "Ctrl+F kutafuta, Ctrl+P kuonyesha, mishale kusogeza. Kuonyesha skrini mbili bila kupigana na Windows.",
        },
      ],
    },
    library: {
      title: "Kilichomo ndani",
      lead: "Setup moja. Lugha tatu. Maktaba kamili ya operator, nje ya mtandao.",
      items: [
        {
          title: "English",
          sermons: "1,208",
          sermonsLabel: "mahubiri",
          songs: "Kitabu cha nyimbo za Kiingereza",
          bible: "Biblia KJV",
        },
        {
          title: "Français",
          sermons: "384",
          sermonsLabel: "mahubiri",
          songs: "Nyimbo 499 za Dinanga",
          bible: "Louis Segond",
        },
        {
          title: "Kiswahili",
          sermons: "622",
          sermonsLabel: "mahubiri",
          songs: "Nyimbo 281 rasmi",
          bible: "Biblia SWHULB",
        },
      ],
    },
    install: {
      title: "Kutoka kupakua hadi kuonyesha",
      lead: "Hatua nne. Hakuna akaunti. Hakuna mtandao baada ya kusakinisha.",
      steps: [
        {
          n: "01",
          title: "Pakua",
          body: "Hifadhi MessageFlowMediaSetup.exe (~563 MB) kwa Windows 10 au 11 (biti 64).",
          labels: {
            primary: "MessageFlowMediaSetup.exe",
            secondary: "~563 MB · biti 64",
            action: "Pakua",
            badge: "Setup",
          },
        },
        {
          n: "02",
          title: "Sakinisha",
          body: "Fungua setup. Chagua diski yenye nafasi ya kutosha kwa maktaba nje ya mtandao.",
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
        "Programu ya bure ya Windows kwa dawati la kuonyesha kanisani — mahubiri, Biblia, na nyimbo, nje ya mtandao.",
      product: "Bidhaa",
      release: "Toleo",
      copyright: "© 2026 MessageFlow Media. Haki zote zimehifadhiwa.",
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
      success: "Ujumbe wako umetumwa kikamilifu.",
      successDetail:
        "Asante kwa maoni yako. Tutawasiliana nawe ikiwa jibu litahitajika.",
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
