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
    menuOpen: string;
    menuClose: string;
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
    safetyTitle: string;
    safetyLead: string;
    chromeTitle: string;
    chromeBody: string;
    windowsTitle: string;
    windowsBody: string;
    hashLabel: string;
    notMalware: string;
  };
  product: {
    title: string;
    lead: string;
    darkLabel: string;
    lightLabel: string;
  };
  features: {
    title: string;
    lead: string;
    items: { title: string; body: string; detail: string }[];
  };
  install: {
    title: string;
    lead: string;
    steps: { n: string; title: string; body: string }[];
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
      features: "Library",
      product: "Interface",
      install: "Install",
      download: "Download for Windows",
      feedback: "Support",
    },
    header: {
      languageLabel: "Language",
      menuOpen: "Open menu",
      menuClose: "Close menu",
    },
    hero: {
      eyebrow: "Version {version} · Free for churches · Windows 10 / 11",
      title: "Find the sermon, verse, or song. Put it on the screen.",
      subtitle:
        "MessageFlow Media is free Windows software for church media teams. Search Brother Branham's sermons, Brother Frank's publications, the Bible, and songbooks, then project the paragraph or verse on the second screen. It works offline, in English, French, and Kiswahili.",
      secondaryCta: "See the interface",
    },
    download: {
      button: "Download for Windows",
      heading: "Ready for Sunday morning",
      pageTitle: "Download for Windows",
      pageSubtitle: "Version {version}, released {date}. Windows 10 / 11.",
      note: "Downloads MessageFlowMediaSetup.exe ({size}). Install on Windows 10 or 11 (64-bit), then press Win+P and choose Extend before you project.",
      safetyTitle: "If Chrome or Windows blocks the file",
      safetyLead:
        "MessageFlow Media is free church software. The installer is not signed with a paid Microsoft certificate yet, so some PCs warn on a first download. That is a Windows/Chrome reputation check, not a virus.",
      chromeTitle: "Chrome says “Virus detected”",
      chromeBody:
        "Open the download menu (arrow icon). Select the file → Keep → Keep anyway. Or download again with Microsoft Edge. Then open the .exe from your Downloads folder.",
      windowsTitle: "Windows says “Windows protected your PC”",
      windowsBody:
        "Click More info, then Run anyway. Allow the app if User Account Control asks. Publisher will show as MessageFlow Media.",
      hashLabel: "SHA-256 (optional check)",
      notMalware:
        "The file is the official installer from www.messageflow.tech. It contains sermons, Bibles, and songs for offline church use.",
    },
    product: {
      title: "One operator screen, in dark or light",
      lead: "The operator works on the computer. The congregation sees only the projection window. Pick the theme that suits the room.",
      darkLabel: "Dark theme",
      lightLabel: "Light theme",
    },
    features: {
      title: "What's in the library",
      lead: "Installed once, then available without internet.",
      items: [
        {
          title: "Sermons and publications",
          body: "Search by title, sermon code, year, phrase, or paragraph number, then project a single paragraph.",
          detail:
            "English: 1,203 Brother Branham sermons and 85 Brother Frank publications. French: 384. Kiswahili: 622.",
        },
        {
          title: "The Bible",
          body: "Type a reference such as John 3:16, or a keyword. Move verse by verse with the arrow keys.",
          detail: "King James Version · Louis Segond 1910 · Biblia Takatifu (SWHULB)",
        },
        {
          title: "Songs",
          body: "Find a song by its title or a line of the lyrics, then project it verse by verse, with the chorus where it belongs.",
          detail: "357 English · 499 French · 281 Kiswahili",
        },
      ],
    },
    install: {
      title: "From download to projection",
      lead: "Four steps. No account, and no internet needed after installing.",
      steps: [
        {
          n: "01",
          title: "Download",
          body: "Download MessageFlowMediaSetup.exe ({size}) for Windows 10 or 11, 64-bit.",
        },
        {
          n: "02",
          title: "Install",
          body: "Run the installer. If Windows says it protected your PC, choose More info, then Run anyway. Pick a drive with room for the library.",
        },
        {
          n: "03",
          title: "Connect the projector",
          body: "Plug in the projector or TV, press Win+P, and choose Extend.",
        },
        {
          n: "04",
          title: "Search and project",
          body: "Open MessageFlow Media, choose your language, find what you need, and press Ctrl+P.",
        },
      ],
    },
    footer: {
      blurb:
        "Free Windows software for church media teams: sermons, the Bible, and songs, fully offline.",
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
      features: "Bibliothèque",
      product: "Interface",
      install: "Installation",
      download: "Télécharger pour Windows",
      feedback: "Assistance",
    },
    header: {
      languageLabel: "Langue",
      menuOpen: "Ouvrir le menu",
      menuClose: "Fermer le menu",
    },
    hero: {
      eyebrow: "Version {version} · Gratuit pour les églises · Windows 10 / 11",
      title: "Trouvez la prédication, le verset ou le cantique. Projetez-le.",
      subtitle:
        "MessageFlow Media est un logiciel Windows gratuit pour les équipes média des églises. Recherchez les prédications de frère Branham, les publications de frère Frank, la Bible et les recueils de cantiques, puis projetez le paragraphe ou le verset sur le second écran. Il fonctionne hors ligne, en anglais, en français et en kiswahili.",
      secondaryCta: "Voir l'interface",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Prêt pour le dimanche matin",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Télécharge MessageFlowMediaSetup.exe ({size}). Installez-le sous Windows 10 ou 11 (64 bits), puis appuyez sur Win+P et choisissez Étendre avant de projeter.",
      safetyTitle: "Si Chrome ou Windows bloque le fichier",
      safetyLead:
        "MessageFlow Media est un logiciel d'église gratuit. L'installateur n'est pas encore signé avec un certificat Microsoft payant, donc certains PC affichent un avertissement au premier téléchargement. Ce n'est pas un virus : c'est un contrôle de réputation Windows/Chrome.",
      chromeTitle: "Chrome affiche « Virus detected »",
      chromeBody:
        "Ouvrez le menu Téléchargements (flèche). Choisissez le fichier → Conserver → Conserver quand même. Ou retéléchargez avec Microsoft Edge. Puis ouvrez le .exe depuis le dossier Téléchargements.",
      windowsTitle: "Windows affiche « Windows a protégé votre PC »",
      windowsBody:
        "Cliquez sur Plus d'infos, puis Exécuter quand même. Autorisez l'app si le Contrôle de compte d'utilisateur le demande. L'éditeur s'affiche comme MessageFlow Media.",
      hashLabel: "SHA-256 (vérification facultative)",
      notMalware:
        "Ce fichier est l'installateur officiel de www.messageflow.tech. Il contient prédications, Bibles et cantiques pour un usage hors ligne à l'église.",
    },
    product: {
      title: "Un seul écran opérateur, en sombre ou en clair",
      lead: "L'opérateur travaille sur l'ordinateur. L'assemblée ne voit que la fenêtre de projection. Choisissez le thème qui convient à la salle.",
      darkLabel: "Thème sombre",
      lightLabel: "Thème clair",
    },
    features: {
      title: "Ce que contient la bibliothèque",
      lead: "Installée une fois, puis disponible sans Internet.",
      items: [
        {
          title: "Prédications et publications",
          body: "Recherchez par titre, code de prédication, année, expression ou numéro de paragraphe, puis projetez un seul paragraphe.",
          detail:
            "Anglais : 1 203 prédications de frère Branham et 85 publications de frère Frank. Français : 384. Kiswahili : 622.",
        },
        {
          title: "La Bible",
          body: "Tapez une référence comme Jean 3:16, ou un mot-clé. Avancez verset par verset avec les flèches.",
          detail: "King James Version · Louis Segond 1910 · Biblia Takatifu (SWHULB)",
        },
        {
          title: "Cantiques",
          body: "Trouvez un cantique par son titre ou une ligne des paroles, puis projetez-le couplet par couplet, avec le refrain au bon endroit.",
          detail: "357 en anglais · 499 en français · 281 en kiswahili",
        },
      ],
    },
    install: {
      title: "Du téléchargement à la projection",
      lead: "Quatre étapes. Aucun compte, et aucune connexion Internet après l'installation.",
      steps: [
        {
          n: "01",
          title: "Télécharger",
          body: "Téléchargez MessageFlowMediaSetup.exe ({size}) pour Windows 10 ou 11, 64 bits.",
        },
        {
          n: "02",
          title: "Installer",
          body: "Lancez l'installateur. Si Windows indique qu'il a protégé votre PC, choisissez Plus d'infos, puis Exécuter quand même. Choisissez un disque avec assez d'espace pour la bibliothèque.",
        },
        {
          n: "03",
          title: "Brancher le projecteur",
          body: "Branchez le projecteur ou le téléviseur, appuyez sur Win+P et choisissez Étendre.",
        },
        {
          n: "04",
          title: "Rechercher et projeter",
          body: "Ouvrez MessageFlow Media, choisissez la langue, trouvez ce qu'il vous faut et appuyez sur Ctrl+P.",
        },
      ],
    },
    footer: {
      blurb:
        "Logiciel Windows gratuit pour les équipes média des églises : prédications, Bible et cantiques, entièrement hors ligne.",
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
      features: "Maktaba",
      product: "Kiolesura",
      install: "Sakinisha",
      download: "Pakua kwa Windows",
      feedback: "Msaada",
    },
    header: {
      languageLabel: "Lugha",
      menuOpen: "Fungua menyu",
      menuClose: "Funga menyu",
    },
    hero: {
      eyebrow: "Toleo {version} · Bure kwa makanisa · Windows 10 / 11",
      title: "Pata hubiri, mstari au wimbo. Uonyeshe kwenye skrini.",
      subtitle:
        "MessageFlow Media ni programu ya Windows ya bure kwa timu za media za kanisa. Tafuta mahubiri ya Ndugu Branham, machapisho ya Ndugu Frank, Biblia na vitabu vya nyimbo, kisha onyesha aya au mstari kwenye skrini ya pili. Inafanya kazi bila intaneti, kwa Kiingereza, Kifaransa na Kiswahili.",
      secondaryCta: "Tazama kiolesura",
    },
    download: {
      button: "Pakua kwa Windows",
      heading: "Tayari kwa ibada ya Jumapili",
      pageTitle: "Pakua kwa Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Inapakua MessageFlowMediaSetup.exe ({size}). Isakinishe kwenye Windows 10 au 11 (biti 64), kisha bonyeza Win+P na uchague Extend kabla ya kuonyesha.",
      safetyTitle: "Ikiwa Chrome au Windows inazuia faili",
      safetyLead:
        "MessageFlow Media ni programu ya kanisa bila malipo. Setup bado haijatiwa saini kwa cheti cha Microsoft kinacholipiwa, kwa hiyo kompyuta nyingine huonyesha onyo wakati wa kupakua mara ya kwanza. Si virusi: ni ukaguzi wa sifa wa Windows/Chrome.",
      chromeTitle: "Chrome inasema “Virus detected”",
      chromeBody:
        "Fungua menyu ya Vipakuliwa (mshale). Chagua faili → Keep → Keep anyway. Au pakua tena kwa Microsoft Edge. Kisha fungua .exe kutoka kwenye folda ya Downloads.",
      windowsTitle: "Windows inasema “Windows protected your PC”",
      windowsBody:
        "Bonyeza More info, kisha Run anyway. Ruhusu programu ikiwa User Account Control inauliza. Mchapishaji ataonekana kama MessageFlow Media.",
      hashLabel: "SHA-256 (hiari)",
      notMalware:
        "Faili hii ni setup rasmi kutoka www.messageflow.tech. Ina mahubiri, Biblia, na nyimbo kwa matumizi ya kanisa nje ya mtandao.",
    },
    product: {
      title: "Skrini moja ya operator, meusi au meupe",
      lead: "Operator anafanya kazi kwenye kompyuta. Waumini wanaona dirisha la kuonyesha pekee. Chagua mandhari inayofaa ukumbi.",
      darkLabel: "Mandhari meusi",
      lightLabel: "Mandhari meupe",
    },
    features: {
      title: "Kilichomo kwenye maktaba",
      lead: "Sakinisha mara moja, kisha tumia bila intaneti.",
      items: [
        {
          title: "Mahubiri na machapisho",
          body: "Tafuta kwa kichwa, msimbo wa hubiri, mwaka, maneno au namba ya aya, kisha onyesha aya moja.",
          detail:
            "Kiingereza: mahubiri 1,203 ya Ndugu Branham na machapisho 85 ya Ndugu Frank. Kifaransa: 384. Kiswahili: 622.",
        },
        {
          title: "Biblia",
          body: "Andika rejea kama Yohana 3:16, au neno kuu. Songa mstari kwa mstari kwa vitufe vya mishale.",
          detail: "King James Version · Louis Segond 1910 · Biblia Takatifu (SWHULB)",
        },
        {
          title: "Nyimbo",
          body: "Tafuta wimbo kwa kichwa chake au mstari wa maneno yake, kisha uonyeshe ubeti kwa ubeti, pamoja na kiitikio mahali pake.",
          detail: "Kiingereza 357 · Kifaransa 499 · Kiswahili 281",
        },
      ],
    },
    install: {
      title: "Kutoka kupakua hadi kuonyesha",
      lead: "Hatua nne. Hakuna akaunti, na hakuna intaneti baada ya kusakinisha.",
      steps: [
        {
          n: "01",
          title: "Pakua",
          body: "Pakua MessageFlowMediaSetup.exe ({size}) kwa Windows 10 au 11, biti 64.",
        },
        {
          n: "02",
          title: "Sakinisha",
          body: "Endesha setup. Ikiwa Windows inasema imelinda kompyuta yako, chagua More info, kisha Run anyway. Chagua diski yenye nafasi ya kutosha kwa maktaba.",
        },
        {
          n: "03",
          title: "Unganisha projekta",
          body: "Chomeka projekta au TV, bonyeza Win+P, kisha chagua Extend.",
        },
        {
          n: "04",
          title: "Tafuta na uonyeshe",
          body: "Fungua MessageFlow Media, chagua lugha, tafuta unachohitaji, kisha bonyeza Ctrl+P.",
        },
      ],
    },
    footer: {
      blurb:
        "Programu ya Windows ya bure kwa timu za media za kanisa: mahubiri, Biblia na nyimbo, bila intaneti.",
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

/**
 * Installer size in the reader's units (French writes megabytes as "Mo"), with a
 * non-breaking space so "598" and "MB" never wrap onto separate lines.
 */
export function localizedSize(locale: Locale, size: string) {
  const units = locale === "fr" ? size.replace(/\bMB\b/, "Mo") : size;
  return units.replace(/ /g, " ");
}
