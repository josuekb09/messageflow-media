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
  header: {
    languageLabel: string;
    github: string;
  };
  hero: {
    eyebrow: string;
    title: string;
    subtitle: string;
    downloadMeta: string;
    secondaryCta: string;
  };
  about: {
    title: string;
    body: string;
  };
  solve: {
    title: string;
    items: { title: string; body: string }[];
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
  cta: {
    title: string;
    body: string;
  };
  footer: {
    blurb: string;
    github: string;
    contact: string;
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
  notify: {
    downloadStarted: string;
    title: string;
    body: string;
    accept: string;
    decline: string;
    emailLabel: string;
    emailPlaceholder: string;
    submit: string;
    submitting: string;
    privacy: string;
    emailInvalid: string;
    error: string;
    successTitle: string;
    successBody: string;
    close: string;
  };
  subscription: {
    confirmTitle: string;
    confirmBody: string;
    confirmButton: string;
    confirming: string;
    confirmedTitle: string;
    confirmedBody: string;
    expiredTitle: string;
    expiredBody: string;
    invalidTitle: string;
    invalidBody: string;
    unsubscribeTitle: string;
    unsubscribeBody: string;
    unsubscribeButton: string;
    unsubscribing: string;
    unsubscribedTitle: string;
    unsubscribedBody: string;
    unsubscribeInvalidBody: string;
    error: string;
    home: string;
  };
};

export const dictionaries: Record<Locale, Dictionary> = {
  en: {
    header: {
      languageLabel: "Language",
      github: "GitHub",
    },
    hero: {
      eyebrow: "Free for churches · Windows 10 & 11",
      title: "Find the sermon, verse, or song. Put it on the screen.",
      subtitle:
        "MessageFlow Media is free projection software for church media teams. Search Brother Branham's sermons, Brother Frank's publications, the Bible, and the songbook — then project the exact paragraph or verse on the second screen, online or off.",
      downloadMeta: "Latest version {version} · about {size}",
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
      title: "One screen to operate. A clean window to project.",
      lead: "The media team works on the computer. The congregation sees only the projection window — in the theme that suits the room.",
      darkLabel: "Dark theme",
      lightLabel: "Light theme",
    },
    about: {
      title: "What is MessageFlow Media?",
      body:
        "MessageFlow Media is a free Windows application for churches. It brings the sermons, the Bible, and the songs a congregation uses every week into one place, and puts the right passage on the projector in seconds. Everything is stored on the computer, so it keeps working even without an internet connection.",
    },
    solve: {
      title: "What we solve",
      items: [
        {
          title: "Faithful to the source",
          body: "What appears on the screen matches the printed book, word for word. Nothing is retyped during the service, and nothing is added or changed.",
        },
        {
          title: "Ready in seconds",
          body: "Search by title, reference, paragraph number, or a line of a song, then project it on the second screen with one shortcut — while the service keeps moving.",
        },
        {
          title: "Free and offline",
          body: "Free for every church. Installed once, it runs entirely on your computer, with no account and no internet needed afterward.",
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
    cta: {
      title: "Bring it to your church",
      body: "MessageFlow Media is free. Download it, install it, and you're ready for the next service.",
    },
    footer: {
      blurb:
        "Free Windows software for church media teams: sermons, the Bible, and songs, fully offline.",
      github: "GitHub",
      contact: "Contact",
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
    notify: {
      downloadStarted: "Your download has started.",
      title: "Want to be notified when a new version is released?",
      body: "We'll send you a short email when a new version of MessageFlow Media is ready. Nothing else.",
      accept: "Yes, notify me",
      decline: "No thanks",
      emailLabel: "Your email address",
      emailPlaceholder: "you@example.com",
      submit: "Notify me",
      submitting: "Sending…",
      privacy:
        "We only use your email to announce new versions. We never sell it, and you can unsubscribe at any time.",
      emailInvalid: "Enter a valid email address.",
      error: "Something went wrong. Please try again in a moment.",
      successTitle: "Check your inbox",
      successBody:
        "We sent you an email. Click the button inside to confirm. If you don't see it, look in your spam folder.",
      close: "Close",
    },
    subscription: {
      confirmTitle: "Confirm your email",
      confirmBody: "Click the button to start receiving an email when a new version is released.",
      confirmButton: "Confirm my email",
      confirming: "Confirming…",
      confirmedTitle: "You're subscribed",
      confirmedBody: "Thank you. We'll email you when a new version of MessageFlow Media is ready.",
      expiredTitle: "This link has expired",
      expiredBody: "Please sign up again: click Download for Windows and choose “Yes, notify me”.",
      invalidTitle: "This link is not valid",
      invalidBody: "It may already have been used. If you already confirmed, you're all set.",
      unsubscribeTitle: "Unsubscribe",
      unsubscribeBody:
        "Click the button to stop receiving emails about new versions. Your email address will be deleted.",
      unsubscribeButton: "Unsubscribe",
      unsubscribing: "Unsubscribing…",
      unsubscribedTitle: "You're unsubscribed",
      unsubscribedBody: "Your email address has been deleted. You won't receive any more emails from us.",
      unsubscribeInvalidBody: "This link is not valid. Please use the link from one of our emails.",
      error: "Something went wrong. Please try again in a moment.",
      home: "Back to the home page",
    },
  },
  fr: {
    header: {
      languageLabel: "Langue",
      github: "GitHub",
    },
    hero: {
      eyebrow: "Gratuit pour les églises · Windows 10 et 11",
      title: "Trouvez la prédication, le verset ou le cantique. Projetez-le.",
      subtitle:
        "MessageFlow Media est un logiciel de projection gratuit pour les équipes média des églises. Recherchez les prédications de frère Branham, les publications de frère Frank, la Bible et le recueil de cantiques, puis projetez le paragraphe ou le verset exact sur le second écran, avec ou sans Internet.",
      downloadMeta: "Dernière version {version} · environ {size}",
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
      title: "Un écran pour piloter. Une fenêtre nette pour projeter.",
      lead: "L'équipe média travaille sur l'ordinateur. L'assemblée ne voit que la fenêtre de projection, dans le thème qui convient à la salle.",
      darkLabel: "Thème sombre",
      lightLabel: "Thème clair",
    },
    about: {
      title: "Qu'est-ce que MessageFlow Media ?",
      body:
        "MessageFlow Media est une application Windows gratuite destinée aux églises. Elle réunit en un seul endroit les prédications, la Bible et les cantiques qu'une assemblée utilise chaque semaine, et affiche le bon passage au projecteur en quelques secondes. Tout est enregistré sur l'ordinateur : elle continue de fonctionner même sans connexion Internet.",
    },
    solve: {
      title: "Ce que nous apportons",
      items: [
        {
          title: "Fidèle à la source",
          body: "Ce qui s'affiche à l'écran correspond mot pour mot au livre imprimé. Rien n'est ressaisi pendant le culte, et rien n'est ajouté ni modifié.",
        },
        {
          title: "Prêt en quelques secondes",
          body: "Recherchez par titre, référence, numéro de paragraphe ou ligne de cantique, puis projetez sur le second écran d'un seul raccourci, sans interrompre le culte.",
        },
        {
          title: "Gratuit et hors ligne",
          body: "Gratuit pour toutes les églises. Une fois installé, il fonctionne entièrement sur votre ordinateur, sans compte et sans Internet par la suite.",
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
    cta: {
      title: "Pour votre église",
      body: "MessageFlow Media est gratuit. Téléchargez-le, installez-le, et vous êtes prêts pour le prochain culte.",
    },
    footer: {
      blurb:
        "Logiciel Windows gratuit pour les équipes média des églises : prédications, Bible et cantiques, entièrement hors ligne.",
      github: "GitHub",
      contact: "Contact",
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
    notify: {
      downloadStarted: "Votre téléchargement a commencé.",
      title: "Voulez-vous être prévenu quand une nouvelle version sort ?",
      body: "Nous vous enverrons un court e-mail quand une nouvelle version de MessageFlow Media sera prête. Rien d'autre.",
      accept: "Oui, prévenez-moi",
      decline: "Non merci",
      emailLabel: "Votre adresse e-mail",
      emailPlaceholder: "vous@exemple.com",
      submit: "Me prévenir",
      submitting: "Envoi…",
      privacy:
        "Nous utilisons votre e-mail uniquement pour annoncer les nouvelles versions. Nous ne le vendons jamais, et vous pouvez vous désabonner à tout moment.",
      emailInvalid: "Saisissez une adresse e-mail valide.",
      error: "Une erreur s'est produite. Veuillez réessayer dans un instant.",
      successTitle: "Consultez votre boîte de réception",
      successBody:
        "Nous vous avons envoyé un e-mail. Cliquez sur le bouton qu'il contient pour confirmer. Si vous ne le voyez pas, regardez dans les courriers indésirables.",
      close: "Fermer",
    },
    subscription: {
      confirmTitle: "Confirmez votre adresse e-mail",
      confirmBody: "Cliquez sur le bouton pour recevoir un e-mail à chaque nouvelle version.",
      confirmButton: "Confirmer mon adresse",
      confirming: "Confirmation…",
      confirmedTitle: "Vous êtes inscrit",
      confirmedBody: "Merci. Nous vous écrirons quand une nouvelle version de MessageFlow Media sera prête.",
      expiredTitle: "Ce lien a expiré",
      expiredBody: "Veuillez vous inscrire à nouveau : cliquez sur Télécharger pour Windows et choisissez « Oui, prévenez-moi ».",
      invalidTitle: "Ce lien n'est pas valide",
      invalidBody: "Il a peut-être déjà été utilisé. Si vous avez déjà confirmé, tout est en ordre.",
      unsubscribeTitle: "Se désabonner",
      unsubscribeBody:
        "Cliquez sur le bouton pour ne plus recevoir d'e-mails sur les nouvelles versions. Votre adresse sera supprimée.",
      unsubscribeButton: "Me désabonner",
      unsubscribing: "Désabonnement…",
      unsubscribedTitle: "Vous êtes désabonné",
      unsubscribedBody: "Votre adresse e-mail a été supprimée. Vous ne recevrez plus d'e-mails de notre part.",
      unsubscribeInvalidBody: "Ce lien n'est pas valide. Veuillez utiliser le lien de l'un de nos e-mails.",
      error: "Une erreur s'est produite. Veuillez réessayer dans un instant.",
      home: "Retour à l'accueil",
    },
  },
  sw: {
    header: {
      languageLabel: "Lugha",
      github: "GitHub",
    },
    hero: {
      eyebrow: "Bure kwa makanisa · Windows 10 na 11",
      title: "Pata hubiri, mstari au wimbo. Uonyeshe kwenye skrini.",
      subtitle:
        "MessageFlow Media ni programu ya bure ya kuonyesha maandiko kwa timu za media za kanisa. Tafuta mahubiri ya Ndugu Branham, machapisho ya Ndugu Frank, Biblia na kitabu cha nyimbo, kisha onyesha aya au mstari halisi kwenye skrini ya pili, ukiwa na intaneti au bila.",
      downloadMeta: "Toleo jipya {version} · takriban {size}",
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
      title: "Skrini moja ya kuendesha. Dirisha safi la kuonyesha.",
      lead: "Timu ya media inafanya kazi kwenye kompyuta. Kusanyiko linaona dirisha la onyesho tu, katika mandhari yanayofaa ukumbi.",
      darkLabel: "Mandhari meusi",
      lightLabel: "Mandhari meupe",
    },
    about: {
      title: "MessageFlow Media ni nini?",
      body:
        "MessageFlow Media ni programu ya Windows ya bure kwa makanisa. Inaweka pamoja mahubiri, Biblia na nyimbo ambazo kusanyiko hutumia kila wiki, na kuweka kifungu sahihi kwenye projekta kwa sekunde chache. Kila kitu kimehifadhiwa kwenye kompyuta, kwa hiyo inaendelea kufanya kazi hata bila intaneti.",
    },
    solve: {
      title: "Tunachotatua",
      items: [
        {
          title: "Aminifu kwa chanzo",
          body: "Kinachoonekana kwenye skrini kinalingana neno kwa neno na kitabu kilichochapishwa. Hakuna kinachoandikwa upya wakati wa ibada, na hakuna kinachoongezwa wala kubadilishwa.",
        },
        {
          title: "Tayari kwa sekunde chache",
          body: "Tafuta kwa kichwa, rejea, namba ya aya au mstari wa wimbo, kisha uonyeshe kwenye skrini ya pili kwa mkato mmoja wa kibodi, huku ibada ikiendelea.",
        },
        {
          title: "Bure na bila intaneti",
          body: "Bure kwa kila kanisa. Ikishasakinishwa, inafanya kazi yote kwenye kompyuta yako, bila akaunti na bila intaneti baadaye.",
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
    cta: {
      title: "Ilete kanisani kwako",
      body: "MessageFlow Media ni bure. Ipakue, isakinishe, na uko tayari kwa ibada ijayo.",
    },
    footer: {
      blurb:
        "Programu ya Windows ya bure kwa timu za media za kanisa: mahubiri, Biblia na nyimbo, bila intaneti.",
      github: "GitHub",
      contact: "Wasiliana nasi",
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
    notify: {
      downloadStarted: "Upakuaji wako umeanza.",
      title: "Ungependa kujulishwa toleo jipya linapotoka?",
      body: "Tutakutumia barua pepe fupi toleo jipya la MessageFlow Media likiwa tayari. Hakuna kingine.",
      accept: "Ndiyo, nijulishe",
      decline: "Hapana, asante",
      emailLabel: "Barua pepe yako",
      emailPlaceholder: "wewe@mfano.com",
      submit: "Nijulishe",
      submitting: "Inatuma…",
      privacy:
        "Tunatumia barua pepe yako tu kutangaza matoleo mapya. Hatuiuzi kamwe, na unaweza kujiondoa wakati wowote.",
      emailInvalid: "Weka anwani sahihi ya barua pepe.",
      error: "Kumetokea hitilafu. Tafadhali jaribu tena baadaye kidogo.",
      successTitle: "Angalia sanduku lako la barua",
      successBody:
        "Tumekutumia barua pepe. Bonyeza kitufe kilichomo ili kuthibitisha. Usipoiona, angalia kwenye barua taka (spam).",
      close: "Funga",
    },
    subscription: {
      confirmTitle: "Thibitisha barua pepe yako",
      confirmBody: "Bonyeza kitufe ili uanze kupokea barua pepe toleo jipya linapotoka.",
      confirmButton: "Thibitisha barua pepe yangu",
      confirming: "Inathibitisha…",
      confirmedTitle: "Umejiandikisha",
      confirmedBody: "Asante. Tutakutumia barua pepe toleo jipya la MessageFlow Media likiwa tayari.",
      expiredTitle: "Kiungo hiki kimeisha muda",
      expiredBody: "Tafadhali jiandikishe tena: bonyeza Pakua kwa Windows kisha chagua “Ndiyo, nijulishe”.",
      invalidTitle: "Kiungo hiki si sahihi",
      invalidBody: "Huenda kimeshatumika. Ikiwa umeshathibitisha, kila kitu kiko sawa.",
      unsubscribeTitle: "Jiondoe",
      unsubscribeBody:
        "Bonyeza kitufe ili uache kupokea barua pepe kuhusu matoleo mapya. Barua pepe yako itafutwa.",
      unsubscribeButton: "Jiondoe",
      unsubscribing: "Inaondoa…",
      unsubscribedTitle: "Umejiondoa",
      unsubscribedBody: "Barua pepe yako imefutwa. Hutapokea barua pepe zaidi kutoka kwetu.",
      unsubscribeInvalidBody: "Kiungo hiki si sahihi. Tafadhali tumia kiungo kutoka kwenye mojawapo ya barua pepe zetu.",
      error: "Kumetokea hitilafu. Tafadhali jaribu tena baadaye kidogo.",
      home: "Rudi ukurasa wa mwanzo",
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
