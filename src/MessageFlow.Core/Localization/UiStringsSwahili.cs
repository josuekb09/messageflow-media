namespace MessageFlow.Core.Localization;

/// <summary>
/// Kiswahili UI strings. Starts from the English table so every key is present,
/// then overlays native wording for operator-facing screens. Admin and rare
/// status messages that are not yet translated fall back to the English text
/// already copied into this table.
/// </summary>
public static class UiStringsSwahili
{
    private static readonly Dictionary<string, string> NativeOverrides = new(StringComparer.Ordinal)
    {
        // ---- Common ----
        ["Common_Close"] = "Funga",
        ["Common_Cancel"] = "Ghairi",
        ["Common_Browse"] = "Vinjari",
        ["Common_Save"] = "Hifadhi",
        ["Common_Search"] = "Tafuta",
        ["Common_Project"] = "Onyesha",
        ["Common_Copy"] = "Nakili",
        ["Common_Admin"] = "Mipangilio",
        ["Common_Clear"] = "Futa",
        ["Common_Preview"] = "Hakikisha",
        ["Common_Import"] = "Ingiza",
        ["Common_Title"] = "Kichwa",
        ["Common_Author"] = "Mwandishi",
        ["Common_Description"] = "Maelezo",
        ["Common_Language"] = "Lugha",
        ["Common_Status"] = "Hali",
        ["Common_Folder"] = "Folda",
        ["Common_Ready"] = "Tayari",

        // ---- Language selector ----
        ["Lang_SelectorTooltip"] = "Badilisha lugha ya programu",
        ["Lang_Changed"] = "Lugha ya programu imewekwa kuwa {0}.",
        ["Lang_ChangedToEnglish"] = "Lugha ya programu imewekwa kuwa Kiingereza.",
        ["Lang_ChangedToFrench"] = "Lugha ya programu imewekwa kuwa Kifaransa.",

        // ---- Top toolbar ----
        ["Toolbar_SearchSermonsTooltip"] = "Tafuta mahubiri kwa kichwa, msimbo, kifungu, au namba ya ibara",
        ["Toolbar_FontSizeTooltip"] = "Ukubwa wa maandishi ya onyesho",
        ["Toolbar_DecreaseTextTooltip"] = "Punguza ukubwa wa maandishi",
        ["Toolbar_FitTextTooltip"] = "Linganisha maandishi na skrini",
        ["Toolbar_IncreaseTextTooltip"] = "Ongeza ukubwa wa maandishi",
        ["Toolbar_Fit"] = "Linganisha",

        // ---- Navigation / library ----
        ["Nav_Library"] = "Maktaba",
        ["Nav_Sermons"] = "Mahubiri",
        ["Nav_Bible"] = "Biblia",
        ["Nav_Songs"] = "Nyimbo",
        ["Nav_Favorites"] = "Vipendwa",
        ["Nav_History"] = "Historia",

        // ---- Centre / preview panels ----
        ["Panel_BiblePreview"] = "Hakikisho la Biblia",
        ["Panel_SongSections"] = "Sehemu za Nyimbo",
        ["Panel_ReadingSermon"] = "Kusoma Mahubiri",
        ["Panel_LiveProjection"] = "Moja kwa moja / Onyesho",
        ["Panel_SearchResults"] = "Matokeo ya utafutaji",
        ["Panel_SermonsBreadcrumb"] = "Mahubiri > {0}",
        ["Panel_CircularLetterResults"] = "Matokeo ya barua za mzunguko",
        ["Panel_SermonResults"] = "Matokeo ya mahubiri",

        // ---- Sermons ----
        ["Sermon_SearchHint"] = "Tafuta mahubiri kwa kichwa, msimbo, kifungu, au namba ya ibara.",
        ["Sermon_SearchHintShort"] = "Tafuta kwa kichwa cha mahubiri, msimbo, kifungu, au namba ya ibara.",
        ["Sermon_Open"] = "Fungua Mahubiri",
        ["Sermon_OpenTooltip"] = "Fungua kila ibara ya mahubiri haya ili kusoma kwa makini",
        ["Sermon_OpenSelectedTooltip"] = "Fungua kila ibara ya mahubiri yaliyochaguliwa ili kusoma kwa makini",
        ["Sermon_ReadyToSearch"] = "Tayari kutafuta mahubiri.",
        ["Sermon_ReadyToSearchHeader"] = "Tayari kutafuta mahubiri",
        ["Sermon_Back"] = "Rudi",
        ["Sermon_BackTooltip"] = "Rudi kwenye matokeo ya awali ya mahubiri",
        ["Sermon_FindInTooltip"] = "Tafuta katika mahubiri yaliyochaguliwa",
        ["Sermon_PreviousMatch"] = "Kipatano kilichotangulia",
        ["Sermon_NextMatch"] = "Kipatano kinachofuata",
        ["Sermon_PreviousPage"] = "Ukurasa uliotangulia",
        ["Sermon_NextPage"] = "Ukurasa unaofuata",
        ["Sermon_PreviousParagraph"] = "Ibara iliyotangulia",
        ["Sermon_NextParagraph"] = "Ibara inayofuata",
        ["Sermon_ParagraphLabel"] = "Ibara {0}",
        ["Sermon_NoFrenchContent"] = "Mahubiri ya Kifaransa hayajapatikana bado.",
        ["Sermon_NoFrenchContentDetail"] = "Mahubiri ya Kifaransa bado hayajaongezwa kwenye maktaba hii.",
        ["Sermon_NoContentForLanguage"] = "Mahubiri hayajapatikana katika {0} bado.",
        ["Sermon_NoContentForLanguageDetail"] = "Mahubiri katika {0} bado hayajaongezwa kwenye maktaba hii. Maudhui ya lugha nyingine yanabaki kwenye kiolesura chake na hayaonyeshwi hapa.",
        ["Sermon_FindInSelected"] = "Tafuta katika mahubiri yaliyochaguliwa.",

        // ---- Bible ----
        ["Bible_SearchHint"] = "Tafuta kwa kitabu, sura, aya, au neno.",
        ["Bible_SearchTooltip"] = "Tafuta marejeo ya Biblia au maneno",
        ["Bible_NotAvailable"] = "Biblia haipatikani. Fungua Mipangilio ikiwa usanidi unahitajika.",
        ["Bible_ReadyToSearch"] = "Tayari kutafuta Biblia.",
        ["Bible_ReadyToSearchHeader"] = "Tayari kutafuta Biblia",
        ["Bible_ReferenceExamples"] = "Mifano: Yohana 3:16, Warumi 8:28, Zaburi 23.",
        ["Bible_PreviousVerse"] = "Aya iliyotangulia",
        ["Bible_NextVerse"] = "Aya inayofuata",
        ["Bible_AddFavorite"] = "Ongeza kipendwa cha Biblia",
        ["Bible_RemoveFavorite"] = "Ondoa kipendwa cha Biblia",
        ["Bible_Book"] = "Kitabu",
        ["Bible_Chapter"] = "Sura",
        ["Bible_SelectBookHint"] = "Chagua kitabu hiki ili kuchagua sura.",
        ["Bible_SelectChapterHint"] = "Chagua sura hii ili kuchagua aya.",
        ["Bible_ChapterMeta"] = "Sura | {0}",
        ["Bible_NoTranslationForLanguage"] = "Tafsiri ya Biblia bado haipatikani kwa lugha hii.",

        // ---- Songs ----
        ["Song_SearchHint"] = "Tafuta nyimbo kwa kichwa au maneno.",
        ["Song_SearchTooltip"] = "Tafuta nyimbo kwa kichwa au maneno",
        ["Song_SearchHintShort"] = "Tafuta kwa kichwa au kifungu cha maneno.",
        ["Song_ReadyToSearch"] = "Tayari kutafuta nyimbo.",
        ["Song_ReadyToSearchHeader"] = "Tayari kutafuta nyimbo",
        ["Song_PreviousSection"] = "Sehemu iliyotangulia",
        ["Song_NextSection"] = "Sehemu inayofuata",
        ["Song_Source"] = "Chanzo cha wimbo",
        ["Song_NoFrenchContent"] = "Nyimbo za Kifaransa hazijapatikana bado.",
        ["Song_NoFrenchContentDetail"] = "Nyimbo za Kifaransa bado hazijaongezwa kwenye maktaba hii.",
        ["Song_NoContentForLanguage"] = "Nyimbo hazijapatikana katika {0} bado.",
        ["Song_NoContentForLanguageDetail"] = "Nyimbo katika {0} bado hazijaongezwa kwenye maktaba hii. Nyimbo za lugha nyingine zinabaki kwenye kiolesura chake na hazionyeshwi hapa.",

        // ---- Favorites ----
        ["Fav_None"] = "Hakuna vipendwa bado.",
        ["Fav_NoneDetail"] = "Hifadhi ibara za mahubiri au aya za Biblia unazotumia mara nyingi ili kuzipata haraka.",
        ["Fav_SermonSection"] = "Vipendwa vya mahubiri",
        ["Fav_BibleSection"] = "Vipendwa vya Biblia",
        ["Fav_NoSermonFavorites"] = "Hakuna vipendwa vya mahubiri bado.",
        ["Fav_NoBibleFavorites"] = "Hakuna vipendwa vya Biblia bado.",
        ["Fav_Remove"] = "Ondoa kipendwa",
        ["Fav_Add"] = "Ongeza kipendwa",
        ["Fav_SavedAt"] = "Imehifadhiwa {0}",
        ["Fav_KindFavorite"] = "Kipendwa",
        ["Fav_KindHistory"] = "Historia",
        ["Meta_Paragraph"] = "Paragrafu {0}",

        // ---- History ----
        ["History_Recent"] = "Maonyesho ya hivi karibuni",
        ["History_Clear"] = "Futa historia",
        ["History_None"] = "Hakuna historia ya onyesho bado.",

        // ---- Count nouns ----
        ["Count_BibleResult_One"] = "tokeo la Biblia",
        ["Count_BibleResult_Many"] = "matokeo ya Biblia",
        ["Count_Song_One"] = "wimbo",
        ["Count_Song_Many"] = "nyimbo",
        ["Count_Sermon_One"] = "hubiri",
        ["Count_Sermon_Many"] = "mahubiri",
        ["Count_Paragraph_One"] = "ibara",
        ["Count_Paragraph_Many"] = "ibara",
        ["Count_Chapter_One"] = "sura",
        ["Count_Chapter_Many"] = "sura",
        ["Count_Verse_One"] = "aya",
        ["Count_Verse_Many"] = "aya",
        ["Count_NoResults"] = "Hakuna matokeo",
        ["Count_BibleBook_One"] = "kitabu cha Biblia",
        ["Count_BibleBook_Many"] = "vitabu vya Biblia",
        ["Count_Document_One"] = "hati",
        ["Count_Document_Many"] = "hati",

        // ---- Status / Bible info used on the main screen ----
        ["Status_Ready"] = "Tayari",
        ["Status_Selected"] = "{0} imechaguliwa.",
        ["Status_SearchingBible"] = "Inatafuta Biblia...",
        ["Status_Searching"] = "Inatafuta...",
        ["Status_SearchingSongs"] = "Inatafuta nyimbo...",
        ["Status_LoadingSongs"] = "Inapakia nyimbo...",
        ["Status_NoSongsFound"] = "Hakuna nyimbo zilizopatikana.",
        ["Status_NoBibleMatches"] = "Hakuna vinavyolingana katika Biblia.",
        ["Status_VerseNotFound"] = "{0} haikupatikana.",
        ["Status_VersesFoundIn"] = "{0} zimepatikana katika {1}.",
        ["Status_BooksFound"] = "{0} zimepatikana.",
        ["Status_ChaptersFound"] = "{0} zimepatikana kwa {1}.",
        ["Status_NoMatchingVerse"] = "Hakuna aya ya Biblia inayolingana.",
        ["Status_SelectVerseBeforeCopy"] = "Tafadhali chagua aya ya Biblia kabla ya kunakili.",
        ["Status_BibleVerseCopied"] = "Aya ya Biblia imenakiliwa.",
        ["Status_SelectVerseBeforeProject"] = "Tafadhali chagua aya ya Biblia kabla ya kuonyesha.",
        ["Status_SelectSectionBeforeCopy"] = "Tafadhali chagua sehemu ya wimbo kabla ya kunakili.",
        ["Status_SelectSectionBeforeProject"] = "Tafadhali chagua sehemu ya wimbo kabla ya kuonyesha.",
        ["Status_SongSectionCopied"] = "Sehemu ya wimbo imenakiliwa.",
        ["Status_ParagraphCopied"] = "Ibara imenakiliwa.",
        ["Status_Copied"] = "{0} imenakiliwa.",
        ["Status_SelectedParagraph"] = "Ibara {0} imechaguliwa.",
        ["Status_NoMatchingParagraph"] = "Hakuna ibara inayolingana.",
        ["Status_StartupFailed"] = "Kuanzisha kumeshindikana. Tazama logs\\app-startup.log.",
        ["Status_ProjectionOpen"] = "Onyesho: limefunguliwa kwenye {0}",
        ["Status_ProjectionClosed"] = "Onyesho: limefungwa",
        ["Status_NoExactPhrase"] = "Hakuna kifungu kamili kilichopatikana",
        ["Status_ExactPhrase"] = "Kifungu kamili",
        ["Status_AllWords"] = "Maneno yote",
        ["Status_Match"] = "Kipatano",
        ["Status_BrowseFound"] = "{0} yamepatikana katika {1} ms.",
        ["Status_SearchFound"] = "{0} yamepatikana katika {1} ({2}) katika {3} ms.",
        ["Status_SearchNoMatch"] = "{0} katika {1} ms.",
        ["Info_CurrentTranslation"] = "Biblia ya sasa: {0} ({1})",
        ["Info_NoTranslation"] = "Hakuna tafsiri ya Biblia iliyopakiwa.",
        ["Info_NoTranslationImported"] = "Hakuna tafsiri ya Biblia iliyoingizwa bado.",
        ["Info_VersionNone"] = "Toleo: hakuna",
        ["Info_Version"] = "Toleo: {0}",
        ["Info_NoVersesImported"] = "Hakuna aya za Biblia zilizoingizwa.",
        ["Info_VerseCount"] = "Aya {0} zinapatikana.",
        ["Filter_AllAuthors"] = "Waandishi wote",
        ["Filter_AllSources"] = "Vyanzo vyote",
        ["Filter_AllYears"] = "Miaka yote",
        ["Font_Small"] = "Ndogo",
        ["Font_Medium"] = "Wastani",
        ["Font_Large"] = "Kubwa",
        ["Font_ExtraLarge"] = "Kubwa sana",
        ["Confirm_ClearHistory"] = "Ungependa kufuta historia yote ya onyesho? Hii haitafuta mahubiri, aya za Biblia, vipendwa, wala vyanzo.",
        ["Confirm_ClearHistoryTitle"] = "Futa historia",
        ["ImportBible_SuggestedFile"] = "Folda inayopendekezwa: Documents\\Bible"
    };

    public static IReadOnlyDictionary<string, string> Values { get; } = Build();

    private static IReadOnlyDictionary<string, string> Build()
    {
        var values = new Dictionary<string, string>(UiStringsEnglish.Values, StringComparer.Ordinal);
        foreach (var pair in NativeOverrides)
        {
            values[pair.Key] = pair.Value;
        }

        return values;
    }
}
