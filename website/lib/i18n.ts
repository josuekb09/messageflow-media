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
    windowsBody: string;
    browserBody: string;
  };
  product: {
    title: string;
    lead: string;
    darkLabel: string;
    lightLabel: string;
  };
  whatsNew: {
    title: string;
    items: { title: string; body: string }[];
  };
  features: {
    title: string;
    lead: string;
    items: { title: string; body: string }[];
    table: {
      caption: string;
      columns: { en: string; fr: string; sw: string };
      branham: string;
      frank: string;
      englishOnly: string;
      bible: string;
      songs: string;
      notAvailable: string;
    };
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
      product: "The app",
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
        "MessageFlow Media is free Windows software for church media teams. Search Brother Branham's sermons, Brother Frank's publications (in English), the Bible, and songbooks, then project the paragraph or verse on the second screen. It works offline, in English, French, and Kiswahili.",
      secondaryCta: "See the app",
    },
    download: {
      button: "Download for Windows",
      heading: "Ready for Sunday morning",
      pageTitle: "Download for Windows",
      pageSubtitle: "Version {version}, released {date}. Windows 10 / 11.",
      note: "About {size}, for Windows 10 and 11. Free, with no account needed.",
      safetyTitle: "A quick tip",
      windowsBody: "If Windows shows a blue warning, click “More info”, then “Run anyway”.",
      browserBody: "If your browser asks whether to keep the file, choose “Keep”.",
    },
    product: {
      title: "One operator screen, in dark or light",
      lead: "The operator works on the computer. The congregation sees only the projection window. Pick the theme that suits the room.",
      darkLabel: "Dark theme",
      lightLabel: "Light theme",
    },
    whatsNew: {
      title: "What's new in {version}",
      items: [
        {
          title: "Opens instantly",
          body: "MessageFlow is ready to use a moment after you open it.",
        },
        {
          title: "Projected text always fits the screen",
          body: "Every verse, song, and paragraph fits neatly on the projector. No word is ever cut off.",
        },
        {
          title: "Correct line breaks in Swahili songs",
          body: "Each line of a Swahili song now appears on its own line, just as it is sung.",
        },
        {
          title: "New clean light theme",
          body: "A fresh white-and-blue look for bright rooms. The dark look is still there too.",
        },
      ],
    },
    features: {
      title: "What's in the library",
      lead: "Installed once, then available without internet.",
      items: [
        {
          title: "Sermons and publications",
          body: "Search by title, sermon code, year, phrase, or paragraph number, then project a single paragraph.",
        },
        {
          title: "The Bible",
          body: "Type a reference such as John 3:16, or a keyword. Move verse by verse with the arrow keys.",
        },
        {
          title: "Songs",
          body: "Find a song by its title or a line of the lyrics, then project it verse by verse, with the chorus where it belongs.",
        },
      ],
      table: {
        caption: "Content by language",
        columns: { en: "English", fr: "French", sw: "Kiswahili" },
        branham: "Brother Branham sermons",
        frank: "Brother Frank publications",
        englishOnly: "English only",
        bible: "Bible translation",
        songs: "Songs",
        notAvailable: "Not available",
      },
    },
    install: {
      title: "From download to projection",
      lead: "Four steps. No account, and no internet needed after installing.",
      steps: [
        {
          n: "01",
          title: "Download",
          body: "Click Download for Windows. The file is about {size} and works on Windows 10 and 11.",
        },
        {
          n: "02",
          title: "Install",
          body: "Open the downloaded file and follow the steps. If Windows shows a blue warning, click “More info”, then “Run anyway”. Needs about {space} of free space.",
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
        "Share a comment, an idea, or a problem. We read every message.",
      lead: "The Windows app stays offline. Use this form to reach the MessageFlow team.",
      calloutTitle: "Questions or ideas?",
      calloutBody:
        "Send us a comment, an idea, or tell us about a problem. We are happy to help.",
      calloutCta: "Open the form",
      nameLabel: "Name",
      nameOptional: "(optional)",
      namePlaceholder: "Your name",
      emailLabel: "Email",
      emailPlaceholder: "you@example.com",
      categoryLabel: "Category",
      categories: {
        comment: "Comment",
        feature: "Idea",
        bug: "Problem",
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
      errorConfig: "Messages can’t be sent right now. Please try again later.",
      errorValidation: "Please check the highlighted fields.",
      emailInvalid: "Enter a valid email address.",
      messageRequired: "Enter a message.",
    },
  },
  fr: {
    nav: {
      features: "Bibliothèque",
      product: "L'application",
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
        "MessageFlow Media est un logiciel Windows gratuit pour les équipes média des églises. Recherchez les prédications de frère Branham, les publications de frère Frank (en anglais), la Bible et les recueils de cantiques, puis projetez le paragraphe ou le verset sur le second écran. Il fonctionne hors ligne, en anglais, en français et en kiswahili.",
      secondaryCta: "Voir l'application",
    },
    download: {
      button: "Télécharger pour Windows",
      heading: "Prêt pour le dimanche matin",
      pageTitle: "Télécharger pour Windows",
      pageSubtitle: "Version {version}, publiée en {date}. Windows 10 / 11.",
      note: "Environ {size}, pour Windows 10 et 11. Gratuit, sans compte.",
      safetyTitle: "Petit conseil",
      windowsBody: "Si Windows affiche un avertissement bleu, cliquez sur « Informations complémentaires », puis « Exécuter quand même ».",
      browserBody: "Si votre navigateur demande s'il faut conserver le fichier, choisissez « Conserver ».",
    },
    product: {
      title: "Un seul écran opérateur, en sombre ou en clair",
      lead: "L'opérateur travaille sur l'ordinateur. L'assemblée ne voit que la fenêtre de projection. Choisissez le thème qui convient à la salle.",
      darkLabel: "Thème sombre",
      lightLabel: "Thème clair",
    },
    whatsNew: {
      title: "Nouveautés de la version {version}",
      items: [
        {
          title: "S'ouvre instantanément",
          body: "MessageFlow est prêt à l'emploi un instant après l'ouverture.",
        },
        {
          title: "Le texte projeté tient toujours à l'écran",
          body: "Chaque verset, cantique et paragraphe s'affiche proprement au projecteur. Aucun mot n'est coupé.",
        },
        {
          title: "Des retours à la ligne corrects dans les cantiques en kiswahili",
          body: "Chaque ligne d'un cantique en kiswahili s'affiche désormais sur sa propre ligne, comme on la chante.",
        },
        {
          title: "Nouveau thème clair et épuré",
          body: "Un nouveau look blanc et bleu pour les salles lumineuses. Le thème sombre est toujours là.",
        },
      ],
    },
    features: {
      title: "Ce que contient la bibliothèque",
      lead: "Installée une fois, puis disponible sans Internet.",
      items: [
        {
          title: "Prédications et publications",
          body: "Recherchez par titre, code de prédication, année, expression ou numéro de paragraphe, puis projetez un seul paragraphe.",
        },
        {
          title: "La Bible",
          body: "Tapez une référence comme Jean 3:16, ou un mot-clé. Avancez verset par verset avec les flèches.",
        },
        {
          title: "Cantiques",
          body: "Trouvez un cantique par son titre ou une ligne des paroles, puis projetez-le couplet par couplet, avec le refrain au bon endroit.",
        },
      ],
      table: {
        caption: "Contenu par langue",
        columns: { en: "Anglais", fr: "Français", sw: "Kiswahili" },
        branham: "Prédications de frère Branham",
        frank: "Publications de frère Frank",
        englishOnly: "en anglais uniquement",
        bible: "Traduction de la Bible",
        songs: "Cantiques",
        notAvailable: "Non disponible",
      },
    },
    install: {
      title: "Du téléchargement à la projection",
      lead: "Quatre étapes. Aucun compte, et aucune connexion Internet après l'installation.",
      steps: [
        {
          n: "01",
          title: "Télécharger",
          body: "Cliquez sur Télécharger pour Windows. Le fichier fait environ {size} et fonctionne sous Windows 10 et 11.",
        },
        {
          n: "02",
          title: "Installer",
          body: "Ouvrez le fichier téléchargé et suivez les étapes. Si Windows affiche un avertissement bleu, cliquez sur « Informations complémentaires », puis « Exécuter quand même ». Prévoyez environ {space} d'espace libre.",
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
        "Partagez un commentaire, une idée ou un problème. Nous lisons chaque message.",
      lead: "L'application Windows reste hors ligne. Utilisez ce formulaire pour contacter l'équipe MessageFlow.",
      calloutTitle: "Une question ou une idée ?",
      calloutBody:
        "Envoyez-nous un commentaire, une idée, ou signalez-nous un problème. Nous sommes heureux de vous aider.",
      calloutCta: "Ouvrir le formulaire",
      nameLabel: "Nom",
      nameOptional: "(facultatif)",
      namePlaceholder: "Votre nom",
      emailLabel: "E-mail",
      emailPlaceholder: "vous@exemple.com",
      categoryLabel: "Catégorie",
      categories: {
        comment: "Commentaire",
        feature: "Idée",
        bug: "Problème",
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
      errorConfig: "Les messages ne peuvent pas être envoyés pour le moment. Veuillez réessayer plus tard.",
      errorValidation: "Veuillez vérifier les champs indiqués.",
      emailInvalid: "Saisissez une adresse e-mail valide.",
      messageRequired: "Saisissez un message.",
    },
  },
  sw: {
    nav: {
      features: "Maktaba",
      product: "Programu",
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
        "MessageFlow Media ni programu ya Windows ya bure kwa timu za media za kanisa. Tafuta mahubiri ya Ndugu Branham, machapisho ya Ndugu Frank (kwa Kiingereza), Biblia na vitabu vya nyimbo, kisha onyesha aya au mstari kwenye skrini ya pili. Inafanya kazi bila intaneti, kwa Kiingereza, Kifaransa na Kiswahili.",
      secondaryCta: "Tazama programu",
    },
    download: {
      button: "Pakua kwa Windows",
      heading: "Tayari kwa ibada ya Jumapili",
      pageTitle: "Pakua kwa Windows",
      pageSubtitle: "Toleo {version}, lililotolewa {date}. Windows 10 / 11.",
      note: "Takriban {size}, kwa Windows 10 na 11. Bure, bila akaunti.",
      safetyTitle: "Kidokezo kidogo",
      windowsBody: "Ikiwa Windows inaonyesha onyo la buluu, bonyeza “More info”, kisha “Run anyway”.",
      browserBody: "Ikiwa kivinjari kinauliza kama kuhifadhi faili, chagua “Keep”.",
    },
    product: {
      title: "Skrini moja ya operator, meusi au meupe",
      lead: "Operator anafanya kazi kwenye kompyuta. Waumini wanaona dirisha la kuonyesha pekee. Chagua mandhari inayofaa ukumbi.",
      darkLabel: "Mandhari meusi",
      lightLabel: "Mandhari meupe",
    },
    whatsNew: {
      title: "Mapya katika toleo {version}",
      items: [
        {
          title: "Inafunguka papo hapo",
          body: "MessageFlow iko tayari kutumika mara tu unapoifungua.",
        },
        {
          title: "Maandishi yanatoshea skrini kila wakati",
          body: "Kila mstari, wimbo na aya inaonekana vizuri kwenye projekta. Hakuna neno linalokatwa.",
        },
        {
          title: "Mistari sahihi katika nyimbo za Kiswahili",
          body: "Kila mstari wa wimbo wa Kiswahili sasa unaonekana peke yake, kama unavyoimbwa.",
        },
        {
          title: "Mandhari meupe mapya na safi",
          body: "Muonekano mpya mweupe na buluu kwa kumbi zenye mwanga. Mandhari meusi bado yapo.",
        },
      ],
    },
    features: {
      title: "Kilichomo kwenye maktaba",
      lead: "Sakinisha mara moja, kisha tumia bila intaneti.",
      items: [
        {
          title: "Mahubiri na machapisho",
          body: "Tafuta kwa kichwa, msimbo wa hubiri, mwaka, maneno au namba ya aya, kisha onyesha aya moja.",
        },
        {
          title: "Biblia",
          body: "Andika rejea kama Yohana 3:16, au neno kuu. Songa mstari kwa mstari kwa vitufe vya mishale.",
        },
        {
          title: "Nyimbo",
          body: "Tafuta wimbo kwa kichwa chake au mstari wa maneno yake, kisha uonyeshe ubeti kwa ubeti, pamoja na kiitikio mahali pake.",
        },
      ],
      table: {
        caption: "Maudhui kwa lugha",
        columns: { en: "Kiingereza", fr: "Kifaransa", sw: "Kiswahili" },
        branham: "Mahubiri ya Ndugu Branham",
        frank: "Machapisho ya Ndugu Frank",
        englishOnly: "kwa Kiingereza pekee",
        bible: "Tafsiri ya Biblia",
        songs: "Nyimbo",
        notAvailable: "Haipatikani",
      },
    },
    install: {
      title: "Kutoka kupakua hadi kuonyesha",
      lead: "Hatua nne. Hakuna akaunti, na hakuna intaneti baada ya kusakinisha.",
      steps: [
        {
          n: "01",
          title: "Pakua",
          body: "Bonyeza Pakua kwa Windows. Faili ni takriban {size} na inafanya kazi kwenye Windows 10 na 11.",
        },
        {
          n: "02",
          title: "Sakinisha",
          body: "Fungua faili uliyopakua na ufuate hatua. Ikiwa Windows inaonyesha onyo la buluu, bonyeza “More info”, kisha “Run anyway”. Inahitaji nafasi ya takriban {space}.",
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
        "Tuma maoni, wazo, au tatizo. Tunasoma kila ujumbe.",
      lead: "Programu ya Windows inabaki nje ya mtandao. Tumia fomu hii kuwasiliana na timu ya MessageFlow.",
      calloutTitle: "Una swali au wazo?",
      calloutBody:
        "Tutumie maoni, wazo, au tueleze tatizo. Tunafurahi kukusaidia.",
      calloutCta: "Fungua fomu",
      nameLabel: "Jina",
      nameOptional: "(si lazima)",
      namePlaceholder: "Jina lako",
      emailLabel: "Barua pepe",
      emailPlaceholder: "wewe@mfano.com",
      categoryLabel: "Aina",
      categories: {
        comment: "Maoni",
        feature: "Wazo",
        bug: "Tatizo",
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
      errorConfig: "Ujumbe hauwezi kutumwa kwa sasa. Tafadhali jaribu tena baadaye.",
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
  const units =
    locale === "fr"
      ? size.replace(/\bMB\b/, "Mo").replace(/\bGB\b/, "Go").replace(/(\d)\.(\d)/, "$1,$2")
      : size;
  return units.replace(/ /g, " ");
}

/** "2026-10" as "October 2026", "octobre 2026", or "Oktoba 2026". */
export function localizedMonth(locale: Locale, month: string) {
  const [year, monthNumber] = month.split("-").map(Number);
  return new Intl.DateTimeFormat(localeHtmlLang[locale], {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, monthNumber - 1, 1)));
}
